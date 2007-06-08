namespace DbTool {
    
    
    public class ItemDbEntry : DbEntry {
        
        private int number;
        
        private string name;
        
        private string shortName;
        
        private int rank;
        
        private TypeEnum type = TypeEnum.Use;
        
        [PropertyOrderAttribute(0)]
        public int Number {
            get {
                return this.number;
            }
            set {
                this.number = value;
            }
        }
        
        [PropertyOrderAttribute(1)]
        public string Name {
            get {
                return this.name;
            }
            set {
                this.name = value;
            }
        }
        
        [PropertyOrderAttribute(2)]
        public string ShortName {
            get {
                return this.shortName;
            }
            set {
                this.shortName = value;
            }
        }
        
        [PropertyOrderAttribute(3)]
        public int Rank {
            get {
                return this.rank;
            }
            set {
                this.rank = value;
            }
        }
        
        [PropertyOrderAttribute(4)]
        public TypeEnum Type {
            get {
                return this.type;
            }
            set {
                this.type = value;
            }
        }
        
        public override string ToString() {
            return Name;
        }
        
        public enum TypeEnum {
            

			Eat = 0, Use, Smoke, Weapon, Armor, Shield, Helm, Left, Right, Script1, Script2,
			FaceAcc, Crown, Mantle, Necklace, Boots, Coat, Hand, Etc, UsePc
		
        }
    }
    
    public class DbEntries {
        
        public static void Init() {
            DbEntry.Register("item_db.txt", delegate() { return new ItemDbEntry(); });
        }
    }
}
