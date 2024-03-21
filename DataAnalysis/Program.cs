using System.Runtime.Serialization.Formatters;

namespace DataAnalysis
{

    public static class Extend
    {   
        public static double StandardDeviation(this IEnumerable<double> values)
        {
            double avg = values.Average();
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            List<string> data1 = new List<string>();
            List<string> data2 = new List<string>();
            string line = "";
            while (line != "done")
            {
                line = Console.ReadLine();
                if (line != "" && line != "done") data1.Add(line);
            }
            data1.RemoveAt(0);
            line = "";
            while (line != "done")
            {
                line = Console.ReadLine();
                if (line != "" && line != ";;;;;" && line != "done") data2.Add(line);
            }
            //data2.RemoveAt(0);

            // Get averages of thingies 
            List<double> lpResAvg = new List<double>();
            List<double> lsResAvg = new List<double>();
            List<double> lpResWc = new List<double>();
            List<double> lsResWc = new List<double>();
            for (int i = 0; i < data1.Count;) {
                i++; // skip header
                double lp = 0, ls = 0;
                double lp_wc = 0, ls_wc = 0;
                int j = 0;
                for (; i + j < data1.Count && j < 5; j++)
                {
                    string[] chunks = data1[i + j].Split(";");
                    lp += double.Parse(chunks[1]);
                    ls += double.Parse(chunks[^2]);
                    if (lp_wc < double.Parse(chunks[1])) lp_wc = double.Parse(chunks[1]);
                    if (ls_wc < double.Parse(chunks[^2])) ls_wc = double.Parse(chunks[^2]);
                }
                lp /= j;
                ls /= j;
                i += j;
                lpResAvg.Add(lp);   
                lsResAvg.Add(ls);
                lpResWc.Add(lp_wc);
                lsResWc.Add(ls_wc);
            }

            List<double> BK = data2.Select(x => double.Parse(x.Split(";")[^2])).ToList();
            
            // Avg
            List<double> ratiosLS = lsResAvg.Zip(BK).Select((x) => x.First / x.Second).ToList();
            List<double> ratiosLP = lpResAvg.Zip(BK).Select((x) => x.First / x.Second).ToList();
            double avgLS = ratiosLS.Average();
            double avgLP = ratiosLP.Average();
            double stdDevLS = ratiosLS.StandardDeviation();
            double stdDevLP = ratiosLP.StandardDeviation();

            Console.WriteLine($"{Math.Round(avgLP, 3)} & {Math.Round(avgLS, 3)} & {Math.Round(stdDevLP, 3)} & {Math.Round(stdDevLS, 3)}");

            // Worst case
            List<double> ratiosLSWC = lsResWc.Zip(BK).Select((x) => x.First / x.Second).ToList();
            List<double> ratiosLPWC = lpResWc.Zip(BK).Select((x) => x.First / x.Second).ToList();
            double avgLSWC = ratiosLSWC.Average();
            double avgLPWC = ratiosLPWC.Average();
            double stdDevLSWC = ratiosLSWC.StandardDeviation();
            double stdDevLPWC = ratiosLPWC.StandardDeviation();
            Console.WriteLine($"{Math.Round(avgLPWC, 3)} & {Math.Round(avgLSWC, 3)} & {Math.Round(stdDevLPWC, 3)} & {Math.Round(stdDevLSWC, 3)}");
        }
    }
}