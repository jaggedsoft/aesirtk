using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

namespace Aesir.Nexus {
	class PaletteException : Exception {
		public PaletteException(string message) : base(message) { }
	}
	/// <summary>
	///		A collection of palettes, as stored in a PAL file.
	/// </summary>
	class PaletteCollection {
		public static PaletteCollection FromStream(Stream stream) {
			BinaryReader binaryReader = new BinaryReader(stream);
			// The first byte is the number of palettes in the collection
			int count = binaryReader.ReadByte();
			stream.Seek(3, SeekOrigin.Current); // Three unknown bytes (padding?)
			Palette[] palettes = new Palette[count];
			// Load the palettes, which are stored sequentially in the PAL file
			for(int index = 0; index < count; ++index) {
				try {
					palettes[index] = Palette.FromStream(stream);
				} catch(PaletteException exception) {
					StringBuilder message = new StringBuilder();
					message.Append("Error while loading palette " + index + " of " + count);
					message.Append(" (" + exception.Message + ")");
					throw new PaletteException(message.ToString());
				}
			}
			return new PaletteCollection(palettes);
		}
		private PaletteCollection(Palette[] palettes) {
			this.palettes = palettes;
		}
		public Palette this[int index] {
			get { return palettes[index]; }
		}
		private Palette[] palettes;
		public const string FileExtension = "pal";
	}
	/// <summary>
	///		A palette table maps absolute graphic indices to their associated palette.
	/// </summary>
	class PaletteTable {
		public static PaletteTable FromStream(Stream stream) {
			BinaryReader binaryReader = new BinaryReader(stream);
			int count = binaryReader.ReadUInt16();
			stream.Seek(2, SeekOrigin.Current);
			int[] entries = new int[count];
			for(int index = 0; index < count; ++index) {
				byte buffer = binaryReader.ReadByte();
				stream.Seek(1, SeekOrigin.Current);
				entries[index] = buffer;
			}
			return new PaletteTable(entries);
		}
		private PaletteTable(int[] entries) {
			this.entries = entries;
		}
		private int[] entries;
		public int this[int index] {
			get { return entries[index]; }
		}
		public const string FileExtension = "tbl";
	}
	/// <summary>
	///		A palette maps byte values to a corrosponding color.
	/// </summary>
	class Palette {
		private const string validHeaderMagic = "DLPalette";
		private Color[] entries;
		public Color[] Entries {
			get { return entries; }
		}
		public static Palette FromStream(Stream stream) {
			// Every palette should start with the magic header "DLPalette" in ASCII
			BinaryReader binaryReader = new BinaryReader(stream);
			byte[] headerMagicBuffer = new byte[validHeaderMagic.Length + 1];
			headerMagicBuffer[validHeaderMagic.Length] = 0;
			binaryReader.Read(headerMagicBuffer, 0, validHeaderMagic.Length);
			string headerMagic = Encoding.ASCII.GetString(headerMagicBuffer);
			if(headerMagic.CompareTo(validHeaderMagic) != 0)
				throw new PaletteException("Invalid palette header");
			stream.Seek(15, SeekOrigin.Current); // Skip over some unknown bytes
			byte type = binaryReader.ReadByte();
			switch(type) {
			case 3: stream.Seek(13, SeekOrigin.Current); break;
			case 1: stream.Seek(9, SeekOrigin.Current); break;
			case 2: stream.Seek(11, SeekOrigin.Current); break;
			case 4: stream.Seek(15, SeekOrigin.Current); break;
			default: stream.Seek(7, SeekOrigin.Current); break;
			}
			// Read in the colors which are stored sequentially
			Color[] entries = new Color[256];
			for(int index = 0; index < 256; ++index) {
				byte[] colorData = binaryReader.ReadBytes(4);
				entries[index] = Color.FromArgb(colorData[0], colorData[1], colorData[2]);
			}
			return new Palette(entries);
		}
		public Palette(Color[] entries) {
			this.entries = entries;
		}
	}
}