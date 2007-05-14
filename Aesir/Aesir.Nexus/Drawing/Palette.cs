using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Aesir.Nexus.Drawing {
	/// <summary>
	///		A palette maps byte values to full colors. In EPF files, palettes are used to compress
	///		graphics.
	/// </summary>
	public class Palette {
		public Palette(Color[] entries) {
			this.entries = entries;
		}
		public Palette() {
			entries = new Color[256];
		}
		private Color[] entries;
		public Color[] Entries { get { return entries; } }
	}
}