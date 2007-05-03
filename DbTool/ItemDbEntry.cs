using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace DbTool {
	class ItemDbEntry : DbEntry {
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
		public enum TypeEnum {
			Eat = 0, Use, Smoke, Helm, Weapon, Armor, Shield, Hand, Etc, UseSpecial }
		private TypeEnum type = TypeEnum.Use;
		[PropertyOrder(3)]
		[Category("General")]
		public TypeEnum Type {
			get { return type; }
			set { type = value; }
		}
		private int price = 0;
		[PropertyOrder(4)]
		[Category("General")]
		public int Price {
			get { return price; }
			set { price = value; }
		}
		private int sell = 0;
		[PropertyOrder(5)]
		[Category("General")]
		public int Sell {
			get { return sell; }
			set { sell = value; }
		}
		private int maxAmount = 0;
		[PropertyOrder(6)]
		[Category("General")]
		public int MaxAmount {
			get { return maxAmount; }
			set { maxAmount = value; }
		}
		private int classNumber = 0;
		[PropertyOrder(7)]
		[Category("General")]
		public int Class {
			get { return classNumber; }
			set { classNumber = value; }
		}
		public enum SexEnum { Male = 0, Female, All }
		private SexEnum sex = SexEnum.All;
		[PropertyOrder(8)]
		[Category("General")]
		public SexEnum Sex {
			get { return sex; }
			set { sex = value; }
		}
		private int level = 0;
		[PropertyOrder(9)]
		[Category("General")]
		public int Level {
			get { return level; }
			set { level = value; }
		}
		private int look = 0;
		[PropertyOrder(10)]
		[Category("Appearance")]
		public int Look {
			get { return look; }
			set { look = value; }
		}
		private int lookColor = 0;
		[PropertyOrder(11)]
		[Category("Appearance")]
		public int LookColor {
			get { return lookColor; }
			set { lookColor = value; }
		}
		private int icon = 0;
		[PropertyOrder(12)]
		[Category("Appearance")]
		public int Icon {
			get { return icon; }
			set { icon = value; }
		}
		private int iconColor = 0;
		[PropertyOrder(13)]
		[Category("Appearance")]
		public int IconColor {
			get { return iconColor; }
			set { iconColor = value; }
		}
		private int sound = 0;
		[PropertyOrder(14)]
		[Category("Appearance")]
		public int Sound {
			get { return sound; }
			set { sound = value; }
		}
		private int durability = 0;
		[PropertyOrder(15)]
		[Category("Stats")]
		public int Durability {
			get { return durability; }
			set { durability = value; }
		}
		private int might = 0;
		[PropertyOrder(16)]
		[Category("Stats")]
		public int Might {
			get { return might; }
			set { might = value; }
		}
		private int will = 0;
		[PropertyOrder(17)]
		[Category("Stats")]
		public int Will {
			get { return will; }
			set { will = value; }
		}
		private int grace = 0;
		[PropertyOrder(18)]
		[Category("Stats")]
		public int Grace {
			get { return grace; }
			set { grace = value; }
		}
		private int armorClass = 0;
		[PropertyOrder(19)]
		[Category("Stats")]
		public int ArmorClass {
			get { return armorClass; }
			set { armorClass = value; }
		}
		private int hit = 0;
		[PropertyOrder(20)]
		[Category("Stats")]
		public int Hit {
			get { return hit; }
			set { hit = value; }
		}
		private int damage = 0;
		[PropertyOrder(21)]
		[Category("Stats")]
		public int Damage {
			get { return damage; }
			set { damage = value; }
		}
		private int vita = 0;
		[PropertyOrder(22)]
		[Category("Stats")]
		public int Vita {
			get { return vita; }
			set { vita = value; }
		}
		private int mana = 0;
		[PropertyOrder(23)]
		[Category("Stats")]
		public int Mana {
			get { return mana; }
			set { mana = value; }
		}
		private int protection = 0;
		[PropertyOrder(24)]
		[Category("Stats")]
		public int Protection {
			get { return protection; }
			set { protection = value; }
		}
		private int healing = 0;
		[PropertyOrder(25)]
		[Category("Stats")]
		public int Healing {
			get { return healing; }
			set { healing = value; }
		}
		private int minimumDamage = 0;
		[PropertyOrder(26)]
		[Category("Stats")]
		public int MinimumDamage {
			get { return minimumDamage; }
			set { minimumDamage = value; }
		}
		private int maximumDamage = 0;
		[PropertyOrder(27)]
		[Category("Stats")]
		public int MaximumDamage {
			get { return maximumDamage; }
			set { maximumDamage = value; }
		}
		public override string ToString() { return name; }
	}
}