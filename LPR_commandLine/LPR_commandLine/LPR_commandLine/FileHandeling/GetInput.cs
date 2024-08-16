using LPR_commandLine.Models;
namespace LPR_commandLine.FileHandeling
{
    public class FileParser
    {
        public static void AddBinaryConstraints(LinearProgrammingModel model)
        {
            int numVariables = model.Objective.Coefficients.Count;

            for (int i = 0; i < numVariables; i++)
            {
                if (model.SignRestrictions[i] == "bin")
                {
                    var binaryConstraint = new Constraint
                    {
                        Coefficients = new List<double>()
                    };

                    // Add 1 for the variable corresponding to this constraint
                    for (int j = 0; j < numVariables; j++)
                    {
                        binaryConstraint.Coefficients.Add(j == i ? 1 : 0);
                    }

                    // Add slack variable for this binary constraint
                    for (int j = 0; j < numVariables; j++)
                    {
                        binaryConstraint.Coefficients.Add(j == i ? 1 : 0);
                    }

                    binaryConstraint.Relation = "<=";
                    binaryConstraint.RHS = 1;

                    model.Constraints.Add(binaryConstraint);
                }
            }
        }

        public static LinearProgrammingModel ParseInputFile(string filePath)
        {
            var parser = new FileParser();
            var model = new LinearProgrammingModel();
            var lines = File.ReadAllLines(filePath).ToList();

            if (lines.Count < 3)
            {
                throw new FormatException("Input file is too short. Must contain at least an objective function, one constraint, and sign restrictions.");
            }

            // Parse the objective function
            var objectiveLine = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var objective = new ObjectiveFunction
            {
                Type = objectiveLine[0],
                Coefficients = new List<double>()
            };

            for (int i = 1; i < objectiveLine.Count; i++)
            {
                if (double.TryParse(objectiveLine[i], out double coefficient))
                {
                    objective.Coefficients.Add(coefficient);
                }
                else
                {
                    throw new FormatException("Invalid coefficient format in the objective function.");
                }
            }
            model.Objective = objective;

            // Initialize list for constraints
            model.Constraints = new List<Constraint>();

            // Parse constraints
            for (int i = 1; i < lines.Count - 1; i++)
            {
                var constraintLine = lines[i].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                //debuging Test
                //Console.WriteLine("Parsing constraint: " + string.Join(" ", constraintLine));

                var constraint = new Constraint
                {
                    Coefficients = new List<double>()
                };


                // Find index of the relation symbol
                int relationIndex = constraintLine.FindIndex(element => element.Trim() == "<=" || element.Trim() == ">=" || element.Trim() == "=");
                if (relationIndex == -1)
                {
                    Console.WriteLine("Error: Relation symbol not found in constraint: " + string.Join(" ", constraintLine));
                    throw new FormatException("Invalid constraint format.");
                }

                // Extract coefficients
                for (int j = 0; j < relationIndex; j++)
                {
                    if (double.TryParse(constraintLine[j], out double coefficient))
                    {
                        constraint.Coefficients.Add(coefficient);
                    }
                    else
                    {
                        throw new FormatException("Invalid coefficient format in constraint.");
                    }
                }

                // Extract relation and RHS
                constraint.Relation = constraintLine[relationIndex].Trim();
                if (relationIndex + 1 < constraintLine.Count)
                {
                    if (double.TryParse(constraintLine[relationIndex + 1], out double rhs))
                    {
                        constraint.RHS = rhs;
                    }
                    else
                    {
                        throw new FormatException("Invalid RHS format in constraint.");
                    }
                }
                model.Constraints.Add(constraint);
            }

            // Parse sign restrictions
            //var signRestrictionLine = lines.Last().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            //model.SignRestrictions = new List<string>(signRestrictionLine);

            var signRestrictionLine = lines.Last().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            model.SignRestrictions = new List<string>(signRestrictionLine);
            model.IsBinary = signRestrictionLine.Select(r => r == "bin").ToList();

            // Ensure all constraints and objective function have the same number of variables
            int maxVariables = model.Constraints.Max(c => c.Coefficients.Count);
            if (model.Objective.Coefficients.Count < maxVariables)
            {
                model.Objective.Coefficients.AddRange(Enumerable.Repeat(0.0, maxVariables - model.Objective.Coefficients.Count));
            }

            foreach (var constraint in model.Constraints)
            {
                if (constraint.Coefficients.Count < maxVariables)
                {
                    constraint.Coefficients.AddRange(Enumerable.Repeat(0.0, maxVariables - constraint.Coefficients.Count));
                }
            }

            AddBinaryConstraints(model);

            return model;
        }
    }
}
