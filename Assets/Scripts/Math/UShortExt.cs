using System;
public static class UShortExt
{
    private static ushort Clamp(int value) => (ushort)Math.Clamp(value, ushort.MinValue, ushort.MaxValue);
    public static ushort RoundAndClamp(float value) => (ushort)Math.Clamp(Math.Round(value), ushort.MinValue, ushort.MaxValue);

    public static ushort Add(this ushort first, ushort second) => Clamp(first + second);
    public static ushort Sub(this ushort first, ushort second) => Clamp(first - second);
    public static ushort Mul(this ushort first, ushort second) => Clamp(first * second);
    public static ushort Div(this ushort first, ushort second) => Clamp(first / second);

    public static ushort Add(this ushort first, int second) => Clamp(first + second);
    public static ushort Sub(this ushort first, int second) => Clamp(first - second);
    public static ushort Mul(this ushort first, int second) => Clamp(first * second);
    public static ushort Div(this ushort first, int second) => Clamp(first / second);


    public static ushort Add(this ushort first, float second) => RoundAndClamp(first + second);
    public static ushort Sub(this ushort first, float second) => RoundAndClamp(first - second);
    public static ushort Mul(this ushort first, float second) => RoundAndClamp(first * second);
    public static ushort Div(this ushort first, float second) => RoundAndClamp(first / second);

    public static ushort ClampToMax(this ushort number, ushort max) => Clamp(Math.Clamp(number, 0, (int)max));
}
