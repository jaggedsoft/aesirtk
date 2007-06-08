using System;
using System.Collections.Generic;
using System.Text;

namespace DbTool {
	class SpawnDbEntry : DbEntry {
		private int map = 0;
		[PropertyOrder(1)]
		public int Map {
			get { return map; }
			set { map = value; }
		}
		private int x = 0;
		[PropertyOrder(2)]
		public int X {
			get { return x; }
			set { x = value; }
		}
		private int y = 0;
		[PropertyOrder(3)]
		public int Y {
			get { return y; }
			set { y = value; }
		}
		private int id = 0;
		[PropertyOrder(4)]
		public int Id {
			get { return id; }
			set { id = value; }
		}
		public override string ToString() {
			StringBuilder builder = new StringBuilder();
			builder.Append("<");
			builder.Append(map);
			builder.Append(", ");
			builder.Append(x);
			builder.Append(", ");
			builder.Append(y);
			builder.Append(", ");
			builder.Append(id);
			builder.Append(">");
			return builder.ToString();
		}
	}
}
