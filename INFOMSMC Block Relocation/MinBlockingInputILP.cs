using Gurobi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INFOMSMC_Block_Relocation
{
    internal class MinBlockingInputILP
    {
        public List<(int, int)> BlockingPairs(int[] inputSeq, int[] outputSeq)
        {
            List<(int, int)> blockingPairs = new();

            for (int i =  0; i < inputSeq.Length; i++)
            {
                for (int j = i + 1; j < outputSeq.Length; j++)
                { 
                    int baseNum              = inputSeq[i];
                    int possibleBlockingNum  = inputSeq[j];

                    if (Array.IndexOf(outputSeq, baseNum) < Array.IndexOf(inputSeq, possibleBlockingNum))
                    {
                        blockingPairs.Add((possibleBlockingNum, baseNum));
                    }
                }
            }

            return blockingPairs;
        }
        public void Solve(Problem p)
        {
            GRBEnv env = new GRBEnv(true);
            env.Set("LogFile", "mip1.log");
            env.Start();

            // Create empty model
            GRBModel model = new GRBModel(env);

            // Create variables
            List<GRBVar> xis = new List<GRBVar>();
            List<GRBVar> yis = new List<GRBVar>();
            foreach (int i in p.Families)
            {
                foreach (int s in Enumerable.Range(0, p.State.Count))
                {
                    xis.Add(model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"x_({i},{s})"));
                    yis.Add(model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"y_({i},{s})"));
                }
            }

            // TODO: Constraints, objective

            // Set objective: maximize x + y + 2 z
            //model.SetObjective(x + y + 2 * z, GRB.MAXIMIZE);

            //// Add constraint: x + 2 y + 3 z <= 4
            //model.AddConstr(x + 2 * y + 3 * z <= 4.0, "c0");

            //// Add constraint: x + y >= 1
            //model.AddConstr(x + y >= 1.0, "c1");

            //// Optimize model
            //model.Optimize();

            //Console.WriteLine(x.VarName + " " + x.X);
            //Console.WriteLine(y.VarName + " " + y.X);
            //Console.WriteLine(z.VarName + " " + z.X);

            //Console.WriteLine("Obj: " + model.ObjVal);

            //// Dispose of model and env
            //model.Dispose();
            //env.Dispose();

        }
    }
}
