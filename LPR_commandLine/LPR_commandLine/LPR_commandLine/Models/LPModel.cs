namespace LPR_commandLine.Models
{
    public class LinearProgrammingModel
    {
        public ObjectiveFunction Objective { get; set; }
        public List<Constraint> Constraints { get; set; }
        public List<string> SignRestrictions { get; set; }
        public double[,] Tableau { get; set; }
        public List<bool> IsBinary { get; set; }
        public int NumVariables => Objective.Coefficients.Count;
        public int NumConstraints => Constraints.Count;


        public LinearProgrammingModel()
        {
            Constraints = new List<Constraint>();
            SignRestrictions = new List<string>();
            IsBinary = new List<bool>();
            Tableau = null;
        }
        public void InitializeFromTable(Table table)
        {
            // Initialize Objective Function
            Objective = new ObjectiveFunction
            {
                Type = table.ObjectiveCoefficients.Any(x => x < 0) ? "Maximize" : "Minimize",
                Coefficients = new List<double>(table.ObjectiveCoefficients)
            };

            // Initialize Constraints
            Constraints.Clear();
            for (int i = 0; i < table.ConstraintCoefficients.Count; i++)
            {
                var constraint = new Constraint
                {
                    Coefficients = new List<double>(table.ConstraintCoefficients[i]),
                    Relation = table.Relations[i],
                    RHS = table.RHS[i]
                };
                Constraints.Add(constraint);
            }

            // Initialize Sign Restrictions
            SignRestrictions = new List<string>(table.SignRestrictions);

            // Initialize Binary Variables
            IsBinary = new List<bool>(table.IsBinary);

            // Initialize the Tableau
            int numRows = Constraints.Count + 1; // One row for each constraint + the objective function
            int numCols = Objective.Coefficients.Count + Constraints.Count + 1; // Variables + Slack/Surplus variables + RHS
            InitializeTableau(numRows, numCols);

            // Populate the Tableau with the initial data
            UpdateTableau();
        }
        public Table ToTable()
        {
            var table = new Table();

            // Populate Objective Function
            table.ObjectiveCoefficients = new List<double>(Objective.Coefficients);

            // Populate Constraints
            foreach (var constraint in Constraints)
            {
                table.ConstraintCoefficients.Add(new List<double>(constraint.Coefficients));
                table.Relations.Add(constraint.Relation);
                table.RHS.Add(constraint.RHS);
            }

            // Populate Sign Restrictions
            table.SignRestrictions = new List<string>(SignRestrictions);

            // Populate Binary Variables
            table.IsBinary = new List<bool>(IsBinary);

            return table;
        }


        public void InitializeTableau(int numRows, int numCols)
        {
            Tableau = new double[numRows, numCols];
            // Additional logic to populate the tableau with initial values
        }

        public double[,] GetTableau()
        {
            return Tableau;
        }

        public void SetInitialTableau(double[,] initialTableau)
        {
            Tableau = initialTableau;
        }
        public double[,] CopyOptimalTableau()
        {
            if (Tableau == null)
            {
                throw new InvalidOperationException("Optimal tableau is not initialized.");
            }

            int numRows = Tableau.GetLength(0);
            int numCols = Tableau.GetLength(1);

            // Create a new tableau with the same dimensions
            double[,] copiedTableau = new double[numRows, numCols];

            // Copy the contents of the original tableau
            Array.Copy(Tableau, copiedTableau, Tableau.Length);

            return copiedTableau;
        }
        public void ResizeTableau(int numRows, int numCols)
        {
            if (Tableau == null || numRows > Tableau.GetLength(0) || numCols > Tableau.GetLength(1))
            {
                double[,] newTableau = new double[numRows, numCols];
                if (Tableau != null)
                {
                    int rowsToCopy = Math.Min(numRows, Tableau.GetLength(0));
                    int colsToCopy = Math.Min(numCols, Tableau.GetLength(1));
                    for (int i = 0; i < rowsToCopy; i++)
                    {
                        for (int j = 0; j < colsToCopy; j++)
                        {
                            newTableau[i, j] = Tableau[i, j];
                        }
                    }
                }
                Tableau = newTableau;
            }
        }

        public void AddBinaryConstraint(int variableIndex)
        {
            while (IsBinary.Count <= variableIndex)
            {
                IsBinary.Add(false);
            }
            IsBinary[variableIndex] = true;
        }

        public void AddConstraint(double[] coefficients, double rhs, string relation)
        {
            Constraints.Add(new Constraint
            {
                Coefficients = coefficients.ToList(),
                RHS = rhs,
                Relation = relation
            });
        }

        public void AddConstraint(Constraint constraint)
        {
            Constraints.Add(constraint);
        }

        public LinearProgrammingModel CloneModel()
        {
            var clonedModel = new LinearProgrammingModel
            {
                Objective = new ObjectiveFunction
                {
                    Type = this.Objective.Type,
                    Coefficients = new List<double>(this.Objective.Coefficients)
                },
                Constraints = this.Constraints.Select(c => new Constraint
                {
                    Coefficients = new List<double>(c.Coefficients),
                    RHS = c.RHS,
                    Relation = c.Relation
                }).ToList(),
                SignRestrictions = new List<string>(this.SignRestrictions),
                IsBinary = new List<bool>(this.IsBinary)
            };

            if (this.Tableau != null)
            {
                int rows = this.Tableau.GetLength(0);
                int cols = this.Tableau.GetLength(1);
                clonedModel.Tableau = new double[rows, cols];
                Array.Copy(this.Tableau, clonedModel.Tableau, this.Tableau.Length);
            }

            return clonedModel;
        }

        public void UpdateTableau()
        {
            int numRows = Constraints.Count + 1; // Constraints + Objective function
            int numCols = Objective.Coefficients.Count + Constraints.Count + 1; // Variables + Slack/Surplus variables + RHS

            ResizeTableau(numRows, numCols);

            // Set objective function row
            for (int j = 0; j < Objective.Coefficients.Count; j++)
            {
                Tableau[0, j] = Objective.Coefficients[j];
            }

            // Set constraints in the tableau
            for (int i = 1; i < numRows; i++)
            {
                for (int j = 0; j < Constraints[i - 1].Coefficients.Count; j++)
                {
                    Tableau[i, j] = Constraints[i - 1].Coefficients[j];
                }
                Tableau[i, numCols - 1] = Constraints[i - 1].RHS; // RHS column
            }

            // Add identity matrix for slack/surplus variables
            for (int i = 1; i < numRows; i++)
            {
                Tableau[i, Objective.Coefficients.Count + i - 1] = 1;
            }

            // Debugging: Print the tableau after updating
            Console.WriteLine("Tableau After Update:");
            PrintTableau(Tableau);
        }

        public void ApplyCut(Constraint cut)
        {
            Constraints.Add(cut);
            AddConstraintToOptimalTableau(cut);
        }

        private void AddConstraintToOptimalTableau(Constraint newConstraint)
        {
            if (Tableau == null)
            {
                throw new InvalidOperationException("Tableau has not been initialized.");
            }

            int numRows = Tableau.GetLength(0) + 1; // Add one row for the new constraint
            int numCols = Tableau.GetLength(1) + 1; // Add one column for the slack variable or other variables

            ResizeTableau(numRows, numCols);

            int newRowIndex = numRows - 1; // Index of the new row

            // Add the new constraint coefficients to the tableau
            for (int j = 0; j < newConstraint.Coefficients.Count; j++)
            {
                Tableau[newRowIndex, j] = newConstraint.Coefficients[j];
            }

            // Add the slack variable (if applicable)
            int slackVarIndex = Objective.Coefficients.Count + Constraints.Count - 1;
            if (slackVarIndex < numCols)
            {
                Tableau[newRowIndex, slackVarIndex] = 1;
            }

            // Set the RHS value
            Tableau[newRowIndex, numCols - 1] = newConstraint.RHS;

            // Debugging: Print the tableau after adding the constraint
            Console.WriteLine("Tableau After Adding Constraint:");
            PrintTableau(Tableau);
        }

        public void PrintTableau(double[,] tableau)
        {
            if (tableau == null)
            {
                Console.WriteLine("Tableau is null.");
                return;
            }

            int numRows = tableau.GetLength(0);
            int numCols = tableau.GetLength(1);

            Console.WriteLine("Current Tableau:");
            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    Console.Write($"{tableau[i, j],10:F2} "); // Format to 2 decimal places
                }
                Console.WriteLine();
            }
        }

        public void PrintConstraints()
        {
            if (Constraints == null || Constraints.Count == 0)
            {
                Console.WriteLine("No constraints available.");
                return;
            }

            Console.WriteLine("Current Constraints:");
            for (int i = 0; i < Constraints.Count; i++)
            {
                var c = Constraints[i];
                Console.WriteLine($"Constraint {i + 1}: " +
                    $"{string.Join(" ", c.Coefficients.Select(x => x.ToString("F2")))} " +
                    $"{c.Relation} {c.RHS:F2}");
            }
        }
        private Constraint TransformConstraint(double[,] tableau, int constraintIndex)
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

            // Handle negative decimals
            for (int j = 0; j < numCols - 1; j++)
            {
                if (decimalCoefficients[j] < 0)
                {
                    decimalCoefficients[j] = 1 + decimalCoefficients[j];
                }
            }

            // Remove integer parts
            for (int j = 0; j < numCols - 1; j++)
            {
                coefficients[j] = decimalCoefficients[j];
            }
            decimalRhs = decimalRhs < 0 ? 1 + decimalRhs : decimalRhs;

            // Multiply by -1
            for (int j = 0; j < numCols - 1; j++)
            {
                coefficients[j] = -coefficients[j];
            }
            rhs = -decimalRhs;

            // Create the new constraint
            Constraint newConstraint = new Constraint
            {
                Coefficients = coefficients.ToList(),
                Relation = "<=",
                RHS = rhs
            };

            return newConstraint;
        }
    }



}

public class ObjectiveFunction
{
    public string Type { get; set; } // "Minimize" or "Maximize"
    public List<double> Coefficients { get; set; }

    public ObjectiveFunction()
    {
        Coefficients = new List<double>();
    }

    public ObjectiveFunction(string type, double[] coefficients)
    {
        Type = type;
        Coefficients = new List<double>(coefficients);
    }
}

public class Constraint
{
    public List<double> Coefficients { get; set; }
    public string Relation { get; set; }
    public double RHS { get; set; }

    public Constraint()
    {
        Coefficients = new List<double>();
    }
    public override bool Equals(object obj)
    {
        if (obj is Constraint other)
        {
            return Coefficients.SequenceEqual(other.Coefficients) && RHS == other.RHS && Relation == other.Relation;
        }
        return false;
    }

    public override int GetHashCode()
    {
        // Generate a hash code based on coefficients, RHS, and relation
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + RHS.GetHashCode();
            hash = hash * 23 + Relation.GetHashCode();
            foreach (var coef in Coefficients)
            {
                hash = hash * 23 + coef.GetHashCode();
            }
            return hash;
        }
    }

    public void Transform()
    {
        for (int i = 0; i < Coefficients.Count; i++)
        {
            double coefficient = Coefficients[i];
            int intPart = (int)coefficient;
            double decimalPart = coefficient - intPart;

            if (decimalPart < 0)
            {
                decimalPart = 1 - decimalPart;
            }

            Coefficients[i] = -decimalPart; // Apply sign inversion
        }

        RHS = -RHS; // Apply sign inversion and move to left side
    }
}

public class Table
{
    public List<string> VariableNames { get; set; } = new List<string>();
    public List<double> ObjectiveCoefficients { get; set; } = new List<double>();
    public List<List<double>> ConstraintCoefficients { get; set; } = new List<List<double>>();
    public List<string> Relations { get; set; } = new List<string>();
    public List<double> RHS { get; set; } = new List<double>();
    public List<string> SignRestrictions { get; set; } = new List<string>();
    public List<bool> IsBinary { get; set; } = new List<bool>();

    public Table()
    {
        VariableNames = new List<string>();
        ObjectiveCoefficients = new List<double>();
        ConstraintCoefficients = new List<List<double>>();
        Relations = new List<string>();
        RHS = new List<double>();
        SignRestrictions = new List<string>();
        IsBinary = new List<bool>();
    }
}
