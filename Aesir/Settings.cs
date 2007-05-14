using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace Aesir {
	sealed class Settings : ApplicationSettingsBase {
		[ApplicationScopedSetting()]
		// TODO: Change this to "", and then get the path from the registry!
		[DefaultSettingValue(@"C:\program files\nexustk\data")]
		public string DataPath {
			get { return (string)this["DataPath"]; }
			set { this["DataPath"] = value; }
		}
		private static Settings defaultInstance = new Settings();
		public static Settings Default { get { return defaultInstance; } }
	}
}