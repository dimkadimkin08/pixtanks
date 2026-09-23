using System;
public static class MathUShort
{
    public static ushort Clamp(int value) => (ushort)Math.Clamp(value, ushort.MinValue, ushort.MaxValue);

    public static ushort Add(ushort first, ushort second) => Clamp(first + second);
    public static ushort Sub(ushort first, ushort second) => Clamp(first - second);
    public static ushort Mul(ushort first, ushort second) => Clamp(first * second);
    public static ushort Div(ushort first, ushort second) => Clamp(first / second);

    public static ushort Add(ref ushort first, ushort second) => first = Clamp(first + second);
    public static ushort Sub(ref ushort first, ushort second) => first = Clamp(first - second);
    public static ushort Mul(ref ushort first, ushort second) => first = Clamp(first * second);
    public static ushort Div(ref ushort first, ushort second) => first = Clamp(first / second);
}
