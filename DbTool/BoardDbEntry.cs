using System;
using System.Collections.Generic;
using System.Text;

namespace DbTool {
	class BoardDbEntry : DbEntry {
		private int number;
		[PropertyOrder(0)]
		public int Number {
			get { return number; }
			set { number = value; }
		}
		private string name;
		[PropertyOrder(1)]
		public string Name {
			get { return name; }
			set { name = value; }
		}
		private int level = 0;
		[PropertyOrder(2)]
		public int Level {
			get { return level; }
			set { level = value; }
		}
		private int gmLevel = 0;
		[PropertyOrder(3)]
		public int GMLevel {
			get { return gmLevel; }
			set { gmLevel = value; }
		}
		private int path = 0;
		[PropertyOrder(4)]
		public int Path {
			get { return path; }
			set { path = value; }
		}
		private int clan = 0;
		[PropertyOrder(5)]
		public int Clan {
			get { return clan; }
			set { clan = value; }
		}
		public override string ToString() { return name; }
	}
}
