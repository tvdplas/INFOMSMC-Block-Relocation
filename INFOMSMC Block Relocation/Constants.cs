namespace INFOMSMC_Block_Relocation
{
    internal static class Config
    {
        // Determines whether the front or the back of a list is the top of a stack
        // Internally, back of array is always the top, however in test cases that are
        // sourced this might not be the case. 
        public static bool BACK_OF_ARRAY_IS_TOP = true;

        public static Random random = new Random();
        internal static int RUNS_PER_TEST_CASE = 5;
    }

    public enum InputGenerationStrategy
    {
        AlwaysHighest,
        FullyRandomized,
        GreedyRandomizedStack, 
        LTR,
        RTL
    }
}
