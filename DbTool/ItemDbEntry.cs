using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace DbTool {
	class ItemDbEntry : DbEntry {
		public enum TypeEnum {
			Eat = 0, Use, Smoke, Weapon, Armor, Shield, Helm, Left, Right, Script1, Script2,
			FaceAcc, Crown, Mantle, Necklace, Boots, Coat, Hand, Etc, UsePc
		}
		public enum SexEnum { Male = 0, Female, All }
		private int number = 0;
		[PropertyOrder(0)]
		public int Number {
			get { return number; }
			set { number = value; }
		}
		private string name = "";
		[PropertyOrder(1)]
		public string Name {
			get { return name; }
			set { name = value; }
		}
		private string shortName = "";
		[PropertyOrder(2)]
		public string ShortName {
			get { return shortName; }
			set { shortName = value; }
		}
		private int rank = 0;
		[PropertyOrder(3)]
		public int Rank {
			get { return rank; }
			set { rank = value; }
		}
		private TypeEnum type = TypeEnum.Use;
		[PropertyOrder(4)]
		public TypeEnum Type {
			get { return type; }
			set { type = value; }
		}
		private int price = 0;
		[PropertyOrder(5)]
		public int Price {
			get { return price; }
			set { price = value; }
		}
		private int sell = 0;
		[PropertyOrder(6)]
		public int Sell {
			get { return sell; }
			set { sell = value; }
		}
		private int maxAmount = 0;
		[PropertyOrder(7)]
		public int MaxAmount {
			get { return maxAmount; }
			set { maxAmount = value; }
		}
		private int classNumber = 0;
		[PropertyOrder(8)]
		public int Class {
			get { return classNumber; }
			set { classNumber = value; }
		}
		private SexEnum sex = SexEnum.All;
		[PropertyOrder(9)]
		public SexEnum Sex {
			get { return sex; }
			set { sex = value; }
		}
		private int level = 0;
		[PropertyOrder(10)]
		public int Level {
			get { return level; }
			set { level = value; }
		}
		private int look = 0;
		[PropertyOrder(11)]
		public int Look {
			get { return look; }
			set { look = value; }
		}
		private int lookColor = 0;
		[PropertyOrder(12)]
		public int LookColor {
			get { return lookColor; }
			set { lookColor = value; }
		}
		private int icon = 0;
		[PropertyOrder(13)]
		public int Icon {
			get { return icon; }
			set { icon = value; }
		}
		private int iconColor = 0;
		[PropertyOrder(14)]
		public int IconColor {
			get { return iconColor; }
			set { iconColor = value; }
		}
		private int sound = 0;
		[PropertyOrder(15)]
		public int Sound {
			get { return sound; }
			set { sound = value; }
		}
		private int durability = 0;
		[PropertyOrder(16)]
		public int Durability {
			get { return durability; }
			set { durability = value; }
		}
		private int might = 0;
		[PropertyOrder(17)]
		public int Might {
			get { return might; }
			set { might = value; }
		}
		private int will = 0;
		[PropertyOrder(18)]
		public int Will {
			get { return will; }
			set { will = value; }
		}
		private int grace = 0;
		[PropertyOrder(19)]
		public int Grace {
			get { return grace; }
			set { grace = value; }
		}
		private int armor = 0;
		[PropertyOrder(20)]
		public int Armor {
			get { return armor; }
			set { armor = value; }
		}
		private int hit = 0;
		[PropertyOrder(21)]
		public int Hit {
			get { return hit; }
			set { hit = value; }
		}
		private int damage = 0;
		[PropertyOrder(22)]
		public int Damage {
			get { return damage; }
			set { damage = value; }
		}
		private int vita = 0;
		[PropertyOrder(23)]
		public int Vita {
			get { return vita; }
			set { vita = value; }
		}
		private int mana = 0;
		[PropertyOrder(24)]
		public int Mana {
			get { return mana; }
			set { mana = value; }
		}
		private int protection = 0;
		[PropertyOrder(25)]
		public int Protection {
			get { return protection; }
			set { protection = value; }
		}
		private int healing = 0;
		[PropertyOrder(26)]
		public int Healing {
			get { return healing; }
			set { healing = value; }
		}
		private int minDamage = 0;
		[PropertyOrder(27)]
		public int MinDamage {
			get { return minDamage; }
			set { minDamage = value; }
		}
		private int maxDamage = 0;
		[PropertyOrder(28)]
		public int MaxDamage {
			get { return maxDamage; }
			set { maxDamage = value; }
		}
		public override string ToString() { return name; }
	}
}