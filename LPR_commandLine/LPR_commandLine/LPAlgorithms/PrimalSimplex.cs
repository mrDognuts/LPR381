using LPR_commandLine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LPR_commandLine.LPAlgorithms
{
    public class PrimalSimplex : ISimplexAlgorithm
    {
        private InitializeTable _initializer;
        private LinearProgrammingModel model;
        private string[] columnNames;
        private string[] rowNames;

        public PrimalSimplex(LinearProgrammingModel model)
        {
            _initializer = new InitializeTable(model);
            this.model = model;

            // Generate column and row names based on your model
            columnNames = GenerateColumnNames(model);
            rowNames = GenerateRowNames(model);
        }

        // Public implementation of ISimplexAlgorithm methods
        public void UpdateModel(LinearProgrammingModel model)
        {
            this.model = model;
            _initializer = new InitializeTable(model);

            // Re-generate column and row names based on the new model
            columnNames = GenerateColumnNames(model);
            rowNames = GenerateRowNames(model);
        }

        public (double[] Solution, bool IsOptimal, double[,] FinalTableau) Solve()
        {
            _initializer.InitializeTableau();
            int tableNumber = 1;
            Console.WriteLine("------------------------------------------------------------------------------------------------------------");
            Console.WriteLine($"Table {tableNumber}");
            _initializer.PrintTableau();

            double[,] tableau = _initializer.Tableau;
            int numRows = tableau.GetLength(0);
            int numCols = tableau.GetLength(1);

            while (true)
            {
                int pivotColumn = FindPivotColumn(tableau, model.Objective.Type);

                if (pivotColumn == -1)
                {
                    Console.WriteLine("Optimal solution found.");
                    return (GetSolution(tableau, numRows, numCols), true, tableau);
                }

                double minRatio = double.PositiveInfinity;
                int pivotRow = -1;
                for (int i = 1; i < numRows; i++)
                {
                    if (tableau[i, pivotColumn] > 0)
                    {
                        double ratio = tableau[i, numCols - 1] / tableau[i, pivotColumn];
                        if (ratio < minRatio)
                        {
                            minRatio = ratio;
                            pivotRow = i;
                        }
                    }
                }

                if (pivotRow == -1)
                {
                    Console.WriteLine("The problem is unbounded.");
                    return (null, false, tableau);
                }

                Pivot(tableau, pivotRow, pivotColumn);

                tableNumber++;
                Console.WriteLine("------------------------------------------------------------------------------------------------------------");
                Console.WriteLine($"Table {tableNumber}");
                _initializer.PrintTableau();
            }
        }

        private double[] GetSolution(double[,] tableau, int numRows, int numCols)
        {
            double[] solution = new double[numCols - 1]; // Adjust according to model
            for (int j = 0; j < numCols - 1; j++)
            {
                bool isBasic = false;
                for (int i = 1; i < numRows; i++)
                {
                    if (Math.Abs(tableau[i, j] - 1) < 1e-9 && tableau[i, j] == 1) // Check if column is a basic variable
                    {
                        solution[j] = tableau[i, numCols - 1];
                        isBasic = true;
                        break;
                    }
                }
                if (!isBasic)
                {
                    solution[j] = 0;
                }
            }
            return solution;
        }


        private void Pivot(double[,] tableau, int pivotRow, int pivotColumn)
        {
            int numCols = tableau.GetLength(1);
            int numRows = tableau.GetLength(0);

            if (pivotRow >= 0 && pivotRow < numRows && pivotColumn >= 0 && pivotColumn < numCols)
            {
                double pivotElement = tableau[pivotRow, pivotColumn];
                for (int j = 0; j < numCols; j++)
                {
                    tableau[pivotRow, j] /= pivotElement;
                }

                for (int i = 0; i < numRows; i++)
                {
                    if (i != pivotRow)
                    {
                        double factor = tableau[i, pivotColumn];
                        for (int j = 0; j < numCols; j++)
                        {
                            tableau[i, j] -= factor * tableau[pivotRow, j];
                        }
                    }
                }

                Console.WriteLine($"Pivot Column: {columnNames[pivotColumn]}");
                Console.WriteLine($"Pivot Row: {rowNames[pivotRow]}");
            }
            else
            {
                Console.WriteLine("Pivot indices are out of bounds!");
            }
        }

        private string[] GenerateColumnNames(LinearProgrammingModel model)
        {
            int numVariables = model.Objective.Coefficients.Count;
            int numSlackExcess = model.Constraints.Count; // Assuming one slack/excess variable per constraint
            string[] names = new string[numVariables + numSlackExcess + 1]; // +1 for the RHS column

            for (int i = 0; i < numVariables; i++)
            {
                names[i] = $"x{i + 1}";
            }
            for (int i = 0; i < numSlackExcess; i++)
            {
                names[numVariables + i] = $"s/e{i + 1}";
            }
            names[numVariables + numSlackExcess] = "RHS";
            return names;
        }

        private string[] GenerateRowNames(LinearProgrammingModel model)
        {
            int numConstraints = model.Constraints.Count;
            string[] names = new string[numConstraints + 1];
            names[0] = "z";
            for (int i = 0; i < numConstraints; i++)
            {
                names[i + 1] = $"c{i + 1}";
            }
            return names;
        }

        private int FindPivotColumn(double[,] tableau, string objectiveType)
        {
            int numCols = tableau.GetLength(1);
            int pivotColumn = -1;

            if (objectiveType == "max")
            {
                double mostNegative = 0;
                for (int j = 0; j < numCols - 1; j++)
                {
                    if (tableau[0, j] < mostNegative)
                    {
                        mostNegative = tableau[0, j];
                        pivotColumn = j;
                    }
                }
            }
            else if (objectiveType == "min")
            {
                double mostPositive = 0;
                for (int j = 0; j < numCols - 1; j++)
                {
                    if (tableau[0, j] > mostPositive)
                    {
                        mostPositive = tableau[0, j];
                        pivotColumn = j;
                    }
                }
            }

            return pivotColumn;
        }

        void ISimplexAlgorithm.UpdateModel(LinearProgrammingModel model)
        {
            this.UpdateModel(model);
        }

        (object solution, bool isOptimal, double[,] finalTableau) ISimplexAlgorithm.Solve()
        {
            var result = this.Solve();
            return (result.Solution, result.IsOptimal, result.FinalTableau);
        }
    }
}