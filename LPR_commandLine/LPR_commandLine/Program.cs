using LPR_commandLine.FileHandeling;
using LPR_commandLine.LPAlgorithms;
using LPR_commandLine.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LPR381_Project
{

    internal class Program
    {
        public static string inputFile = @"C:\Users\walte\OneDrive - belgiumcampus.ac.za\STUDIES\3ThirdYear\LPR381 LinearPrograming\Project\LPR_commandLine\LPR_commandLine\LP.txt";
        private static LinearProgrammingModel? model = null;
        private static double[,]? _optimalTableau = null;
        private static List<int> _basicVariables = new List<int>();
        private static List<int> _nonBasicVariables = new List<int>();


        static void Main(string[] args)
        {

            ModelDisplay modelDisplay = new ModelDisplay();
            while (true)
            {
                Console.WriteLine("Menu:");
                Console.WriteLine("1. Load Model from File");
                Console.WriteLine("2. PrimalSimplex/DualSimplex");
                Console.WriteLine("3. Perform Sensitivity Analysis");
                Console.WriteLine("4. Branch and Bound");
                Console.WriteLine("5. Knapsack Branch and Bound");
                Console.WriteLine("6. Cutting Plane Algorithm");
                Console.WriteLine("7. Exit");
                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        LoadModel();
                        modelDisplay.DisplayConstraints(model);
                        DisplayTable();
                        DisplayInitialTable();
                        break;
                    case "2":
                        Console.Clear();
                        RunSimplexBasedOnModel();
                        break;
                    case "3":
                        Console.Clear();
                        PerformSensitivityAnalysis();
                        break;
                    case "4":
                        Console.Clear();
                        BranchAndBoundAlgorithm branchandbound = new BranchAndBoundAlgorithm(model);
                        branchandbound.RunBranchAndBound();
                        break;
                    case "5":
                        Console.Clear();
                        RunKnapsackBranchAndBound();
                        break;
                    case "6":
                        Console.Clear();
                        RunCuttingPlane();
                        break;
                    case "7":
                        return;
                    default:
                        Console.Clear();
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }
        private static void LoadModel()
        {
            Console.Clear();
            if (File.Exists(inputFile))
            {
                model = FileParser.ParseInputFile(inputFile);
                Console.WriteLine("Model loaded successfully.");
                Console.WriteLine("Objective: " + model.Objective.Type);
                Console.Write("z = ");
                for (int i = 0; i < model.Objective.Coefficients.Count; i++)
                {
                    if (i > 0)
                    {
                        Console.Write(" + ");
                    }
                    Console.Write($"{model.Objective.Coefficients[i]}x{i + 1}");
                }
                Console.WriteLine();

                Console.WriteLine("Constraints:");
                foreach (var constraint in model.Constraints)
                {
                    string constraintExpression = "";
                    for (int i = 0; i < constraint.Coefficients.Count; i++)
                    {
                        if (i > 0)
                        {
                            constraintExpression += " + ";
                        }
                        constraintExpression += $"{constraint.Coefficients[i]}x{i + 1}";
                    }
                    Console.WriteLine($"{constraintExpression} {constraint.Relation} {constraint.RHS}");
                }

                Console.WriteLine("Sign Restrictions: " + string.Join(", ", model.SignRestrictions));
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("File not found.");
            }
        }
        private static void DisplayTable()
        {
            if (model != null)
            {
                var primalTable = LoadTable(model);
                DisplayTable(model);
            }
            else
            {
                Console.WriteLine("Model not loaded.");
            }
            Console.WriteLine();
        }
        private static Table LoadTable(LinearProgrammingModel model)
        {
            Table primalTable = new Table();
            int numVariables = model.Objective.Coefficients.Count;
            // Generate var
            for (int i = 0; i < numVariables; i++)
            {
                primalTable.VariableNames.Add($"x{i + 1}");
            }
            //Add obj
            primalTable.ObjectiveCoefficients.AddRange(model.Objective.Coefficients);
            //Add const
            foreach (var constraint in model.Constraints)
            {
                primalTable.ConstraintCoefficients.Add(constraint.Coefficients);
                primalTable.Relations.Add(constraint.Relation);
                primalTable.RHS.Add(constraint.RHS);
            }
            primalTable.SignRestrictions.AddRange(model.SignRestrictions);

            return primalTable;
        }

        private static void DisplayTable(LinearProgrammingModel model)
        {
            const int colWidth = 8;
            Console.WriteLine("Primal Table:");
            Console.Write("".PadRight(colWidth));
            foreach (var varName in model.Objective.Coefficients.Select((_, i) => $"x{i + 1}"))
            {
                Console.Write($"{varName.PadRight(colWidth)}");
            }
            int slackIndex = 1;
            foreach (var constraint in model.Constraints)
            {
                if (constraint.Relation == "<=" || constraint.Relation == "=")
                {
                    Console.Write($"s{slackIndex++}".PadRight(colWidth));
                }
            }
            Console.WriteLine($"{"|".PadLeft(3)} {"RHS".PadRight(colWidth)}");
            Console.Write("z".PadRight(colWidth));
            foreach (var coef in model.Objective.Coefficients)
            {
                Console.Write($"{coef.ToString().PadRight(colWidth)}");
            }
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                if (model.Constraints[i].Relation == "<=" || model.Constraints[i].Relation == "=")
                {
                    Console.Write($"{0.ToString().PadRight(colWidth)}");
                }
            }

            Console.WriteLine();

            for (int i = 0; i < model.Constraints.Count; i++)
            {
                Console.Write($"c{i + 1}".PadRight(colWidth));
                var constraint = model.Constraints[i];
                foreach (var coef in constraint.Coefficients)
                {
                    Console.Write($"{coef.ToString().PadRight(colWidth)}");
                }

                // s/e
                if (constraint.Relation == "<=")
                {
                    Console.Write($"1".PadRight(colWidth));
                }
                else if (constraint.Relation == ">=")
                {
                    Console.Write($"-1".PadRight(colWidth));
                }
                else if (constraint.Relation == "=")
                {
                    Console.Write($"1".PadRight(colWidth)); // s
                    Console.Write($"-1".PadRight(colWidth)); //e
                }

                Console.WriteLine($"{"|".PadLeft(3)} {constraint.RHS.ToString().PadRight(colWidth)}");
            }

            Console.WriteLine();
            for (int i = 0; i < model.IsBinary.Count; i++)
            {
                if (model.IsBinary[i])
                {
                    Console.WriteLine($"{model.Objective.Coefficients.Select((_, i) => $"x{i + 1}").ElementAt(i).PadRight(colWidth)}= bin");
                }
            }
        }





        private static void DisplayInitialTable()
        {
            if (model != null)
            {
                var simplex = new InitializeTable(model);
                simplex.InitializeTableau();
                simplex.PrintTableau();
            }
            else
            {
                Console.WriteLine("Model not loaded.");
            }
        }
        private static void RunSimplexBasedOnModel()
        {
            if (model != null)
            {
                // Initialize the tableau
                InitializeTable simplexTable = new InitializeTable(model);
                simplexTable.InitializeTableau();
                _optimalTableau = simplexTable.Tableau;

                // Debugging
                Console.WriteLine("Initial Tableau:");
                simplexTable.PrintTableau();

                // Extract the RHS column for checking
                var rhsValues = new List<double>();
                for (int i = 0; i < simplexTable.Tableau.GetLength(0); i++)
                {
                    rhsValues.Add(simplexTable.Tableau[i, simplexTable.Tableau.GetLength(1) - 1]);
                }

                // Debugging: Print RHS values
                Console.WriteLine("RHS Values:");
                foreach (var value in rhsValues)
                {
                    Console.WriteLine(value);
                }

                bool useDualSimplex = rhsValues.Any(value => value < 0);
                Console.WriteLine($"Use Dual Simplex: {useDualSimplex}");

                if (useDualSimplex)
                {
                    Console.WriteLine("Running Dual Simplex...");
                    DualSimplex dsimplex = new DualSimplex(model);
                    dsimplex.Solve();
                }
                else
                {
                    Console.WriteLine("Running Primal Simplex...");
                    PrimalSimplex simplex = new PrimalSimplex(model);
                    simplex.Solve();
                }
            }
            else
            {
                Console.WriteLine("Model not loaded.");
            }
        }

        


        private static void RunKnapsackBranchAndBound()
        {
            if (model != null)
            {
                Console.WriteLine("Running Knapsack Branch and Bound...");
                knapsackBranchAndBound knapsackBranchAndBound = new knapsackBranchAndBound();
                knapsackBranchAndBound.SolveKnapsackBranchAndBound(model);
            }
            else
            {
                Console.WriteLine("Model not loaded.");
            }
        }

        //  private static ISimplexAlgorithm simplexAlgorithm = new PrimalSimplex(); // Use your actual implementation of ISimplexAlgorithm

        private static void RunCuttingPlane()
        {
            if (model != null)
            {
                try
                {
                    // Initialize the tableau dimensions
                    int numRows = model.NumConstraints + 1; // Include objective row
                    int numCols = model.NumVariables + model.NumConstraints + 1; // Include RHS column
                    model.InitializeTableau(numRows, numCols);

                    // Update the tableau with initial values
                    model.UpdateTableau();

                    // Retrieve and set the initial tableau
                    double[,] initialTableau = model.GetTableau();
                    model.SetInitialTableau(initialTableau);

                    // Debugging: Print the initial tableau
                    Console.WriteLine("Initial Tableau:");
                    PrintTableau(initialTableau);

                    // Initialize Simplex and Dual Simplex algorithms
                    PrimalSimplex simplex = new PrimalSimplex(model);
                    DualSimplex dualSimplex = new DualSimplex(model);

                    // Initialize and solve using the Cutting Plane algorithm
                    CuttingPlane cuttingPlane = new CuttingPlane(model, simplex, dualSimplex);
                    cuttingPlane.Solve();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error running Cutting Plane Algorithm: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Model or Simplex Algorithm not loaded.");
            }
        }

        // Helper method to print the tableau
        private static void PrintTableau(double[,] tableau)
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


        private static void PerformSensitivityAnalysis()
        {
            var lpModel = model;
            var optimalTableau = _optimalTableau; 
            var basicVariables = _basicVariables; 
            var nonBasicVariables = _nonBasicVariables; 

            SensitivityAnalysisModel saModel = new SensitivityAnalysisModel(lpModel, optimalTableau, basicVariables, nonBasicVariables);
            SensitivityAnalysis sa = new SensitivityAnalysis(saModel);

            if (model != null)
            {


                while (true)
                {
                    Console.Clear();
                    Console.WriteLine("Performing Sensitivity Analysis...");
                    Console.WriteLine("Sensitivity Analysis Menu:");
                    Console.WriteLine("1. Display Range of a Non-Basic Variable");
                    Console.WriteLine("2. Apply Change to a Non-Basic Variable");
                    Console.WriteLine("3. Display Range of a Basic Variable");
                    Console.WriteLine("4. Apply Change to a Basic Variable");
                    Console.WriteLine("5. Display Range of a Constraint RHS");
                    Console.WriteLine("6. Apply Change to a Constraint RHS");
                    Console.WriteLine("7. Display Range of a Variable in a Non-Basic Column");
                    Console.WriteLine("8. Apply Change to a Variable in a Non-Basic Column");
                    Console.WriteLine("9. Add New Activity");
                    Console.WriteLine("10. Add New Constraint");
                    Console.WriteLine("11. Display Shadow Prices");
                    Console.WriteLine("12. Apply Duality");
                    Console.WriteLine("13. Solve Dual Programming Model");
                    Console.WriteLine("14. Verify Strong or Weak Duality");
                    Console.WriteLine("15. Return to Main Menu");

                    var choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            DisplayRangeOfNonBasicVariable(sa);
                            break;
                        case "2":
                            ApplyChangeToNonBasicVariable();
                            break;
                        case "3":
                            DisplayRangeOfBasicVariable(sa);
                            break;
                        case "4":
                            ApplyChangeToBasicVariable();
                            break;
                        case "5":
                            DisplayRangeOfConstraintRHS(sa);
                            break;
                        case "6":
                            ApplyChangeToConstraintRHS();
                            break;
                        case "7":
                            DisplayRangeOfVariableInNonBasicColumn(sa);
                            break;
                        case "8":
                            ApplyChangeToVariableInNonBasicColumn();
                            break;
                        case "9":
                            AddNewActivity(sa);
                            break;
                        case "10":
                            AddNewConstraint(sa);
                            break;
                        case "11":
                            DisplayShadowPrices(sa);
                            break;
                        case "12":
                            ApplyDuality(sa);
                            break;
                        case "13":
                            SolveDualProgrammingModel(sa);
                            break;
                        case "14":
                            VerifyDuality(sa);
                            break;
                        case "15":
                            return; 
                        default:
                            Console.WriteLine("Invalid choice. Please try again.");
                            break;
                    }

                    Console.WriteLine("Press any key to return to the Sensitivity Analysis Menu...");
                    Console.ReadKey();
                }
            }
            else
            {
                Console.WriteLine("Model not loaded.");
            }
        }


        private static void DisplayRangeOfNonBasicVariable(SensitivityAnalysis sa)
        {
            Console.WriteLine("Enter the index of the non-basic variable:");
            if (int.TryParse(Console.ReadLine(), out int variableIndex))
            {
                try
                {
                    double[] range = sa.GetNonBasicVariableRange(variableIndex);
                    Console.WriteLine($"Range for non-basic variable at index {variableIndex}:");
                    Console.WriteLine($"Lower Bound: {range[0]}");
                    Console.WriteLine($"Upper Bound: {range[1]}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter a valid integer index.");
            }
        }

        private static void ApplyChangeToNonBasicVariable()
        {
            if (model != null)
            {
                Console.WriteLine("Enter the index of the non-basic binary variable to change:");

                if (int.TryParse(Console.ReadLine(), out int variableIndex))
                {
                    //debuging
                    Console.WriteLine("Non-Basic Variables: " + string.Join(", ", _nonBasicVariables));

                    if (variableIndex >= 0 && variableIndex < _nonBasicVariables.Count)
                    {
                        Console.WriteLine("Enter the new value for the binary variable (0 or 1):");

                        if (int.TryParse(Console.ReadLine(), out int newValue) && (newValue == 0 || newValue == 1))
                        {
                            try
                            {
                                var sa = new SensitivityAnalysis(new SensitivityAnalysisModel(model, _optimalTableau, _basicVariables, _nonBasicVariables));

                                int actualVariableIndex = _nonBasicVariables[variableIndex];
                                sa.ApplyNonBasicVariableChange(actualVariableIndex, newValue);

                                RunSimplexBasedOnModel();
                                Console.WriteLine("Change applied successfully and model updated.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error applying change: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid value. Please enter 0 or 1.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid index. Please enter a valid index for the non-basic binary variable.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a numeric index.");
                }
            }
            else
            {
                Console.WriteLine("Model not loaded.");
            }
        }

        private static void DisplayRangeOfBasicVariable(SensitivityAnalysis sa)
        {
            Console.WriteLine("Enter the index of the basic variable:");
            if (int.TryParse(Console.ReadLine(), out int variableIndex))
            {
                try
                {
                    double[] range = sa.GetBasicVariableRange(variableIndex);
                    if (range[0] == double.MinValue || range[1] == double.MaxValue)
                    {
                        Console.WriteLine("Variable index is out of bounds or has not been computed.");
                    }
                    else
                    {
                        // Display the range
                        Console.WriteLine($"Range for basic variable at index {variableIndex}:");
                        Console.WriteLine($"Lower Bound: {range[0]}");
                        Console.WriteLine($"Upper Bound: {range[1]}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter a valid integer index.");
            }
        }


        private static void ApplyChangeToBasicVariable()
        {

        }

        private static void DisplayRangeOfConstraintRHS(SensitivityAnalysis sa)
        {
            if (model != null && model.Constraints.Any())
            {
                Console.WriteLine("Displaying the range of right-hand side (RHS) values for constraints:");
                for (int i = 0; i < model.Constraints.Count; i++)
                {
                    var constraint = model.Constraints[i];
                    Console.WriteLine($"Constraint {i + 1}: RHS = {constraint.RHS}");
                    Console.WriteLine($"Enter new RHS value for constraint {i + 1} (or type 'skip' to leave unchanged):");
                    string input = Console.ReadLine();

                    if (input.ToLower() != "skip" && double.TryParse(input, out double newRhsValue))
                    {
                        try
                        {
                            model.Constraints[i].RHS = newRhsValue;
                            Console.WriteLine($"Constraint {i + 1} updated to new RHS value: {newRhsValue}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error updating constraint RHS: {ex.Message}");
                        }
                    }
                    else if (input.ToLower() != "skip")
                    {
                        Console.WriteLine("Invalid input. Please enter a valid number or 'skip'.");
                    }
                }
            }
            else
            {
                Console.WriteLine("No constraints found in the model or model not loaded.");
            }
        }

        private static void ApplyChangeToConstraintRHS()
        {

        }

        private static void DisplayRangeOfVariableInNonBasicColumn(SensitivityAnalysis sa)
        {
            if (model == null || model.Tableau == null || model.Tableau.GetLength(1) == 0)
            {
                Console.WriteLine("Model or tableau is not initialized.");
                return;
            }

            Console.WriteLine("Displaying the range of non-basic variables in the tableau:");
            Console.WriteLine("Enter the index of the non-basic variable:");

            if (int.TryParse(Console.ReadLine(), out int variableIndex))
            {
                try
                {
                    double[] range = sa.GetNonBasicVariableRange(variableIndex);
                    Console.WriteLine($"Range for non-basic variable at index {variableIndex}:");
                    Console.WriteLine($"Lower Bound: {range[0]}");
                    Console.WriteLine($"Upper Bound: {range[1]}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter a valid integer index.");
            }
        }

        private static void ApplyChangeToVariableInNonBasicColumn()
        {

        }
        private static void DisplayCurrentModel(SensitivityAnalysis sa)
        {
           
            Console.WriteLine("Objective Function:");
            Console.WriteLine("z = " + string.Join(" + ", sa._model.LpModel.Objective.Coefficients.Select((c, i) => $"{c}x{i + 1}")));

            Console.WriteLine("Constraints:");
            foreach (var constraint in sa._model.LpModel.Constraints)
            {
                string constraintExpression = string.Join(" + ", constraint.Coefficients.Select((c, i) => $"{c}x{i + 1}"));
                Console.WriteLine($"{constraintExpression} {constraint.Relation} {constraint.RHS}");
            }

            Console.WriteLine("Sign Restrictions: " + string.Join(", ", sa._model.LpModel.SignRestrictions));
            Console.WriteLine();
        }

        private static void AddNewActivity(SensitivityAnalysis sa)
        {
            int numVariables = sa.GetNumberOfVariables(); 
            // Prompt
            Console.WriteLine($"The current number of variables is {numVariables}. Please enter the number of coefficients.");

            int numCoefficients;
            while (true)
            {
                Console.Write("Enter the number of coefficients for the new activity: ");
                if (int.TryParse(Console.ReadLine(), out numCoefficients) && numCoefficients == numVariables)
                {
                    break;
                }
                else
                {
                    Console.WriteLine($"Invalid input. You must enter {numVariables} coefficients.");
                }
            }
            double[] coefficients = new double[numCoefficients];
            for (int i = 0; i < numCoefficients; i++)
            {
                while (true)
                {
                    Console.Write($"Enter coefficient {i + 1}: ");
                    if (double.TryParse(Console.ReadLine(), out double coefficient))
                    {
                        coefficients[i] = coefficient;
                        break; 
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please enter a valid number.");
                    }
                }
            }
            double cost;
            while (true)
            {
                Console.Write("Enter the cost for the new activity: ");
                if (double.TryParse(Console.ReadLine(), out cost))
                {
                    break; 
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a valid number.");
                }
            }
            sa.AddNewActivity(coefficients, cost);

            DisplayCurrentModel(sa);
        }


        private static void AddNewConstraint(SensitivityAnalysis sa)
        {
            Console.Write("Enter the number of coefficients for the new constraint: ");
            int numCoefficients = Convert.ToInt32(Console.ReadLine());

            double[] coefficients = new double[numCoefficients];
            for (int i = 0; i < numCoefficients; i++)
            {
                Console.Write($"Enter coefficient {i + 1}: ");
                coefficients[i] = Convert.ToDouble(Console.ReadLine());
            }

            Console.Write("Enter the RHS value of the constraint: ");
            double rhs = Convert.ToDouble(Console.ReadLine());
            sa.AddNewConstraint(coefficients, rhs);

            DisplayCurrentModel(sa);
        }


        private static void DisplayShadowPrices(SensitivityAnalysis sa)
        {
            if (model == null || model.Tableau == null || model.Tableau.GetLength(0) == 0)
            {
                Console.WriteLine("Model or tableau is not initialized.");
                return;
            }
            int numConstraints = model.NumConstraints;
            int numVariables = model.NumVariables;

            Console.WriteLine("Shadow Prices for Constraints:");

            for (int i = 0; i < numConstraints; i++)
            {
                double shadowPrice = model.Tableau[numVariables, i];
                Console.WriteLine($"Constraint {i + 1}: Shadow Price = {shadowPrice}");
            }
        }

        private static void ApplyDuality(SensitivityAnalysis sa)
        {
            if (sa != null)
            {
                sa.ApplyDuality();
                Console.WriteLine("Duality has been applied successfully.");
            }
            else
            {
                Console.WriteLine("Sensitivity Analysis model is not initialized.");
            }
        }


        private static void SolveDualProgrammingModel(SensitivityAnalysis sa)
        {
            if (sa != null)
            {
                try
                {
                    sa.SolveDualModel();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error solving dual programming model: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Sensitivity Analysis model is not initialized.");
            }
        }




        private static void VerifyDuality(SensitivityAnalysis sa)
        {
            var (isVerified, dualityType) = sa.VerifyDuality();

            if (isVerified)
            {
                Console.WriteLine($"Duality verified: {dualityType}");
                // Apply duality and solve the dual programming model
                ApplyDuality(sa);
                SolveDualProgrammingModel(sa);
            }
            else
            {
                Console.WriteLine($"Duality cannot be verified. Reason: {dualityType}");
            }
        }
    }
}