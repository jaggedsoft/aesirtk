using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using Aesir.Nexus;
using Microsoft.Win32;
using Aesir.Util;

// NOTE: My Computer\HKEY_CURRENT_USER\Software\Nexon\Kingdom of the Winds\Location
namespace Aesir {
	class Program {
		private static MainForm mainForm;
		public static MainForm MainForm { get { return mainForm; } }
		private static FloorTileManager floorTileManager;
		public static FloorTileManager FloorTileManager { get { return floorTileManager; } }
		private static ObjectTileManager objectTileManager;
		public static ObjectTileManager ObjectTileManager { get { return objectTileManager; } }
		[STAThread()]
		static void Main(string[] args) {
			floorTileManager = new FloorTileManager();
			objectTileManager = new ObjectTileManager();
			mainForm = new MainForm();
			Application.Run(mainForm);
		}
	}
}