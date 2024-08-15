using LPR_commandLine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LPR_commandLine.LPAlgorithms
{
    public class BranchAndBoundAlgorithm
    {
        private LinearProgrammingModel _model;
        private ISimplexAlgorithm _simplexAlgorithm;

        public BranchAndBoundAlgorithm(LinearProgrammingModel model)
        {
            _model = model;
            _simplexAlgorithm = InitializeSimplexAlgorithm();
        }

        public void RunBranchAndBound()
        {
            var solutions = new List<(double[] Solution, double ObjectiveValue)>();
            var nodes = new Queue<LinearProgrammingModel>();
            var visitedModels = new HashSet<string>(); // To avoid revisiting the same model
            nodes.Enqueue(_model);

            while (nodes.Count > 0)
            {
                var currentModel = nodes.Dequeue();
                var modelKey = GetModelKey(currentModel); // Generate a unique key for the model
                if (visitedModels.Contains(modelKey))
                {
                    continue; // Skip already visited models
                }
                visitedModels.Add(modelKey);

                Console.WriteLine("\nProcessing Node:");
                DisplayModel(currentModel);

                // Update the simplex algorithm with the current model
                _simplexAlgorithm.UpdateModel(currentModel);

                // Solve the LP relaxation for the current node
                var (solutionObj, isOptimal, finalTableau) = _simplexAlgorithm.Solve();

                if (!isOptimal)
                {
                    Console.WriteLine("The problem is unbounded or infeasible.");
                    continue;
                }

                // Cast the solution object to double[]
                double[] solution = solutionObj as double[];
                if (solution == null)
                {
                    Console.WriteLine("Error: The solution is not of the expected type.");
                    continue;
                }

                // Compute and display the objective value
                double objectiveValue = ComputeObjectiveValue(solution);
                Console.WriteLine("Solution:");
                for (int i = 0; i < solution.Length; i++)
                {
                    Console.WriteLine($"x{i + 1} = {solution[i]:F2}");
                }
                Console.WriteLine($"Objective Value: {objectiveValue:F2}");

                // Check if the solution is integer
                if (IsIntegerSolution(solution))
                {
                    Console.WriteLine("Integer solution found.");
                    solutions.Add((solution, objectiveValue));
                }
                else
                {
                    // Generate and add branches
                    Console.WriteLine("Solution is not integer. Branching...");
                    GenerateBranches(solution, nodes);
                }
            }

            // Display all integer solutions found
            Console.WriteLine("\nAll Integer Solutions Found:");
            foreach (var (sol, objVal) in solutions)
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
            // Generate a unique key for the model based on its constraints and objective
            var keyBuilder = new StringBuilder();
            keyBuilder.Append(model.Objective.Coefficients.SequenceEqual(new double[] { }) ? "NoObjective" : string.Join(",", model.Objective.Coefficients));
            foreach (var constraint in model.Constraints)
            {
                keyBuilder.Append(string.Join(",", constraint.Coefficients) + constraint.Relation + constraint.RHS + ";");
            }
            return keyBuilder.ToString();
        }

        private ISimplexAlgorithm InitializeSimplexAlgorithm()
        {
            var initializer = new InitializeTable(_model);
            initializer.InitializeTableau();
            double[] rhsValues = GetRHSValues(initializer.Tableau);

            bool useDualSimplex = rhsValues.Any(value => value < 0);
            Console.WriteLine($"Use Dual Simplex: {useDualSimplex}");

            if (useDualSimplex)
            {
                Console.WriteLine("Using Dual Simplex Method.");
                return new DualSimplex(_model); // Ensure DualSimplex class is implemented
            }
            else
            {
                Console.WriteLine("Using Primal Simplex Method.");
                return new PrimalSimplex(_model);
            }
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

        private void GenerateBranches(double[] solution, Queue<LinearProgrammingModel> nodes)
        {
            for (int i = 0; i < solution.Length; i++)
            {
                if (Math.Abs(solution[i] - Math.Round(solution[i])) >= 1e-5)
                {
                    double floorValue = Math.Floor(solution[i]);
                    double ceilValue = Math.Ceiling(solution[i]);

                    var model1 = CreateSubproblemWithConstraint(i, floorValue, "<=");
                    nodes.Enqueue(model1);
                    Console.WriteLine($"Branching with x{i + 1} <= {floorValue}");

                    var model2 = CreateSubproblemWithConstraint(i, ceilValue, ">=");
                    nodes.Enqueue(model2);
                    Console.WriteLine($"Branching with x{i + 1} >= {ceilValue}");

                    break;
                }
            }
        }

        private LinearProgrammingModel CreateSubproblemWithConstraint(int variableIndex, double bound, string relation)
        {
            var newModel = new LinearProgrammingModel
            {
                Objective = _model.Objective,
                Constraints = new List<Constraint>(_model.Constraints),
                SignRestrictions = new List<string>(_model.SignRestrictions)
            };

            var constraint = new Constraint
            {
                Coefficients = Enumerable.Range(0, variableIndex).Select(_ => 0.0).Concat(new[] { 1.0 }).Concat(Enumerable.Range(variableIndex + 1, newModel.NumVariables - variableIndex - 1).Select(_ => 0.0)).ToList(),
                Relation = relation,
                RHS = bound
            };
            newModel.AddConstraint(constraint);

            return newModel;
        }

        private double[] GetRHSValues(double[,] tableau)
        {
            int numRows = tableau.GetLength(0);
            int numCols = tableau.GetLength(1);
            double[] rhsValues = new double[numRows - 1];
            for (int i = 1; i < numRows; i++)
            {
                rhsValues[i - 1] = tableau[i, numCols - 1];
            }
            return rhsValues;
        }
    }
}


