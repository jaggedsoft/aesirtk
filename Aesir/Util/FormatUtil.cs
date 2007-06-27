using System;
using System.Collections.Generic;
using System.Text;

namespace Aesir.Util {
	static class FormatUtil {
		public static string Format<T>(IEnumerable<T> enumerable) {
			List<string> items = new List<string>();
			foreach(T item in enumerable) items.Add(item.ToString());
			return string.Format("[{0}]", string.Join(", ", items.ToArray()));
		}
	}
}
