using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }

        // Resize tableau with proper validation
        public void ResizeTableau(int numRows, int numCols)
        {
            if (Tableau == null)
            {
                Tableau = new double[numRows, numCols];
                return;
            }

            double[,] newTableau = new double[numRows, numCols];
            for (int i = 0; i < Math.Min(numRows, Tableau.GetLength(0)); i++)
            {
                for (int j = 0; j < Math.Min(numCols, Tableau.GetLength(1)); j++)
                {
                    newTableau[i, j] = Tableau[i, j];
                }
            }
            Tableau = newTableau;
        }



        // Add binary constraint and ensure list size
        public void AddBinaryConstraint(int variableIndex)
        {
            while (IsBinary.Count <= variableIndex)
            {
                IsBinary.Add(false);
            }
            IsBinary[variableIndex] = true;
        }

        // Add constraint and update tableau
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

        // Update tableau dimensions and content
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
        }



        // Apply cut to the model
        public void ApplyCut(Constraint cut)
        {
            AddConstraint(cut);
        }
    }

    public class ObjectiveFunction
    {
        public string Type { get; set; }
        public List<double> Coefficients { get; set; }

        public ObjectiveFunction()
        {
            Coefficients = new List<double>();
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
}