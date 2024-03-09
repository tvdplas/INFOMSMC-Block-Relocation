using Newtonsoft.Json;
using System.Diagnostics;

namespace INFOMSMC_Block_Relocation
{
    public class Intermediate
    {
        //ILP maakt van algemeen Intermediate met stacks van items die in een volgorde opgehaald worden
        //Heurstiek maakt van Intermediate een oplossing met verplaatsingen
        public IList<IList<int>> Stacks;
        public int MaxHeight;
        public int OutputSequenceSize;
        public Intermediate(int W, int maxHeight, int outputSequenceSize) 
        {
            this.MaxHeight = maxHeight;
            this.OutputSequenceSize = outputSequenceSize;
            this.Stacks = new List<IList<int>>();
            for (int x = 0; x < W; x++)
                this.Stacks.Add(new List<int>());
        }

        public Intermediate(List<int> InitialStack, List<int> Matching, Problem p) {
            this.MaxHeight = p.MaxHeight;
            this.OutputSequenceSize = p.OutputSequence.Length;
            this.Stacks = new List<IList<int>>();
            for (int x = 0; x < p.State.Count; x++)
                this.Stacks.Add(new List<int>());

            for (int i = 0; i < InitialStack.Count; i++)
            {
                Stacks[InitialStack[i]].Add(Matching[i]);
            }
        }
    }
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

        public Problem Clone()
        {
            return new Problem(
                this.InstanceName,
                this.InputSequence.Select(x => x).ToArray(),
                this.OutputSequence.Select(x => x).ToArray(),
                this.Families.Select(x => x).ToArray(),
                this.MaxHeight,
                this.State.Select(x => x.Select(y => y).ToList()).ToList()
            );
        }

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

        public Problem(string instanceName, int[] inputSequence, int[] outputSequence, int[] families, int maxHeight, List<List<int>> state) : this(instanceName)
        {
            InputSequence = inputSequence;
            OutputSequence = outputSequence;
            Families = families;
            MaxHeight = maxHeight;
            State = state;
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

            //Config.random = new Random(int.Parse(Console.ReadLine()));

            string folderName = "medium_var_nFam";


            var files = Directory.EnumerateFiles($"..\\..\\..\\data\\{folderName}\\");
            string[] instanceNames = new string[files.Count()];
            long[,] times = new long[files.Count(), Config.RUNS_PER_TEST_CASE];
            double[,] ilpObj = new double[files.Count(), Config.RUNS_PER_TEST_CASE];
            long[,] values = new long[files.Count(), Config.RUNS_PER_TEST_CASE];
            double[,] gaps = new double[files.Count(), Config.RUNS_PER_TEST_CASE];
            double[,] minBounds = new double[files.Count(), Config.RUNS_PER_TEST_CASE];
            Stopwatch sw = new Stopwatch();
            int fileC = 0;
            foreach (var file in files) 
            {
                for (int run = 0; run < Config.RUNS_PER_TEST_CASE; run++)
                {
                    sw.Restart();
                    string problemText = File.ReadAllText(file);
                    Problem p = new(problemText);
                    instanceNames[fileC] = p.InstanceName;
                    p.GenerateInputSequence(InputGenerationStrategy.FullyRandomized);
                    (Intermediate inter, double gap, double minBound, double objective) = MinBlockingInputILP.Solve(p);

                    gaps[fileC, run] = gap;
                    minBounds[fileC, run] = minBound;
                    ilpObj[fileC, run] = minBound;

                    GreedyHeuristic greedy = new GreedyHeuristic();
                    greedy.LoadProblem(inter);
                    
                    var value = greedy.Solve();
                    sw.Stop();
                    times[fileC, run] = sw.ElapsedMilliseconds / 1000;
                    values[fileC, run] = value;
                }
                fileC++;
                if (fileC >= 0)
                    break;
            }

            List<string> lines = new List<string>(files.Count() * (1 + Config.RUNS_PER_TEST_CASE) + 1);
            lines.Add("time;value;gap;minBound;ilpval");
            for(fileC = 0; fileC < files.Count(); fileC++)
            {
                lines.Add(instanceNames[fileC]);
                for (int run = 0; run < Config.RUNS_PER_TEST_CASE; run++)
                {
                    lines.Add($"{times[fileC, run]};{values[fileC, run]};{gaps[fileC, run]};{minBounds[fileC, run]};{ilpObj[fileC, run]}");
                }
            }
            File.WriteAllLines($"./results_{folderName}.csv", lines);
        }
    }
}
