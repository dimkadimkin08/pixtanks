using System;

public static class PerlinUtils
{
    public static int[] GeneratePremutationTable(int seed)
    {
        const int size = 256;
        Random random = new(seed);

        int[] permutationTable = new int[size];
        for (int i = 0; i < size; i++)
        {
            permutationTable[i] = i;
        }

        for (int i = size - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);

            int temp = permutationTable[i];
            permutationTable[i] = permutationTable[swapIndex];
            permutationTable[swapIndex] = temp;
        }

        int[] finalTable = new int[512];
        for (int i = 0; i < finalTable.Length; i++)
        {
            finalTable[i] = permutationTable[i % size];
        }

        return finalTable;
    }
}