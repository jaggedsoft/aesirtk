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
	}
}