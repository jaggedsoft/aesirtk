using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace DbTool {
	class MobDbEntry : DbEntry {
		private int number = 0;
		[PropertyOrder(0)]
		[Category("General")]
		public int Number {
			get { return number; }
			set { number = value; }
		}
		private string name = "Untitled";
		[PropertyOrder(1)]
		[Category("General")]
		public string Name {
			get { return name; }
			set { name = value; }
		}
		private string shortName = "Untitled";
		[PropertyOrder(2)]
		[Category("General")]
		public string ShortName {
			get { return shortName; }
			set { shortName = value; }
		}
		public enum TypeEnum { Normal = 0, Aggressive, Stationary }
		private TypeEnum type = TypeEnum.Normal;
		[PropertyOrder(3)]
		[Category("General")]
		public TypeEnum Type {
			get { return type; }
			set { type = value; }
		}
		private int look = 0;
		[PropertyOrder(4)]
		[Category("Appearance")]
		public int Look {
			get { return look; }
			set { look = value; }
		}
		private int lookColor = 0;
		[PropertyOrder(5)]
		[Category("Appearance")]
		public int LookColor {
			get { return lookColor; }
			set { lookColor = value; }
		}
		private int vita = 0;
		[PropertyOrder(6)]
		[Category("Stats")]
		public int Vita {
			get { return vita; }
			set { vita = value; }
		}
		private int baseExperience = 0;
		[PropertyOrder(7)]
		[Category("Stats")]
		public int BaseExperience {
			get { return baseExperience; }
			set { baseExperience = value; }
		}
		private int damage = 0;
		[PropertyOrder(8)]
		[Category("Stats")]
		public int Damage {
			get { return damage; }
			set { damage = value; }
		}
		private int drop1 = 0;
		[PropertyOrder(9)]
		[Category("Drops")]
		public int Drop1 {
			get { return drop1; }
			set { drop1 = value; }
		}
		private int drop2 = 0;
		[PropertyOrder(10)]
		[Category("Drops")]
		public int Drop2 {
			get { return drop2; }
			set { drop2 = value; }
		}
		private int drop3 = 0;
		[PropertyOrder(11)]
		[Category("Drops")]
		public int Drop3 {
			get { return drop3; }
			set { drop3 = value; }
		}
		private int drop4 = 0;
		[PropertyOrder(12)]
		[Category("Drops")]
		public int Drop4 {
			get { return drop4; }
			set { drop4 = value; }
		}
		private int drop5 = 0;
		[PropertyOrder(13)]
		[Category("Drops")]
		public int Drop5 {
			get { return drop5; }
			set { drop5 = value; }
		}
		private int drop6 = 0;
		[PropertyOrder(14)]
		[Category("Drops")]
		public int Drop6 {
			get { return drop6; }
			set { drop6 = value; }
		}
		private int drop7 = 0;
		[PropertyOrder(15)]
		[Category("Drops")]
		public int Drop7 {
			get { return drop7; }
			set { drop7 = value; }
		}
		private int drop8 = 0;
		[PropertyOrder(16)]
		[Category("Drops")]
		public int Drop8 {
			get { return drop8; }
			set { drop8 = value; }
		}
		public override string ToString() { return name; }
	}
}
