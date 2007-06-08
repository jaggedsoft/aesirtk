using System;
using System.Collections.Generic;
using System.Text;

namespace DbTool {
	class Mob2DbEntry : DbEntry {
		private int map = 0;
		[PropertyOrder(0)]
		public int Map {
			get { return map; }
			set { map = value; }
		}
		private int id = 0;
		[PropertyOrder(1)]
		public int Id {
			get { return id; }
			set { id = value; }
		}
		private int times = 0;
		[PropertyOrder(2)]
		public int Times {
			get { return times; }
			set { times = value; }
		}
		public override string ToString() {
			StringBuilder builder = new StringBuilder();
			builder.Append("<");
			builder.Append(map);
			builder.Append(", ");
			builder.Append(id);
			builder.Append(", ");
			builder.Append(times);
			builder.Append(">");
			return builder.ToString();
		}
	}
}
