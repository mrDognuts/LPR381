using LPR_commandLine.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR_commandLine.LPAlgorithms
{
    internal class knapsackBranchAndBound
    {
        private int candidateCount = 0;
        private bool isMaximization = true;
        private double bestObjectiveValue = double.NegativeInfinity;
        private Dictionary<int, double> bestSolution = new Dictionary<int, double>();
        private List<(int candidateNumber, double maxValue)> candidates = new List<(int candidateNumber, double maxValue)>();

        public List<KeyValuePair<int, double>> RankVariablesWithRatio(LinearProgrammingModel model)
        {
            int numberOfConstraints = model.Constraints.Count;
            int numberOfObjectiveCoefficients = model.Objective.Coefficients.Count;
            int numberOfConstraintCoefficients = model.Constraints[0].Coefficients.Count;

            if (numberOfConstraints == 0 || numberOfObjectiveCoefficients == 0)
            {
                Console.WriteLine("Model must have at least one constraint and objective function coefficients.");
            }

            if (numberOfConstraintCoefficients != numberOfObjectiveCoefficients)
            {
                Console.WriteLine("Objective function and first constraint must have the same number of variables.");
            }

            List<KeyValuePair<int, double>> ratios = new List<KeyValuePair<int, double>>(); //The ratios will be stored in this list
            for (int i = 0; i < numberOfObjectiveCoefficients; i++)
            {
                double objectiveCoef = model.Objective.Coefficients[i];
                double constraintCoef = model.Constraints[0].Coefficients[i];

                if (constraintCoef != 0)
                {
                    double ratio = objectiveCoef / constraintCoef;
                    ratios.Add(new KeyValuePair<int, double>(i + 1, ratio));
                    Console.WriteLine($"x{i + 1}: Ratio = {objectiveCoef} / {constraintCoef} = {ratio}");
                }
            }

            ratios.Sort((x, y) => y.Value.CompareTo(x.Value));

            Console.WriteLine("Ranking:");
            int rank = 1;
            foreach (var ratio in ratios)
            {
                Console.WriteLine($"x{ratio.Key}: Ratio = {ratio.Value} --> Rank = {rank}");
                rank++;
            }
            Console.WriteLine("===============================================================");
            return ratios;
        }

        public void SolveKnapsackBranchAndBound(LinearProgrammingModel model, bool isMaximization = true)
        {

            var rankedVariables = RankVariablesWithRatio(model);
            double rhs = model.Constraints[0].RHS;
            SolveSubproblem(rankedVariables, rhs, new Dictionary<int, double>(), model);

            //Print best candidate number, variables and z value
            var bestCandidate = candidates.FirstOrDefault(c => c.maxValue == bestObjectiveValue);

            Console.WriteLine("===============================================================");
            Console.WriteLine($"Best Candidate:  {bestCandidate.candidateNumber}");

            foreach (var variable in bestSolution)
            {
                Console.WriteLine($"x{variable.Key} = {variable.Value}");
            }

            Console.WriteLine($"Z: {bestObjectiveValue}");
        }

        private double CalculateObjectiveValue(Dictionary<int, double> solution, LinearProgrammingModel model)
        {
            //This method calculates the z value according to the objective function
            int numberOfObjectiveCoefficients = model.Objective.Coefficients.Count;
            double objectiveValue = 0;
            for (int i = 0; i < numberOfObjectiveCoefficients; i++)
            {
                if (solution.ContainsKey(i + 1))
                {
                    double value = solution[i + 1];
                    objectiveValue += model.Objective.Coefficients[i] * value;
                }
            }
            return objectiveValue;
        }

        private void SolveSubproblem(List<KeyValuePair<int, double>> rankedVariables, double originalRHS, Dictionary<int, double> fixedValues, LinearProgrammingModel model, string subproblemNumber = "0")
        {
            string type = model.Objective.Type;
            double currentRHS = originalRHS;
            Console.WriteLine("===============================================================");
            Console.Write($"Subproblem {subproblemNumber}:");

            //Print the variable and the value it is fixed to (Either 0 or 1)
            if (fixedValues.Count > 0)
            {
                int lastFixedVariable = fixedValues.Keys.Last();
                double lastFixedValue = fixedValues[lastFixedVariable];
                Console.WriteLine($"x{lastFixedVariable} = {lastFixedValue}");
            }
            else
            {
                Console.WriteLine("Root Node");
            }
            Console.WriteLine("---------------------------");

            //move the new focus variable to the top of the ranking
            List<KeyValuePair<int, double>> adjustedRanking = new List<KeyValuePair<int, double>>();
            foreach (var fixedVar in fixedValues)
            {
                adjustedRanking.Add(new KeyValuePair<int, double>(fixedVar.Key, 0)); // Value 0 to keep it at the top
            }

            foreach (var variable in rankedVariables)
            {
                if (!fixedValues.ContainsKey(variable.Key))
                {
                    adjustedRanking.Add(variable);
                }
            }

            //print new ranking
            Console.WriteLine("New Ranking:");
            for (int i = 0; i < adjustedRanking.Count; i++)
            {
                Console.WriteLine($"x{adjustedRanking[i].Key}: Ratio = {adjustedRanking[i].Value} --> Rank = {i + 1}");
            }
            Console.WriteLine("---------------------------------------------------------------");
            Dictionary<int, double> currentSolution = new Dictionary<int, double>(fixedValues);

            //Iterate through the variable and subtract the values in order of the ranking
            foreach (var variable in adjustedRanking)
            {
                int varIndex = variable.Key;
                double constraintCoef = model.Constraints[0].Coefficients[varIndex - 1];

                if (currentSolution.ContainsKey(varIndex))
                {
                    if (currentSolution[varIndex] == 1)
                    {
                        Console.WriteLine($"x{varIndex} = 1, Remaining RHS = {currentRHS} - {constraintCoef} = {currentRHS - constraintCoef}");
                        currentRHS -= constraintCoef;
                    }
                    else
                    {
                        Console.WriteLine($"x{varIndex} = 0, Remaining RHS = {currentRHS}");
                    }
                }
                else
                {
                    if (constraintCoef <= currentRHS) //If a value can be subtracted without resulting in a fraction, then the variable value is set to 1
                    {
                        Console.WriteLine($"x{varIndex} = 1, Remaining RHS = {currentRHS} - {constraintCoef} = {currentRHS - constraintCoef}");
                        currentSolution[varIndex] = 1;
                        currentRHS -= constraintCoef;
                    }
                    else //If a value is subtracted and results in a fraction, the variable value will be set to the fraction (This variable is branched on)
                    {
                        double fraction = currentRHS / constraintCoef;
                        Console.WriteLine($"x{varIndex} = {currentRHS} / {constraintCoef} = {fraction}");
                        currentSolution[varIndex] = fraction;
                        currentRHS -= constraintCoef * fraction;
                        break;
                    }
                }
            }

            Console.WriteLine("Current Solution:");
            foreach (var variable in currentSolution)
            {
                Console.WriteLine($"x{variable.Key} = {variable.Value}");
            }
            Console.WriteLine("---------------------------------------------------------------");

            // Check if all constraints are satisfied
            bool allConstraintsSatisfied = true;
            for (int j = 0; j < model.Constraints.Count; j++)
            {
                double constraintRHS = model.Constraints[j].RHS;
                double constraintLHS = 0;
                for (int i = 0; i < model.Constraints[j].Coefficients.Count; i++)
                {
                    if (currentSolution.TryGetValue(i + 1, out double value))
                    {
                        constraintLHS += model.Constraints[j].Coefficients[i] * value;
                    }
                }
                if (constraintLHS > constraintRHS)
                {
                    allConstraintsSatisfied = false;
                    Console.WriteLine($"Infeasible solution.");
                    break;
                }
            }

            if (!allConstraintsSatisfied)
            {
                return; //if all the constraints are not satisifed, the branch ends
            }

            if (currentRHS < 0)
            {
                Console.WriteLine("Infeasible solution."); //if the rhs becomes negative then the branch ends and is infeasible
                return;
            }

            int fractionalVarIndex = currentSolution.FirstOrDefault(kv => kv.Value > 0 && kv.Value < 1).Key;

            if (fractionalVarIndex != 0)
            {
                //if the variable has a fraction, that becomes the focus of the next problem
                //the focus variable moves to the top of the ranking
                var focusVar = adjustedRanking.FirstOrDefault(kv => kv.Key == fractionalVarIndex);
                adjustedRanking.Remove(focusVar);
                adjustedRanking.Insert(0, focusVar);

                List<KeyValuePair<int, double>> adjustedRanking0 = new List<KeyValuePair<int, double>>(adjustedRanking);
                List<KeyValuePair<int, double>> adjustedRanking1 = new List<KeyValuePair<int, double>>(adjustedRanking);

                //the focus variable is set to 0
                Dictionary<int, double> fixedValues0 = new Dictionary<int, double>(fixedValues);
                fixedValues0[fractionalVarIndex] = 0;
                string newSubproblemNumber0 = subproblemNumber + ".1";
                SolveSubproblem(adjustedRanking0, originalRHS, fixedValues0, model, newSubproblemNumber0);

                //the focus variable is set to 1
                Dictionary<int, double> fixedValues1 = new Dictionary<int, double>(fixedValues);
                fixedValues1[fractionalVarIndex] = 1;
                string newSubproblemNumber1 = subproblemNumber + ".2";
                SolveSubproblem(adjustedRanking1, originalRHS, fixedValues1, model, newSubproblemNumber1);
            }
            else
            {
                //if there are no fractional variables, the subproblem becomes a candidate
                bool isCandidate = true;
                foreach (var value in currentSolution.Values)
                {
                    if (value != 0 && value != 1)
                    {
                        isCandidate = false;
                        break;
                    }
                }

                if (isCandidate)
                {
                    //the candidate is added to a list of candidates along with its z value
                    candidateCount++;
                    double objectiveValue = CalculateObjectiveValue(currentSolution, model);
                    Console.WriteLine($"Candidate {candidateCount}: {objectiveValue}");
                    candidates.Add((candidateCount, objectiveValue));

                    if ((isMaximization && objectiveValue > bestObjectiveValue) ||
                      (!isMaximization && objectiveValue < bestObjectiveValue))
                    {
                        bestObjectiveValue = objectiveValue;
                        bestSolution = new Dictionary<int, double>(currentSolution);
                    }
                }
                else
                {
                    Console.WriteLine("Infeasible solution.");
                }
            }
        }
    }
}