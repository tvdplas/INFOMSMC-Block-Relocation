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
        private static List<(int, int)> ConflictingPairs(int[] inputSeq, int[] outputSeq)
        {
            List<(int, int)> blockingPairs = new();

            for (int i =  0; i < inputSeq.Length; i++)
            {
                for (int j = i + 1; j < outputSeq.Length; j++)
                { 
                    int baseNum              = inputSeq[i];
                    int possibleBlockingNum  = inputSeq[j];

                    if (Array.IndexOf(inputSeq, baseNum) < Array.IndexOf(inputSeq, possibleBlockingNum) && Array.IndexOf(outputSeq, baseNum) < Array.IndexOf(outputSeq, possibleBlockingNum))
                    {
                        blockingPairs.Add((j, i));
                    }
                }
            }

            return blockingPairs;
        }
        public static Intermediate Solve(Problem p)
        {
            //List<(int, int)> blockingPairs = ConflictingPairs(p.InputSequence, p.OutputSequence);

            GRBEnv env = new GRBEnv(true);
            env.Set("LogFile", "mip1.log");
            env.Start();

            // Create empty model
            GRBModel model = new GRBModel(env);

            // Create variables
            Console.WriteLine("Starting with variable adding");
            Console.WriteLine("variable x and y");
            GRBVar[,] x_is = new GRBVar[p.InputSequence.Length, p.State.Count];
            GRBVar[,] y_is = new GRBVar[p.InputSequence.Length, p.State.Count];
            foreach (int i in Enumerable.Range(0, p.InputSequence.Length))
            {
                foreach (int s in Enumerable.Range(0, p.State.Count))
                {
                    x_is[i, s] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"x_({i},{s})");
                    y_is[i, s] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"y_({i},{s})");
                }
            }

            Console.WriteLine("variable m");
            GRBVar?[,] m_ij = new GRBVar?[p.InputSequence.Length, p.OutputSequence.Length];
            foreach (int i in Enumerable.Range(0, p.InputSequence.Length))
            {
                foreach (int j in Enumerable.Range(0, p.OutputSequence.Length))
                {
                    if (p.InputSequence[i] == p.OutputSequence[j])
                    {
                        m_ij[i, j] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"m_({i},{j})");
                    }
                    else m_ij[i, j] = null;
                }
            }

            Console.WriteLine("variable c");
            GRBVar[,] c_ij = new GRBVar[p.InputSequence.Length, p.InputSequence.Length];
            foreach (int i in Enumerable.Range(0, p.InputSequence.Length))
            {
                foreach (int j in Enumerable.Range(0, p.InputSequence.Length))
                {
                    c_ij[i, j] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"c_({i},{j})");
                }
            }

            // Objective
            Console.WriteLine("Generating objective function");
            GRBLinExpr objectiveExpr = new GRBLinExpr();
            foreach (int i in Enumerable.Range(0, p.InputSequence.Length))
                foreach (int s in Enumerable.Range(0, p.State.Count))
                    objectiveExpr.AddTerm(1, y_is[i, s]);

            model.SetObjective(objectiveExpr, GRB.MINIMIZE);

            // Constraints
            // (1)
            Console.WriteLine("Constraint 1");
            foreach (int j in Enumerable.Range(0, p.OutputSequence.Length))
            {
                GRBLinExpr constrExpr = new GRBLinExpr();

                foreach (int i in Enumerable.Range(0, p.InputSequence.Length))
                {
                    if (!ReferenceEquals(m_ij[i, j], null))
                        constrExpr.AddTerm(1, m_ij[i, j]);
                }

                model.AddConstr(constrExpr, GRB.EQUAL, 1, $"c_1_({j})");
            }

            // (2)
            Console.WriteLine("Constraint 2");
            foreach (int i in Enumerable.Range(0, p.InputSequence.Length))
            {
                GRBLinExpr constrExpr = new GRBLinExpr();

                foreach (int j in Enumerable.Range(0, p.OutputSequence.Length))
                {
                    if (!ReferenceEquals(m_ij[i, j], null))
                        constrExpr.AddTerm(1, m_ij[i, j]);
                }

                model.AddConstr(constrExpr <= 1, $"c_2_({i})");
            }

            // (3)
            Console.WriteLine("Constraint 3");
            foreach (int i in Enumerable.Range(0, p.InputSequence.Length))
            {
                GRBLinExpr constrExpr = new GRBLinExpr();

                foreach (int s in Enumerable.Range(0, p.State.Count))
                {
                    constrExpr.AddTerm(1, x_is[i, s]);
                    constrExpr.AddTerm(1, y_is[i, s]);
                }

                model.AddConstr(constrExpr, GRB.EQUAL, 1, $"c_3_{i}");
            }

            // (4)
            Console.WriteLine("Constraint 4");
            foreach (int s in Enumerable.Range(0, p.State.Count))
            {
                GRBLinExpr constrExpr = new GRBLinExpr();

                foreach (int i in Enumerable.Range(0, p.InputSequence.Length))
                {
                    constrExpr.AddTerm(1, x_is[i, s]);
                    constrExpr.AddTerm(1, y_is[i, s]);
                }

                model.AddConstr(constrExpr, GRB.LESS_EQUAL, p.MaxHeight, $"c_4_{s}");
            }

            // (5)
            Console.WriteLine("Constraint 5");
            foreach (int j in Enumerable.Range(0, p.InputSequence.Length))
            {
                foreach (int i in Enumerable.Range(j + 1, p.InputSequence.Length - j - 1))
                {
                    foreach (int k in Enumerable.Range(0, p.OutputSequence.Length))
                    {
                        if (p.InputSequence[i] != p.OutputSequence[k]) continue;
                        if (p.InputSequence[i] == p.InputSequence[j]) continue;

                        // Gaat dit goed qua k/l?
                        GRBLinExpr constrExpr = new GRBLinExpr();

                        foreach (int l in (p.OutputSequence.Length - k - 1 >= 0) ? Enumerable.Range(k, p.OutputSequence.Length - k - 1) : [])
                            if (!ReferenceEquals(m_ij[i, l], null))
                                constrExpr.AddTerm(1, m_ij[i, l]);
                        foreach (int l in (k - 1 >= 0) ? Enumerable.Range(0, k - 1) : [])
                            if (!ReferenceEquals(m_ij[j, l], null))
                                constrExpr.AddTerm(1, m_ij[j, l]);

                        // Bring to other side of expression
                        constrExpr.AddTerm(-1, c_ij[i, j]);

                        model.AddConstr(constrExpr, GRB.LESS_EQUAL, 1, $"c_5_({i},{j},{k})");
                    }
                }
            }

            // (6)
            Console.WriteLine("Constraint 6");
            foreach (int i in Enumerable.Range(0, p.InputSequence.Length))
            {
                foreach (int j in Enumerable.Range(0, p.InputSequence.Length))
                {
                    foreach (int s in Enumerable.Range(0, p.State.Count))
                    {
                        model.AddConstr(x_is[i, s] + x_is[j, s] + y_is[j, s] + c_ij[i, j] <= 2, $"c_6_({i},{j},{s})");
                    }
                }
            }

            // (7)
            Console.WriteLine("Constraint 7");
            foreach (int j in Enumerable.Range(0, p.InputSequence.Length))
            {
                foreach (int i in Enumerable.Range(j + 1, p.InputSequence.Length - j - 1))
                {
                    GRBLinExpr constrExpr = new GRBLinExpr();

                    foreach (int k in Enumerable.Range(0, p.OutputSequence.Length))
                    {
                        if (!ReferenceEquals(m_ij[i, k], null))
                            constrExpr.AddTerm(1, m_ij[i, k]);
                        if (!ReferenceEquals(m_ij[j, k], null))
                            constrExpr.AddTerm(-1, m_ij[j, k]);
                    }

                    model.AddConstr(constrExpr + c_ij[i, j] >= 0, $"c_7_({i},{j})");
                }
            }

            model.Optimize();
            model.Write($"results_{p.InstanceName}.lp");
            Intermediate res = new Intermediate(p.State.Count);
            Console.WriteLine(m_ij.GetLength(1));
            for(int i = 0; i < x_is.GetLength(0); i++)
            {
                int id;
                for (id = 0; id < m_ij.GetLength(1); id++)
                    if (m_ij[i, id]?.X >= 0.5)
                        break;
                Console.WriteLine(id);
                for(int s = 0; s < p.State.Count; s++)
                    if (x_is[i,s].X + y_is[i,s].X >= 0.5)
                    {
                        res.Stacks[s].Add(id);
                        break;
                    }
            }

            model.Dispose();
            env.Dispose();

            return res;
        }
    }
}
