using System;

namespace SNEAKer.Utils;

internal static class Helper
{
	public static float tFloat(this float value, int digits)
	{
		double num = Math.Pow(10.0, digits);
		return (float)Math.Truncate(num * (double)value / num);
	}
}
