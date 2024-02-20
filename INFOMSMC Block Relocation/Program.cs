using Newtonsoft.Json;

namespace INFOMSMC_Block_Relocation
{
    public class Problem
    {
        public string InstanceName { get; private set; }

        // Sequence for input; Index 0 should be handled first
        public int[] InputSequence;
        // Sequence for output; Index 0 should be handled first
        public int[] OutputSequence;

        public int[] Families { get; private set; } 

        // Maximum height of any of the stacks
        public int MaxHeight;

        // Current problem state
        public List<List<int>> State;

        // Data structures for JSON parsing
        private class JSONProblem
        {
            public int[] sequence { get; set; }
            public int maxHeight { get; set; }
            public List<List<JSONBox>> stacks { get; set; }
            public string name { get; set; }
        }

        private class JSONBox
        {
            public int Index { get; set; }
            public int Id { get; set; }
        }

        /// <summary>
        /// Read in problem from main paper source JSON
        /// </summary>
        /// <param name="s"></param>
        public Problem(string s)
        {
            JSONProblem p = JsonConvert.DeserializeObject<JSONProblem>(s);

            OutputSequence = p.sequence;
            InputSequence = Array.Empty<int>();
            MaxHeight = p.maxHeight;
            InstanceName = p.name;
            State = new List<List<int>>();
            foreach (List<JSONBox> l in p.stacks)
            {
                var stack = l.Select(x => x.Index);
                if (!Config.BACK_OF_ARRAY_IS_TOP) stack.Reverse();
                State.Add(stack.ToList());
            }
            Families = State.SelectMany(i => i).ToArray();
        }

        /// <summary>
        /// Generates a new input sequence from the current state.
        /// </summary>
        public void GenerateInputSequence(InputGenerationStrategy strat)
        {
            List<int> sequence = new List<int>();
            var r = Config.random;

            if (strat == InputGenerationStrategy.FullyRandomized)
            {
                while(State.Sum(x => x.Count) > 0)
                {
                    var candidates = State.Where(x => x.Count > 0).ToList();
                    var index = r.Next(candidates.Count);
                    sequence.Add(candidates[index][^1]);
                    candidates[index].RemoveAt(candidates[index].Count - 1);
                }
            }

            else if (strat == InputGenerationStrategy.AlwaysHighest)
            {
                while(State.Sum(x => x.Count) > 0)
                {
                    var highest = State.Max(x => x.Count);  
                    var candidates = State.Where(x => x.Count == highest).ToList();
                    var index = r.Next(candidates.Count);
                    sequence.Add(candidates[index][^1]);
                    candidates[index].RemoveAt(candidates[index].Count - 1);
                }
            }

            else if (strat == InputGenerationStrategy.GreedyRandomizedStack)
            {
                while (State.Sum(x => x.Count) > 0) { 
                    var candidates = State.Where(x => x.Count > 0).ToList();
                    var index = r.Next(candidates.Count);
                    candidates[index].Reverse();
                    sequence.AddRange(candidates[index]);
                    candidates[index].Clear();
                }
            }

            else if (strat == InputGenerationStrategy.LTR || strat == InputGenerationStrategy.RTL)
            {
                for (int i = 0; i < State.Count; i++)
                {
                    int index = strat == InputGenerationStrategy.LTR ? i : State.Count - i - 1;
                    State[index].Reverse();
                    sequence.AddRange(State[index]);
                    State[index].Clear();
                }
            }

            InputSequence = sequence.ToArray();
        }
    }
    

    internal class Program
    {
        static void Main(string[] args)
        {
            //string problemText = File.ReadAllText("../../../data/Example.json");
            string problemText = File.ReadAllText("C:\\Users\\thoma\\Desktop\\INFOMSMC Block Relocation\\INFOMSMC Block Relocation\\data\\medium_var_nFam\\CompanyLoadedRandom-20-45-40-108-72-linear-0.json");

            Problem p = new(problemText);
            p.GenerateInputSequence(InputGenerationStrategy.FullyRandomized);

            Console.WriteLine(MinBlockingInputILP.Solve(p));
            Console.WriteLine(p);
        }
    }
}
