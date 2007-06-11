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

// NOTE: My Computer\HKEY_CURRENT_USER\Software\Nexon\Kingdom of the Winds\Location
namespace Aesir {
	class Program {
		private const string FloorTileSourceTag = "tile", ObjectTileSourceTag = "tilec";
		private const int FloorTileSourceCount = 16, ObjectTileSourceCount = 19;
		[STAThread()]
		static void Main(string[] args) {
			TileManager floorTileManager, objectTileManager;
			try {
				floorTileManager = new TileManager(TileType.FloorTile,
					FloorTileSourceTag, FloorTileSourceCount);
				objectTileManager = new TileManager(TileType.ObjectTile,
					ObjectTileSourceTag, ObjectTileSourceCount);
			} catch(IOException exception) {
				MessageBox.Show(exception.Message);
				// TODO: Descriptive error message and option to choose a data path manually.
				return;
			}
			MainForm mainForm = new MainForm(floorTileManager, objectTileManager);
			Application.Run(mainForm);
			Settings.Global.Default.Save();
		}
	}
}