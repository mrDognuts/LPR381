using LPR_commandLine.LPAlgorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR_commandLine.Models
{
    public interface ISimplexAlgorithm
    {
        void UpdateModel(LinearProgrammingModel model);
        (object solution, bool isOptimal, double[,] finalTableau) Solve(); // Updated return type
    }

    public class Solve
    {
        public double[] Solution { get; set; }
        public bool IsOptimal { get; set; }
        public double[,] FinalTableau { get; set; } // Added property for final tableau

        public Solve(double[] solution, bool isOptimal, double[,] finalTableau)
        {
            Solution = solution;
            IsOptimal = isOptimal;
            FinalTableau = finalTableau; // Initialize the new property
        }
    }
}


