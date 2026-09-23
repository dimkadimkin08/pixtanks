using Random = UnityEngine.Random;
public static class RandomUtils
{
    public static bool Chance(float chance) => chance > 0 && !(chance < 1) || Random.value < chance;

    public static T Between<T>(params T[] values) => values[Random.Range(0, values.Length)];

    public static T RandomElement<T>(this T[] array) => array[Random.Range(0, array.Length)];
}
