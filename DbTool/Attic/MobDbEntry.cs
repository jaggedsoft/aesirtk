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
		private int experience = 0;
		[PropertyOrder(7)]
		[Category("Stats")]
		public int Experience {
			get { return experience; }
			set { experience = value; }
		}
		private int hit = 0;
		[PropertyOrder(8)]
		[Category("Stats")]
		public int Hit {
			get { return hit; }
			set { hit = value; }
		}
		private int level = 0;
		[PropertyOrder(9)]
		[Category("Stats")]
		public int Level {
			get { return level; }
			set { level = value; }
		}
		private int might = 0;
		[PropertyOrder(10)]
		[Category("Stats")]
		public int Might {
			get { return might; }
			set { might = value; }
		}
		private int grace = 0;
		[PropertyOrder(11)]
		[Category("Stats")]
		public int Grace {
			get { return grace; }
			set { grace = value; }
		}
		private int moveTime = 0;
		[PropertyOrder(12)]
		[Category("Stats")]
		public int MoveTime {
			get { return moveTime; }
			set { moveTime = value; }
		}
		private int spawnTime = 0;
		[PropertyOrder(13)]
		[Category("Stats")]
		public int SpawnTime {
			get { return spawnTime; }
			set { spawnTime = value; }
		}
		private int drop1 = 0;
		[PropertyOrder(14)]
		[Category("Drops")]
		public int Drop1 {
			get { return drop1; }
			set { drop1 = value; }
		}
		private int drop2 = 0;
		[PropertyOrder(15)]
		[Category("Drops")]
		public int Drop2 {
			get { return drop2; }
			set { drop2 = value; }
		}
		private int drop3 = 0;
		[PropertyOrder(16)]
		[Category("Drops")]
		public int Drop3 {
			get { return drop3; }
			set { drop3 = value; }
		}
		private int drop4 = 0;
		[PropertyOrder(17)]
		[Category("Drops")]
		public int Drop4 {
			get { return drop4; }
			set { drop4 = value; }
		}
		private int drop5 = 0;
		[PropertyOrder(18)]
		[Category("Drops")]
		public int Drop5 {
			get { return drop5; }
			set { drop5 = value; }
		}
		private int drop6 = 0;
		[PropertyOrder(19)]
		[Category("Drops")]
		public int Drop6 {
			get { return drop6; }
			set { drop6 = value; }
		}
		private int drop7 = 0;
		[PropertyOrder(20)]
		[Category("Drops")]
		public int Drop7 {
			get { return drop7; }
			set { drop7 = value; }
		}
		private int drop8 = 0;
		[PropertyOrder(21)]
		[Category("Drops")]
		public int Drop8 {
			get { return drop8; }
			set { drop8 = value; }
		}
		private int dropRate1 = 0;
		[PropertyOrder(22)]
		[Category("Drops")]
		public int DropRate1 {
			get { return dropRate1; }
			set { dropRate1 = value; }
		}
		private int dropRate2 = 0;
		[PropertyOrder(23)]
		[Category("Drops")]
		public int DropRate2 {
			get { return dropRate2; }
			set { dropRate2 = value; }
		}
		private int dropRate3 = 0;
		[PropertyOrder(24)]
		[Category("Drops")]
		public int DropRate3 {
			get { return dropRate3; }
			set { dropRate3 = value; }
		}
		private int dropRate4 = 0;
		[PropertyOrder(25)]
		[Category("Drops")]
		public int DropRate4 {
			get { return dropRate4; }
			set { dropRate4 = value; }
		}
		private int dropRate5 = 0;
		[PropertyOrder(26)]
		[Category("Drops")]
		public int DropRate5 {
			get { return dropRate5; }
			set { dropRate5 = value; }
		}
		private int dropRate6 = 0;
		[PropertyOrder(27)]
		[Category("Drops")]
		public int DropRate6 {
			get { return dropRate6; }
			set { dropRate6 = value; }
		}
		private int dropRate7 = 0;
		[PropertyOrder(28)]
		[Category("Drops")]
		public int DropRate7 {
			get { return dropRate7; }
			set { dropRate7 = value; }
		}
		private int dropRate8 = 0;
		[PropertyOrder(29)]
		[Category("Drops")]
		public int DropRate8 {
			get { return dropRate8; }
			set { dropRate8 = value; }
		}
		private int dropCount1 = 0;
		[PropertyOrder(30)]
		[Category("Drops")]
		public int DropCount1 {
			get { return dropCount1; }
			set { dropCount1 = value; }
		}
		private int dropCount2 = 0;
		[PropertyOrder(31)]
		[Category("Drops")]
		public int DropCount2 {
			get { return dropCount2; }
			set { dropCount2 = value; }
		}
		private int dropCount3 = 0;
		[PropertyOrder(32)]
		[Category("Drops")]
		public int DropCount3 {
			get { return dropCount3; }
			set { dropCount3 = value; }
		}
		private int dropCount4 = 0;
		[PropertyOrder(33)]
		[Category("Drops")]
		public int DropCount4 {
			get { return dropCount4; }
			set { dropCount4 = value; }
		}
		private int dropCount5 = 0;
		[PropertyOrder(34)]
		[Category("Drops")]
		public int DropCount5 {
			get { return dropCount5; }
			set { dropCount5 = value; }
		}
		private int dropCount6 = 0;
		[PropertyOrder(35)]
		[Category("Drops")]
		public int DropCount6 {
			get { return dropCount6; }
			set { dropCount6 = value; }
		}
		private int dropCount7 = 0;
		[PropertyOrder(36)]
		[Category("Drops")]
		public int DropCount7 {
			get { return dropCount7; }
			set { dropCount7 = value; }
		}
		private int dropCount8 = 0;
		[PropertyOrder(37)]
		[Category("Drops")]
		public int DropCount8 {
			get { return dropCount8; }
			set { dropCount8 = value; }
		}
		public override string ToString() { return name; }
	}
}
