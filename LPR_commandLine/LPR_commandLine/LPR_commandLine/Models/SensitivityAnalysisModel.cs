using LPR_commandLine.Models;
using LPR_commandLine.LPAlgorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR_commandLine.Models
{
    public class SensitivityAnalysisModel
    {
        public LinearProgrammingModel LpModel { get; set; }
        public double[,] OptimalTableau { get; set; }
        public List<int> BasicVariables { get; set; }
        public List<int> NonBasicVariables { get; set; }
        public List<Constraint> Constraints => LpModel.Constraints;
        public List<string> SignRestrictions => LpModel.SignRestrictions;

        public SensitivityAnalysisModel(LinearProgrammingModel lpModel, double[,] optimalTableau, List<int> basicVariables, List<int> nonBasicVariables)
        {
            LpModel = lpModel;
            OptimalTableau = optimalTableau ?? throw new ArgumentNullException(nameof(optimalTableau), "OptimalTableau cannot be null");
            BasicVariables = basicVariables ?? new List<int>();
            NonBasicVariables = nonBasicVariables ?? new List<int>();
        }

        public void UpdateOptimalTableau(double[,] newTableau)
        {
            OptimalTableau = newTableau;
        }

        public double[] GetShadowPrices()
        {
            int numRows = OptimalTableau.GetLength(0);
            int numColumns = OptimalTableau.GetLength(1);

            double[] shadowPrices = new double[numRows - 1];

            for (int i = 1; i < numRows; i++)
            {
                shadowPrices[i - 1] = OptimalTableau[i, numColumns - 1];
            }

            return shadowPrices;
        }

        public void AddNewActivity(double[] coefficients, double cost)
        {
            SensitivityAnalysis sa = new SensitivityAnalysis(this);
            SensitivityAnalysisModel updatedModel = sa.AddNewActivity(coefficients, cost);
            LpModel = updatedModel.LpModel;
            OptimalTableau = updatedModel.OptimalTableau;
            BasicVariables = updatedModel.BasicVariables;
            NonBasicVariables = updatedModel.NonBasicVariables;

            Console.WriteLine("New activity added. Updated Sensitivity Analysis Model:");
            sa.PrintUpdatedTableau();
        }

        private static double[,] GetOptimalTableauFromModel(LinearProgrammingModel model)
        {
            // Implement logic to extract the optimal tableau from the model
            return new double[0, 0]; // Placeholder
        }

        private static List<int> GetBasicVariablesFromModel(LinearProgrammingModel model)
        {
            // Implement logic to extract basic variables from the model
            return new List<int>(); // Placeholder
        }

        private static List<int> GetNonBasicVariablesFromModel(LinearProgrammingModel model)
        {
            // Implement logic to extract non-basic variables from the model
            return new List<int>(); // Placeholder
        }
    }
}
