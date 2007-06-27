using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using Aesir.Nexus;
using Microsoft.Win32;
using Aesir.Util;
using System.Configuration;
using System.Threading;
using System.Diagnostics;

namespace Aesir {
	class Program {
		private const string FloorTileSourceTag = "tile", ObjectTileSourceTag = "tilec";
		private const int FloorTileSourceCount = 16, ObjectTileSourceCount = 19;
		private static string GetDataPath() {
			RegistryKey nexusKey = null;
			string dataPath;
			try {
				nexusKey = Registry.CurrentUser.OpenSubKey(@"Software\Nexon\Kingdom of the Winds");
				if(nexusKey == null) throw new Exception("Could not find NexusTK registry information.");
				string nexusPath = (string)nexusKey.GetValue("Location");
				if(nexusPath == null) throw new Exception("Could not find \"Location\" registry key.");
				dataPath = Path.Combine(nexusPath, "data");
			} finally {
				if(nexusKey != null) nexusKey.Close();
			}
			return dataPath;
		}
		public static TileManager TEMP_floorTileManager; // TEMP: Global TileManager for convenience
		[STAThread()]
		static void Main(string[] args) {
			// TEMP: In the final version, the DataPath should only be retreived when necessary
			TileManager floorTileManager, objectTileManager;
			try {
				Settings.Global.Default.DataPath = GetDataPath();
				if(!Directory.Exists(Settings.Global.Default.DataPath))
					throw new Exception("Data path does not exist.");
				floorTileManager = new TileManager(TileType.FloorTile,
					FloorTileSourceTag, FloorTileSourceCount);
				objectTileManager = new TileManager(TileType.ObjectTile,
					ObjectTileSourceTag, ObjectTileSourceCount);
			} catch(Exception exception) {
				MessageBox.Show(exception.Message);
				// TODO: Descriptive error message and option to choose a data path manually.
				return;
			}
			File.Delete("Aesir.log");
			//Trace.Listeners.Add(new ConsoleTraceListener());
			Trace.Listeners.Add(new TextWriterTraceListener("Aesir.log"));
			TEMP_floorTileManager = floorTileManager;
			MainForm mainForm = new MainForm(floorTileManager, objectTileManager);
			Application.Run(mainForm);
			Settings.Global.Default.Save();
		}
	}
}