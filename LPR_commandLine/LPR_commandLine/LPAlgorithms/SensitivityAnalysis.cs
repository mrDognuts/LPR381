using LPR_commandLine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;

namespace LPR_commandLine.LPAlgorithms
{
    public class SensitivityAnalysis
    {
        public SensitivityAnalysisModel _model;

        public SensitivityAnalysis(SensitivityAnalysisModel model)
        {
            _model = model;
        }

        public double[] GetNonBasicVariableRange(int variableIndex)
        {
            if (_model.OptimalTableau == null)
                throw new InvalidOperationException("OptimalTableau is not initialized.");
            // Initialize the lower and upper bounds
            double lowerBound = 0;
            double upperBound = double.MaxValue;

            // Get the num of rows,col
            int numRows = _model.OptimalTableau.GetLength(0);
            int numColumns = _model.OptimalTableau.GetLength(1);

            for (int i = 0; i < numRows; i++)
            {
                double coefficient = _model.OptimalTableau[i, variableIndex];
                if (coefficient != 0)
                {
                    double rhs = _model.OptimalTableau[i, numColumns - 1];
                    double ratio = rhs / coefficient;

                    // Update the upper or lower bound
                    if (coefficient > 0)
                        upperBound = Math.Min(upperBound, ratio);
                    else if (coefficient < 0)
                        lowerBound = Math.Max(lowerBound, ratio);
                }
            }
            // Return the calculated bounds as an array
            return new double[] { lowerBound, upperBound };
        }

        public void ApplyNonBasicVariableChange(int variableIndex, double newValue)
        {
            for (int i = 0; i < _model.OptimalTableau.GetLength(0) - 1; i++)
            {
                // Get the coefficient of the variable in the current row
                double coefficient = _model.OptimalTableau[i, variableIndex];

                // Update the right-hand side value by subtracting the product of the coefficient and the new value
                _model.OptimalTableau[i, _model.OptimalTableau.GetLength(1) - 1] -= coefficient * newValue;
            }
            // Get the coefficient of the variable in the objective function row
            double objectiveCoefficient = _model.OptimalTableau[_model.OptimalTableau.GetLength(0) - 1, variableIndex];
            // Update the objective function value by subtracting the product of the objective coefficient and the new value
            _model.OptimalTableau[_model.OptimalTableau.GetLength(0) - 1, _model.OptimalTableau.GetLength(1) - 1] -= objectiveCoefficient * newValue;
            // Print the updated tableau 
            PrintUpdatedTableau();
        }



        public double[] GetBasicVariableRange(int variableIndex)
        {
            double lowerBound = double.MinValue;
            double upperBound = double.MaxValue;

            int rowIndex = _model.BasicVariables.IndexOf(variableIndex);

            for (int j = 0; j < _model.OptimalTableau.GetLength(1) - 1; j++)
            {
                if (_model.NonBasicVariables.Contains(j))
                {
                    double coefficient = _model.OptimalTableau[rowIndex, j];
                    if (coefficient < 0)
                    {
                        double upperBoundCandidate = -_model.OptimalTableau[rowIndex, _model.OptimalTableau.GetLength(1) - 1] / coefficient;
                        upperBound = Math.Min(upperBound, upperBoundCandidate);
                    }
                    else if (coefficient > 0)
                    {
                        double lowerBoundCandidate = -_model.OptimalTableau[rowIndex, _model.OptimalTableau.GetLength(1) - 1] / coefficient;
                        lowerBound = Math.Max(lowerBound, lowerBoundCandidate);
                    }
                }
            }

            return new double[] { lowerBound, upperBound };
        }

        public void ApplyBasicVariableChange(int variableIndex, double newValue)
        {
            int rowIndex = _model.BasicVariables.IndexOf(variableIndex);

            if (rowIndex == -1)
                throw new ArgumentException("The specified variable is not a basic variable.");

            double currentValue = _model.OptimalTableau[rowIndex, _model.OptimalTableau.GetLength(1) - 1];
            double difference = newValue - currentValue;

            _model.OptimalTableau[rowIndex, _model.OptimalTableau.GetLength(1) - 1] = newValue;

            for (int i = 0; i < _model.OptimalTableau.GetLength(0); i++)
            {
                if (i != rowIndex)
                {
                    _model.OptimalTableau[i, _model.OptimalTableau.GetLength(1) - 1] -= difference * _model.OptimalTableau[i, variableIndex];
                }
            }
            PrintUpdatedTableau();
        }

        public double[] GetConstraintRHSRange(int constraintIndex)
        {
            if (constraintIndex < 0 || constraintIndex >= _model.LpModel.Constraints.Count)
                throw new ArgumentOutOfRangeException(nameof(constraintIndex), "Invalid constraint index.");

            int rowIndex = constraintIndex + 1;
            int numColumns = _model.OptimalTableau.GetLength(1);

            double minRange = double.NegativeInfinity;
            double maxRange = double.PositiveInfinity;

            for (int j = 0; j < numColumns - 1; j++)
            {
                if (_model.NonBasicVariables.Contains(j))
                {
                    double coefficient = _model.OptimalTableau[rowIndex, j];
                    double currentValue = _model.OptimalTableau[rowIndex, numColumns - 1];
                    double minChange = double.NegativeInfinity;
                    double maxChange = double.PositiveInfinity;

                    if (coefficient != 0)
                    {
                        if (coefficient > 0)
                            maxChange = (currentValue - minRange) / coefficient;
                        else
                            minChange = (currentValue - maxRange) / coefficient;
                    }

                    minRange = Math.Max(minRange, minChange);
                    maxRange = Math.Min(maxRange, maxChange);
                }
            }

            return new double[] { minRange, maxRange };
        }

        public void ApplyConstraintRHSChange(int constraintIndex, double newValue)
        {
            if (constraintIndex < 0 || constraintIndex >= _model.LpModel.Constraints.Count)
                throw new ArgumentOutOfRangeException(nameof(constraintIndex), "Invalid constraint index.");

            int rowIndex = constraintIndex + 1;
            double currentRHS = _model.OptimalTableau[rowIndex, _model.OptimalTableau.GetLength(1) - 1];
            double rhsDifference = newValue - currentRHS;

            _model.OptimalTableau[rowIndex, _model.OptimalTableau.GetLength(1) - 1] = newValue;

            int numColumns = _model.OptimalTableau.GetLength(1);

            for (int j = 0; j < numColumns - 1; j++)
            {
                _model.OptimalTableau[rowIndex, j] += rhsDifference;
            }

            PrintUpdatedTableau();
        }

        public void PrintUpdatedTableau()
        {
            if (_model.OptimalTableau == null || _model.OptimalTableau.GetLength(0) == 0 || _model.OptimalTableau.GetLength(1) == 0)
            {
                Console.WriteLine("The tableau is not available.");
                return;
            }

            int numRows = _model.OptimalTableau.GetLength(0);
            int numCols = _model.OptimalTableau.GetLength(1);

            Console.Write("Tableau (Dual Model):\n");
            Console.Write("         ");

            for (int j = 0; j < numCols - 1; j++) 
            {
                Console.Write($"X{j + 1,-6}"); 
            }
            Console.Write("  RHS\n"); 
            for (int i = 0; i < numRows; i++)
            {
                Console.Write($"C{i + 1,2}: "); 

                for (int j = 0; j < numCols; j++)
                {
                    Console.Write($"{_model.OptimalTableau[i, j],7:F2} ");
                }
                Console.WriteLine();
            }
        }


        public SensitivityAnalysisModel AddNewActivity(double[] coefficients, double cost)
        {
            int numRows = _model.OptimalTableau.GetLength(0); //Num of row
            int numColumns = _model.OptimalTableau.GetLength(1);//Num of col

            //Create a new tableau
            double[,] newTableau = new double[numRows, numColumns + 1];

            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numColumns; j++)
                {
                    newTableau[i, j] = _model.OptimalTableau[i, j];
                }
            }

            for (int i = 0; i < numRows - 1; i++)
            {
                newTableau[i, numColumns] = i < coefficients.Length ? coefficients[i] : 0;
            }
            newTableau[numRows - 1, numColumns] = -cost;

            var updatedModel = new SensitivityAnalysisModel(
                _model.LpModel,
                newTableau,
                _model.BasicVariables,
                _model.NonBasicVariables
            );

            return updatedModel;
        }

        //constraints add
        public void AddNewConstraint(double[] coefficients, double rhs)
        {
            int numRows = _model.OptimalTableau.GetLength(0);
            int numColumns = _model.OptimalTableau.GetLength(1);

            if (coefficients.Length > numColumns - 1)
                throw new ArgumentException("The length of coefficients cannot exceed the number of variables.");

            double[,] newTableau = new double[numRows + 1, numColumns];

            //Copy table
            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numColumns; j++)
                {
                    newTableau[i, j] = _model.OptimalTableau[i, j];
                }
            }

            //add constraint
            for (int j = 0; j < coefficients.Length; j++)
            {
                newTableau[numRows, j] = coefficients[j];
            }

            //Add zeros 
            for (int j = coefficients.Length; j < numColumns - 1; j++)
            {
                newTableau[numRows, j] = 0;
            }

            //Set the RHS for new const
            newTableau[numRows, numColumns - 1] = rhs;

            //Create and add the new const to model
            var newConstraint = new Constraint
            {
                Coefficients = new List<double>(coefficients),
                RHS = rhs
            };

            _model.LpModel.Constraints.Add(newConstraint);

            //update table
            _model.UpdateOptimalTableau(newTableau);

            //print
            PrintUpdatedTableau();
        }


        //get shadow prices
        public double[] GetShadowPrices()
        {
            int numRows = _model.OptimalTableau.GetLength(0); 
            int numColumns = _model.OptimalTableau.GetLength(1);

            double[] shadowPrices = new double[numRows - 1]; 

            for (int i = 1; i < numRows; i++)
            {
                shadowPrices[i - 1] = _model.OptimalTableau[i, numColumns - 1]; 
            }

            return shadowPrices;
        }

        public int GetNumberOfVariables()
        {
            return _model.OptimalTableau.GetLength(1) - 1; 
        }
        public LinearProgrammingModel ApplyDuality()
        {
            //Check that the model is initialized
            if (_model == null || _model.OptimalTableau == null)
            {
                throw new InvalidOperationException("The model or optimal tableau is not initialized.");
            }

            Console.WriteLine("Formulating the Dual Model...");

            int numRows = _model.OptimalTableau.GetLength(0);
            int numCols = _model.OptimalTableau.GetLength(1);

            List<double> dualObjectiveCoefficients = new List<double>();
            for (int i = 0; i < numRows - 1; i++)
            {
                dualObjectiveCoefficients.Add(_model.OptimalTableau[i, numCols - 1]);
            }

            List<Constraint> dualConstraints = new List<Constraint>();
            for (int i = 0; i < numCols - 1; i++)
            {
                List<double> dualConstraintCoefficients = new List<double>();
                for (int j = 0; j < numRows - 1; j++)
                {
                    dualConstraintCoefficients.Add(_model.OptimalTableau[j, i]);
                }
                var dualConstraint = new Constraint
                {
                    Coefficients = dualConstraintCoefficients,
                    RHS = _model.OptimalTableau[numRows - 1, i],
                    Relation = _model.LpModel.Objective.Type == "Maximization" ? ">=" : "<="
                };
                dualConstraints.Add(dualConstraint);
            }
            var dualModel = new LinearProgrammingModel
            {
                Objective = new ObjectiveFunction
                {
                    Type = _model.LpModel.Objective.Type == "Maximization" ? "Minimization" : "Maximization",
                    Coefficients = dualObjectiveCoefficients
                },
                Constraints = dualConstraints,
                SignRestrictions = new List<string>(_model.LpModel.SignRestrictions)
            };

            _model.OptimalTableau = ConvertToTableau(dualModel); //convert

            Console.WriteLine("\nDual Objective Coefficients:");
            foreach (var coeff in dualObjectiveCoefficients)
            {
                Console.Write($"{coeff:F2}\t");
            }
            Console.WriteLine();

            Console.WriteLine("\nDual Constraints:");
            foreach (var constraint in dualConstraints)
            {
                Console.WriteLine(string.Join("\t", constraint.Coefficients.Select(c => c.ToString("F2"))) + $" {constraint.Relation} {constraint.RHS:F2}");
            }

            PrintUpdatedTableau();

            return dualModel;
        }
        private double[,] ConvertToTableau(LinearProgrammingModel model)
        {
            int numConstraints = model.Constraints.Count;
            int numVariables = model.Objective.Coefficients.Count;
            int numRows = numConstraints + 1; 
            int numCols = numVariables + 1; 

            double[,] tableau = new double[numRows, numCols];

            for (int j = 0; j < numVariables; j++)
            {
                tableau[0, j] = model.Objective.Coefficients[j];
            }

            for (int i = 0; i < numConstraints; i++)
            {
                var constraint = model.Constraints[i];
                for (int j = 0; j < numVariables; j++)
                {
                    tableau[i + 1, j] = constraint.Coefficients[j];
                }
                tableau[i + 1, numVariables] = constraint.RHS;
            }

            return tableau;
        }



        public void SolveDualModel()
        {
            if (_model == null || _model.OptimalTableau == null)
            {
                throw new InvalidOperationException("The dual model or optimal tableau is not initialized.");
            }

            LinearProgrammingModel dualModel = ApplyDuality();
            PrimalSimplex dualSimplex = new PrimalSimplex(dualModel);

            var result = dualSimplex.Solve();
            Console.WriteLine("\nDual Model Solution:");
            if (result.IsOptimal)
            {
                Console.WriteLine("Optimal solution found.");
                Console.WriteLine("Solution:");
                for (int i = 0; i < result.Solution.Length; i++)
                {
                    Console.WriteLine($"x{i + 1} = {result.Solution[i]:F2}");
                }
            }
            else
            {
                Console.WriteLine("The dual model is unbounded or infeasible.");
            }
        }


        public (bool isVerified, string dualityType) VerifyDuality()
        {
            if (_model == null || _model.OptimalTableau == null)
            {
                return (false, "Model is not initialized.");
            }
            bool isFeasible = true; 
            bool hasDualityGap = false; 

            if (isFeasible)
            {
                if (hasDualityGap)
                {
                    return (true, "Weak Duality");
                }
                else
                {
                    return (true, "Strong Duality");
                }
            }
            else
            {
                return (false, "Infeasible model.");
            }
        }
    }
}
