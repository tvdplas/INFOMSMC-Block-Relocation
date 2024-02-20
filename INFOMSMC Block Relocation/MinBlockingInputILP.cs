using Gurobi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INFOMSMC_Block_Relocation
{
    internal static class MinBlockingInputILP
    {
        private static List<(int, int)> BlockingPairs(int[] inputSeq, int[] outputSeq)
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
        public static double Solve(Problem p)
        {
            List<(int, int)> blockingPairs = BlockingPairs(p.InputSequence, p.OutputSequence);

            GRBEnv env = new GRBEnv(true);
            env.Set("LogFile", "mip1.log");
            env.Start();

            // Create empty model
            GRBModel model = new GRBModel(env);

            // Create variables
            GRBVar[,] xis = new GRBVar[p.Families.Max() + 1, p.State.Count];
            GRBVar[,] yis = new GRBVar[p.Families.Max() + 1, p.State.Count];
            foreach (int i in p.Families)
            {
                foreach (int s in Enumerable.Range(0, p.State.Count))
                {
                    xis[i, s] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"x_({i},{s})");
                    yis[i, s] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"y_({i},{s})");
                }
            }

            // Objective
            GRBLinExpr objectiveExpr = new GRBLinExpr();
            foreach (int i in p.Families)
                foreach (int s in Enumerable.Range(0, p.State.Count))
                    objectiveExpr.AddTerm(1, yis[i, s]);

            model.SetObjective(objectiveExpr, GRB.MINIMIZE);

            // Constraints
            // (1)
            foreach (int i in p.Families)
            {
                GRBLinExpr constrExpr = new GRBLinExpr();

                foreach (int s in Enumerable.Range(0, p.State.Count))
                {
                    constrExpr.AddTerm(1, xis[i, s]);
                    constrExpr.AddTerm(1, yis[i, s]); 
                }

                model.AddConstr(constrExpr, GRB.EQUAL, 1, $"c_1_{i}");
            }

            // (2)
            foreach (int s in Enumerable.Range(0, p.State.Count))
            {
                GRBLinExpr constrExpr = new GRBLinExpr();

                foreach (int i in p.Families)
                {
                    constrExpr.AddTerm(1, xis[i, s]);
                    constrExpr.AddTerm(1, yis[i, s]);
                }

                model.AddConstr(constrExpr, GRB.LESS_EQUAL, p.MaxHeight, $"c_2_{s}");
            }

            foreach ((int i, int j) in blockingPairs)
            {
                foreach (int s in Enumerable.Range(0, p.State.Count))
                    model.AddConstr(xis[i,s] + xis[j, s] + yis[j,s] <= 1, $"c_3_({i},{j},{s})");
            }

            model.Optimize();
            model.Write($"results_{p.InstanceName}.lp");
            double res = model.ObjVal;
            model.Dispose();
            env.Dispose();

            return res;
        }
    }
}
