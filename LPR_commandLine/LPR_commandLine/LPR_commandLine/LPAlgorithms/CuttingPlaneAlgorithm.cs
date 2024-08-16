using LPR_commandLine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace LPR_commandLine.LPAlgorithms
{
    public class CuttingPlane
    {
        private LinearProgrammingModel _model;
        private ISimplexAlgorithm _simplexAlgorithm;
        private DualSimplex _dualAlgorithm;
        private int _subproblemCount = 0;

        public CuttingPlane(LinearProgrammingModel model, ISimplexAlgorithm simplexAlgorithm, DualSimplex dualAlgorithm)
        {
            _model = model;
            _simplexAlgorithm = simplexAlgorithm;
            _dualAlgorithm = dualAlgorithm;
        }

        public void Solve()
        {
            const double epsilon = 1e-6; // Tolerance for treating numbers as integers

            // Ensure the model's tableau is updated before solving
            _model.UpdateTableau();

            // Initial solve using the simplex algorithm
            var (solution, isOptimal, finalTableau) = _simplexAlgorithm.Solve();

            while (!IsIntegerSolution(finalTableau, epsilon) && _subproblemCount<=10)
            {
                _subproblemCount++;
                Console.WriteLine($"Subproblem {_subproblemCount}");

                // Print the RHS values for each variable
                PrintRhsValues(finalTableau);

                // Find the constraint with the closest fractional RHS
                int chosenConstraintIndex = FindChosenConstraint(finalTableau, epsilon);
                Console.WriteLine($"Cutting on variable x{chosenConstraintIndex}");

                // Create a new constraint based on the chosen constraint
                var newConstraint = CreateCuttingPlaneConstraint(finalTableau, chosenConstraintIndex);

                // Debug: Print the new constraint being added
                Console.WriteLine($"Adding new constraint: {string.Join(", ", newConstraint.Coefficients)} <= {newConstraint.RHS}");

                // Apply the new constraint to the optimal tableau
                _model.ApplyCut(newConstraint);

                // Update the simplex algorithm with the new model
                _simplexAlgorithm.UpdateModel(_model);

                // Ensure the model's tableau is updated before solving
                _model.UpdateTableau();

                // Re-solve the updated model using the dual algorithm to get the new optimal tableau
                (solution, isOptimal, finalTableau) = _dualAlgorithm.Solve();

                // Debug: Print the new tableau after solving
                Console.WriteLine("New tableau after solving:");
                PrintTableau(finalTableau);
            }

            Console.WriteLine("Optimal integer solution found.");
        }

        private void PrintRhsValues(double[,] tableau)
        {
            int numRows = tableau.GetLength(0);
            int numCols = tableau.GetLength(1);

            for (int i = 1; i < numRows; i++) // Start from row 1 to exclude objective row
            {
                double rhs = tableau[i, numCols - 1];
                Console.WriteLine($"Variable x{i}: RHS = {rhs}");
            }
        }

        private bool IsIntegerSolution(double[,] tableau, double epsilon)
        {
            int numRows = tableau.GetLength(0);
            int numCols = tableau.GetLength(1);

            for (int i = 1; i < numRows; i++) // Start from row 1 to exclude objective row
            {
                double rhs = tableau[i, numCols - 1];
                if (Math.Abs(rhs - Math.Round(rhs)) > epsilon)
                {
                    return false; // Fractional part found
                }
            }

            return true; // All RHS values are close enough to integers
        }

        private int FindChosenConstraint(double[,] tableau, double epsilon)
        {
            int numRows = tableau.GetLength(0);
            int numCols = tableau.GetLength(1);

            double minDistance = double.MaxValue;
            int chosenConstraintIndex = -1;

            for (int i = 1; i < numRows; i++) // Start from row 1 to exclude objective row
            {
                double rhs = tableau[i, numCols - 1];
                double distance = Math.Abs(rhs - Math.Round(rhs));

                // Prioritize constraints closer to 0.5
                if (distance > epsilon && distance < minDistance)
                {
                    minDistance = distance;
                    chosenConstraintIndex = i;
                }
                // If multiple constraints have the same distance, prioritize basic variables
                else if (distance > epsilon && distance == minDistance)
                {
                    int basicVariableIndex = GetBasicVariableIndex(tableau, i);
                    if (basicVariableIndex != -1)
                    {
                        chosenConstraintIndex = i;
                    }
                }
            }

            return chosenConstraintIndex;
        }

        private void PrintTableau(double[,] tableau)
        {
            int numRows = tableau.GetLength(0);
            int numCols = tableau.GetLength(1);

            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    Console.Write($"{tableau[i, j]:F2}\t");
                }
                Console.WriteLine();
            }
        }

        private int GetBasicVariableIndex(double[,] tableau, int rowIndex)
        {
            int numCols = tableau.GetLength(1);

            for (int j = 0; j < numCols - 1; j++)
            {
                if (Math.Abs(tableau[rowIndex, j] - 1) < 0.0001)
                {
                    return j;
                }
            }

            return -1; // No basic variable found
        }

        private Constraint CreateCuttingPlaneConstraint(double[,] tableau, int constraintIndex)
        {
            int numCols = tableau.GetLength(1);

            // Extract coefficients and RHS from the chosen constraint
            double[] coefficients = new double[numCols - 1];
            for (int j = 0; j < numCols - 1; j++)
            {
                coefficients[j] = tableau[constraintIndex, j];
            }
            double rhs = tableau[constraintIndex, numCols - 1];
            
            // Break coefficients and RHS into integer and decimal parts
            int[] integerCoefficients = new int[numCols - 1];
            double[] decimalCoefficients = new double[numCols - 1];
            int integerRhs = (int)rhs;
            double decimalRhs = rhs - integerRhs;

            for (int j = 0; j < numCols - 1; j++)
            {
                integerCoefficients[j] = (int)coefficients[j];
                decimalCoefficients[j] = coefficients[j] - integerCoefficients[j];
            }

             //Modify negative decimals
            for (int j = 0; j < numCols - 1; j++)
            {
                if (decimalCoefficients[j] < 0)
                {
                    decimalCoefficients[j] = (1+ decimalCoefficients[j]);
                }
            }

            // Remove integer parts
            for (int j = 0; j < numCols - 1; j++)
            {
                coefficients[j] = decimalCoefficients[j];
            }

            // Multiply by -1
            for (int j = 0; j < numCols - 1; j++)
            {
                coefficients[j] = -coefficients[j];
            }
            rhs = -decimalRhs; // Correctly negate the decimal part of rhs
            

            // Add slack variables
            List<double> newCoefficients = new List<double>(coefficients);
            newCoefficients.Add(1); // Add one slack variable for now (adjust as needed)

            // Create the new constraint
            Constraint newConstraint = new Constraint
            {
                Coefficients = newCoefficients,
                Relation = "<=",
                RHS = rhs
            };

            return newConstraint;
        }
    }
}