using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR_commandLine.Models
{
    internal class ModelDisplay
    {
        public void DisplayConstraints(LinearProgrammingModel model)
        {
            Console.WriteLine("Constraints:");
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                var constraint = model.Constraints[i];
                StringBuilder constraintBuilder = new StringBuilder();

                for (int j = 0; j < constraint.Coefficients.Count; j++)
                {
                    if (constraint.Coefficients[j] != 0)
                    {
                        if (constraintBuilder.Length > 0)
                        {
                            constraintBuilder.Append(" + ");
                        }
                        constraintBuilder.Append($"{constraint.Coefficients[j]}x{j + 1}");
                    }
                }

                // Add slack variables if they are present
                int slackVariableStartIndex = model.Objective.Coefficients.Count;
                for (int j = slackVariableStartIndex; j < constraint.Coefficients.Count; j++)
                {
                    if (constraint.Coefficients[j] != 0)
                    {
                        if (constraintBuilder.Length > 0)
                        {
                            constraintBuilder.Append(" + ");
                        }
                        constraintBuilder.Append($"{constraint.Coefficients[j]}s{j + 1 - slackVariableStartIndex}");
                    }
                }

                constraintBuilder.Append($" {constraint.Relation} {constraint.RHS}");
                Console.WriteLine(constraintBuilder.ToString());
            }
        }
    }
}
