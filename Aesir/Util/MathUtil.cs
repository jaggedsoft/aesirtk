using System;
using System.Collections.Generic;
using System.Text;

namespace Aesir.Util {
	static class MathUtil {
		public static int Clamp(int value, int min, int max) {
			if(value < min) return min;
			if(value > max) return max;
			return value;
		}
		public static float Clamp(float value, float min, float max) {
			if(value < min) return min;
			if(value > max) return max;
			return value;
		}
		/// <summary>
		///		In normal integer division, 1 / 2 == 1 while -1 / 2 == 0. This utility function
		///		changes the semantics of division such that -1 / 2 == 1.
		/// </summary>
		/// <example>
		///		Debug.Assert(MathUtil.CeilingDivide(1, 2) == 1);
		///		Debug.Assert(MathUtil.CeilingDivide(-1, 2) == 1);
		/// </example>
		public static int CeilingDivide(int left, int right) {
			if(left >= 0) return left / right;
			else return left / right - 1;
		}
	}
}