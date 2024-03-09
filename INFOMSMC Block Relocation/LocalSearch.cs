using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INFOMSMC_Block_Relocation
{ 
    public class LocalSearch
    {
        public Problem Problem;
        public List<int> InitialStack;
        public List<int> Matching;
        public int[] Height;
        public GreedyHeuristic Heuristic;
        public int Score;
        public Dictionary<int, List<int>> Families;
        public List<int> FamilieNamen;
        public LocalSearch(GreedyHeuristic heuristic)
        {
            this.Problem = null;
            this.InitialStack = new List<int>();
            this.Matching = new List<int>();
            this.Height = new int[0];
            this.Heuristic = heuristic;
        }
        public void LoadProblem(Problem p)
        {
            this.Problem = p;
            this.Height = new int[p.State.Count];
            this.InitialStack.Clear();
            this.Matching.Clear();
            Families = new();

            //Place everything random
            int W = p.State.Count, s;
            for(int i = 0; i < p.InputSequence.Length; i++)
            {
                s = Config.random.Next(0, W);
                while (this.Height[s] == p.MaxHeight)
                    s = Config.random.Next(0, W);
                this.InitialStack.Add(s);
                this.Height[s]++;

                if (Families.ContainsKey(p.InputSequence[i]))
                    Families[p.InputSequence[i]].Add(i);
                else Families[p.InputSequence[i]] = [i];
            }
            for (int i = 0; i < p.InputSequence.Length; i++)
                this.Matching.Add(p.OutputSequence.Length);
            for (int i = 0; i < p.OutputSequence.Length; i++)
            {
                for(int j = p.InputSequence.Length - 1; j >= 0; j--)
                {
                    if (p.InputSequence[j] == p.OutputSequence[i] && this.Matching[j] == p.OutputSequence.Length)
                    {
                        this.Matching[j] = i;
                        break;
                    }
                }
            }
            this.Score = int.MaxValue;
            this.Evaluate();
        }
        public Intermediate LocallySearch(int maxtime) 
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds / 1000 < maxtime)
            {
                if (Config.random.NextDouble() < 0.7) ReplaceStack();
                else Tinder();

                if (Score == 0) break;
            }
            sw.Stop();
            return new Intermediate(this.InitialStack, this.Matching, this.Problem);
        }
        public void ReplaceStack()
        {
            int item = Config.random.Next(0, this.InitialStack.Count);
            int newstack = Config.random.Next(0, this.Problem.State.Count), oldstack = this.InitialStack[item];
            while(newstack == oldstack || this.Height[newstack] == this.Problem.MaxHeight)
                newstack = Config.random.Next(0, this.Problem.State.Count);
            this.InitialStack[item] = newstack;
            if (!this.Evaluate())
                this.InitialStack[item] = oldstack;
        }
        public void Tinder()
        {
            int family = Config.random.Next(0, this.Families.Count);
            if (this.Families[family].Count == 1)
                return;
            int length = this.Families[family].Count;
            int item1 = this.Families[family][Config.random.Next(0, length)], item2 = this.Families[family][Config.random.Next(0, length)];
            while(item1 == item2)
                item2 = this.Families[family][Config.random.Next(0, length)];
            int old1 = this.Matching[item1], old2 = this.Matching[item2];
            this.Matching[item1] = old2;
            this.Matching[item2] = old1;
            if (!this.Evaluate())
            {
                this.Matching[item1] = old1;
                this.Matching[item2] = old2;
            }
        }
        public bool Evaluate()//Return if we accept this change
        {
            Intermediate inter = new Intermediate(this.InitialStack, this.Matching, this.Problem);
            this.Heuristic.LoadProblem(inter);
            int value = this.Heuristic.Solve();
            if(value <= this.Score)
            {
                this.Score = value;
                return true;
            }
            return false;
        }
    }
}
