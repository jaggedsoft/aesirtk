using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using Aesir.Nexus;
using Microsoft.Win32;

// NOTE: My Computer\HKEY_CURRENT_USER\Software\Nexon\Kingdom of the Winds\Location
namespace Aesir {
	static class Program {
		[STAThread()]
		static void Main(string[] args) {
			/*RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Nexon\Kingdom of the Winds");
			string value;
			using(key) {
				value = (string)key.GetValue("Location");
			}
			Console.WriteLine(value);*/
			Application.Run(MainForm.Default);
		}
	}
}
