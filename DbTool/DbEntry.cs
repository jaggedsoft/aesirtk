using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;

namespace DbTool {
	public abstract class DbEntry {
		/// <summary>
		///		The <c>PropertyOrderAttribute</c> is used to determine the order of properties as
		///		they are stored in the CSV file.
		/// </summary>
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
		/// <summary>
		///		Gets an ordered (according to <c>PropertyOrderAttribute</c>) list of the properties
		///		for the specified type.
		/// </summary>
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
		internal void Read(string line) {
			PropertyInfo[] properties = GetProperties(GetType());
			string[] items = line.Split(',');
			// Trim leading and trailing spaces from the items
			Array.ForEach<string>(items, delegate(string obj) { obj = obj.Trim(' '); });
			// Remove null entries in the items array
			items = Array.FindAll<string>(items, delegate(string obj) { return obj != ""; });
			if(properties.Length > items.Length)
				throw new Exception("Error reading DB line: expected more items.");
			for(int index = 0; index < properties.Length; ++index) {
				PropertyInfo property = properties[index];
				object convertedItem;
				try {
					if(property.PropertyType.IsSubclassOf(typeof(Enum)))
						convertedItem = int.Parse(items[index]);
					else convertedItem = Convert.ChangeType(items[index], property.PropertyType);
				} catch(FormatException exception) {
					throw new Exception(string.Format("Error reading DB item \"{0}\" ({1}): type mismatch.",
						property.Name, index));
				}
				property.SetValue(this, convertedItem, null);
			}
		}
		internal delegate DbEntry Factory();
		internal static void Register(string name, Factory factory) {
			registry[name.ToLower()] = factory;
		}
		internal static Factory GetFactory(string name) {
			Factory factory;
			if(registry.TryGetValue(name.ToLower(), out factory))
				return factory;
			else return null;
		}
		public static ICollection<String> Names {
			get { return registry.Keys; }
		}
		private static Dictionary<string, Factory> registry = new Dictionary<string, Factory>();
		public override abstract string ToString();
	}
}