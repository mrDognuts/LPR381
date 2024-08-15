using LPR_commandLine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace LPR_commandLine.LPAlgorithms
{

        public class CuttingPlaneAlgorithm
        {
            private LinearProgrammingModel _model;
            private PrimalSimplex _primalSimplex;
            private DualSimplex _dualSimplex;
            private double[,] _optimalTableau;
            private List<(double[] Solution, double ObjectiveValue)> _solutions;
            private bool _useDualSimplex;

            public CuttingPlaneAlgorithm(LinearProgrammingModel model, bool useDualSimplex = false)
            {
                _model = model;
                _primalSimplex = new PrimalSimplex(_model);
                _dualSimplex = new DualSimplex(_model);
                _solutions = new List<(double[] Solution, double ObjectiveValue)>();
                _useDualSimplex = useDualSimplex;
            }

            public void RunCuttingPlane()
            {
                var nodes = new Queue<LinearProgrammingModel>();
                var visitedModels = new HashSet<string>();
                nodes.Enqueue(_model);

                while (nodes.Count > 0)
                {
                    var currentModel = nodes.Dequeue();
                    var modelKey = GetModelKey(currentModel);
                    if (visitedModels.Contains(modelKey))
                    {
                        continue;
                    }
                    visitedModels.Add(modelKey);

                    Console.WriteLine("\nProcessing Node:");
                    DisplayModel(currentModel);

                    var simplexAlgorithm = _useDualSimplex ? (ISimplexAlgorithm)_dualSimplex : _primalSimplex;
                    simplexAlgorithm.UpdateModel(currentModel);

                    var (solutionObj, isOptimal, finalTableau) = simplexAlgorithm.Solve();

                    if (!isOptimal)
                    {
                        Console.WriteLine("The problem is unbounded or infeasible.");
                        continue;
                    }

                    _optimalTableau = finalTableau;

                    double[] solution = solutionObj as double[];
                    if (solution == null)
                    {
                        Console.WriteLine("Error: The solution is not of the expected type.");
                        continue;
                    }

                    Console.WriteLine("Optimal Tableau:");
                    PrintTableau(_optimalTableau);

                    double objectiveValue = ComputeObjectiveValue(solution);
                    Console.WriteLine("Solution:");
                    for (int i = 0; i < solution.Length; i++)
                    {
                        Console.WriteLine($"x{i + 1} = {solution[i]:F2}");
                    }
                    Console.WriteLine($"Objective Value: {objectiveValue:F2}");

                    if (IsIntegerSolution(solution))
                    {
                        Console.WriteLine("Integer solution found.");
                        _solutions.Add((solution, objectiveValue));
                    }
                    else
                    {
                        Console.WriteLine("Solution is not integer. Generating cut...");
                        GenerateCutAndSolve(solution, nodes);
                    }
                }

                Console.WriteLine("\nAll Integer Solutions Found:");
                foreach (var (sol, objVal) in _solutions)
                {
                    Console.WriteLine("Solution:");
                    for (int i = 0; i < sol.Length; i++)
                    {
                        Console.WriteLine($"x{i + 1} = {sol[i]:F2}");
                    }
                    Console.WriteLine($"Objective Value: {objVal:F2}");
                }
            }

            private string GetModelKey(LinearProgrammingModel model)
            {
                var keyBuilder = new StringBuilder();
                keyBuilder.Append(model.Objective.Coefficients.SequenceEqual(new double[] { }) ? "NoObjective" : string.Join(",", model.Objective.Coefficients));
                foreach (var constraint in model.Constraints)
                {
                    keyBuilder.Append(string.Join(",", constraint.Coefficients) + constraint.Relation + constraint.RHS + ";");
                }
                return keyBuilder.ToString();
            }

            private void DisplayModel(LinearProgrammingModel model)
            {
                var initializer = new InitializeTable(model);
                initializer.InitializeTableau();
                Console.WriteLine("Simplex Tableau:");
                initializer.PrintTableau();
            }

            private bool IsIntegerSolution(double[] solution)
            {
                return solution.All(val => Math.Abs(val - Math.Round(val)) < 1e-5);
            }

            private double ComputeObjectiveValue(double[] solution)
            {
                return solution.Zip(_model.Objective.Coefficients, (x, c) => x * c).Sum();
            }

        private void GenerateCutAndSolve(double[] solution, Queue<LinearProgrammingModel> nodes)
        {
            if (_optimalTableau == null || _optimalTableau.GetLength(0) == 0 || _optimalTableau.GetLength(1) == 0)
            {
                Console.WriteLine("Error: Optimal tableau is not initialized.");
                return;
            }

            int numRows = _optimalTableau.GetLength(0);
            int numCols = _optimalTableau.GetLength(1);

            Console.WriteLine($"Tableau Dimensions: {numRows}x{numCols}");

            if (solution.Length != numCols - 1)
            {
                Console.WriteLine("Error: Solution length is inconsistent with tableau dimensions.");
                return;
            }

            for (int i = 0; i < solution.Length; i++)
            {
                if (Math.Abs(solution[i] - Math.Round(solution[i])) >= 1e-5)
                {
                    double fractionalPart = solution[i] - Math.Floor(solution[i]);

                    var newConstraint = new Constraint
                    {
                        Coefficients = new List<double>(new double[numCols - 1]),
                        Relation = ">=",
                        RHS = -fractionalPart
                    };

                    for (int j = 0; j < numCols - 1; j++)
                    {
                        newConstraint.Coefficients[j] = -_optimalTableau[numRows - 1, j];
                    }

                    var newModel = new LinearProgrammingModel
                    {
                        Objective = _model.Objective,
                        Constraints = new List<Constraint>(_model.Constraints),
                        SignRestrictions = new List<string>(_model.SignRestrictions)
                    };

                    newModel.AddConstraint(newConstraint);

                    Console.WriteLine($"Cut applied on x{i + 1}: -x{i + 1} >= {-fractionalPart:F2}");
                    DisplayModel(newModel);

                    var simplexAlgorithm = _useDualSimplex ? (ISimplexAlgorithm)_dualSimplex : _primalSimplex;
                    simplexAlgorithm.UpdateModel(newModel);

                    var (newSolutionObj, isOptimal, newFinalTableau) = simplexAlgorithm.Solve();

                    // Validate the dimensions of the new tableau before proceeding
                    int expectedRows = numRows + 1; // New row for the added constraint
                    int expectedCols = numCols; // Number of columns should include slack variables if any

                    if (newFinalTableau.GetLength(0) == expectedRows && newFinalTableau.GetLength(1) >= expectedCols)
                    {
                        Console.WriteLine("Optimized Tableau after Adding Constraint:");
                        PrintTableau(newFinalTableau);

                        double[] newSolution = newSolutionObj as double[];
                        if (IsIntegerSolution(newSolution))
                        {
                            double newObjectiveValue = ComputeObjectiveValue(newSolution);
                            Console.WriteLine("Integer solution found.");
                            _solutions.Add((newSolution, newObjectiveValue));
                        }
                        else
                        {
                            nodes.Enqueue(newModel);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: New tableau dimensions do not match. Expected: {expectedRows}x{expectedCols}, Got: {newFinalTableau.GetLength(0)}x{newFinalTableau.GetLength(1)}");
                        return; // Exit the method as the dimensions don't match
                    }
                }
            }
        }


        public void PrintTableau(double[,] tableau)
            {
                int numRows = tableau.GetLength(0);
                int numCols = tableau.GetLength(1);

                Console.WriteLine("Simplex Tableau:");
                Console.Write("          ");
                for (int j = 0; j < numCols - 1; j++)
                {
                    Console.Write($"x{j + 1}       ");
                }
                Console.WriteLine("RHS");

                for (int i = 0; i < numRows; i++)
                {
                    Console.Write($" Row{i + 1} ");
                    for (int j = 0; j < numCols; j++)
                    {
                        Console.Write($"{tableau[i, j],-10:F2}");
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
        }
    }