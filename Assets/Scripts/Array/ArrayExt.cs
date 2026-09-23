using System.Linq;

public static class ArrayExt
{
    /// <summary>
    /// Check if this string array fits into another string array
    /// </summary>
    /// <param name="array">this array</param>
    /// <param name="otherArray">another array</param>
    /// <returns>true if this array fits into otherArray</returns>
    public static bool IsFits(this string[] array, string[] otherArray)
    {
        return array.GroupBy(x => x).All(group => otherArray.Count(y => y == group.Key) >= group.Count());
    }
}