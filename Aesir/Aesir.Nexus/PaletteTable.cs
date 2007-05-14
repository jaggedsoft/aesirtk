using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Aesir.Nexus.Drawing;

namespace Aesir.Nexus {
	/// <summary>
	///		A palette table can be used to look up the palette associated with a graphic at a
	///		specified absolute index. A pallete table consists of two parts: a collection of
	///		palettes (which must be read using a <c>PalReader</c> and then provided in the
	///		constructor), and an array that maps graphic indices to palette indices. This latter
	///		part is loaded from a TBL file using the <c>Load</c> method.
	/// </summary>
	public class PaletteTable {
		/// <summary>
		///		Construct a palette table from an array of palettes. You will still need to call
		///		<c>Load</c> to load the actual table data.
		/// </summary>
		public PaletteTable(Palette[] palettes) {
			this.palettes = palettes;
		}
		/// <summary>
		///		Construct a <c>PaletteTable</c> using a PAL file and a TBL file within the provided
		///		archive whose names are specified by <c>tag</c>.
		/// </summary>
		/// <param name="tag">
		///		The extensionless file name of the PAL and TBL file. For example, if this is "tile",
		///		the PAL file will be "tile.pal" and the TBL file will be "tile.tbl".
		/// </param>
		public PaletteTable(DatReader datReader, string tag) {
			DatEntry palEntry = datReader.GetEntry(tag + ".pal");
			DatEntry tblEntry = datReader.GetEntry(tag + ".tbl");
			{
				Stream palStream = palEntry.GetLocalStream(datReader.BaseStream);
				PalReader palReader = new PalReader(palStream);
				palettes = palReader.ReadPalettes();
			}
			{
				Stream tblStream = tblEntry.GetLocalStream(datReader.BaseStream);
				Load(tblStream);
			}
		}
		private void Load(Stream stream) {
			BinaryReader binaryReader = new BinaryReader(stream);
			int count = binaryReader.ReadUInt16();
			stream.Seek(2, SeekOrigin.Current); // Skip unknown bytes
			entries = new int[count];
			for(int index = 0; index < count; ++index) {
				byte buffer = binaryReader.ReadByte();
				stream.Seek(1, SeekOrigin.Current); // Skip unknown bytes
				entries[index] = buffer;
			}
		}
		/// <summary>
		///		This indexer will return the palette associated with the graphic at the specified
		///		absolute index.
		/// </summary>
		public Palette this[uint index] {
			get { return palettes[entries[index]]; }
		}
		private int[] entries;
		private Palette[] palettes;
	}
}