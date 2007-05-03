using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;

namespace DbTool {
	abstract class DbEntry {
		protected class PropertyOrderAttribute : Attribute {
			private int order;
			public int Order { get { return order; } }
			public PropertyOrderAttribute(int order) { this.order = order; }
		}
		private static int GetPropertyOrder(PropertyInfo property) {
			object[] attributes = property.GetCustomAttributes(typeof(PropertyOrderAttribute), false);
			Debug.Assert(attributes.Length == 1);
			return ((PropertyOrderAttribute)attributes[0]).Order;
		}
		private static PropertyInfo[] GetProperties(Type type) {
			PropertyInfo[] properties = type.GetProperties();
			Array.Sort<PropertyInfo>(properties, delegate(PropertyInfo x, PropertyInfo y) {
				return Comparer<int>.Default.Compare(GetPropertyOrder(x), GetPropertyOrder(y));
			});
			return properties;
		}
		internal void Write(TextWriter textWriter) {
			PropertyInfo[] properties = GetProperties(GetType());
			string[] items = new string[properties.Length];
			for(int index = 0; index < properties.Length; ++index) {
				object propertyValue = properties[index].GetValue(this, null);
				if(properties[index].PropertyType.IsSubclassOf(typeof(Enum)))
					propertyValue = ((IConvertible)propertyValue).ToUInt32(null);
				items[index] = propertyValue.ToString();
			}
			textWriter.WriteLine(string.Join(",", items));
		}
		internal static TEntry FromLine<TEntry>(string line) where TEntry : DbEntry, new() {
			TEntry entry = new TEntry();
			PropertyInfo[] properties = GetProperties(typeof(TEntry));
			string[] items = line.Split(',');
			Array.ForEach<string>(items, delegate(string obj) { obj = obj.Trim(' '); });
			items = Array.FindAll<string>(items, delegate(string obj) { return obj != ""; });
			for(int index = 0; index < properties.Length; ++index) {
				if(index >= items.Length) throw new Exception();
				PropertyInfo property = properties[index];
				object convertedItem;
				if(property.PropertyType.IsSubclassOf(typeof(Enum)))
					convertedItem = int.Parse(items[index]);
				else convertedItem = Convert.ChangeType(items[index], property.PropertyType);
				property.SetValue(entry, convertedItem, null);
			}
			return entry;
		}
		public override abstract string ToString();
	}
}