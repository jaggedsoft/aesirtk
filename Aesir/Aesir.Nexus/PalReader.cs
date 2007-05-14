using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Aesir.Nexus.Drawing;
using System.IO;

namespace Aesir.Nexus {
	/// <summary>
	///		This class allows you to read data from Nexus's PAL files, which store a collection
	///		of palettes.
	/// </summary>
	/// <see cref="Aesir.Nexus.Drawing.Palette"/>
	public class PalReader {
		public PalReader(Stream stream) {
			this.stream = stream;
		}
		private const string validHeaderMagic = "DLPalette";
		private void ReadPalette(Palette palette) {
			// Every palette should start with the magic header "DLPalette" in ASCII
			BinaryReader binaryReader = new BinaryReader(stream);
			byte[] headerMagicBuffer = new byte[validHeaderMagic.Length];
			binaryReader.Read(headerMagicBuffer, 0, validHeaderMagic.Length);
			string headerMagic = Encoding.ASCII.GetString(headerMagicBuffer);
			if(headerMagic != validHeaderMagic) {
				string message = string.Format("Invalid header (should be \"{0}\").",
					validHeaderMagic);
				throw new Exception(message);
			}
			stream.Seek(15, SeekOrigin.Current); // Skip unknown bytes
			byte type = binaryReader.ReadByte();
			switch(type) {
			case 3: stream.Seek(13, SeekOrigin.Current); break;
			case 1: stream.Seek(9, SeekOrigin.Current); break;
			case 2: stream.Seek(11, SeekOrigin.Current); break;
			case 4: stream.Seek(15, SeekOrigin.Current); break;
			default: stream.Seek(7, SeekOrigin.Current); break;
			}
			// Read the palette entries, which are stored sequentially
			for(int index = 0; index < 256; ++index) {
				byte[] colorData = binaryReader.ReadBytes(4);
				Color color = Color.FromArgb(colorData[0], colorData[1], colorData[2]);
				palette.Entries[index] = color;
			}
		}
		public Palette[] ReadPalettes() {
			BinaryReader binaryReader = new BinaryReader(stream);
			// The first byte is the number of palettes in the collection
			int count = binaryReader.ReadByte();
			stream.Seek(3, SeekOrigin.Current); // Skip unknown bytes
			Palette[] palettes = new Palette[count];
			// Load the palettes, which are stored sequentially in the PAL file
			for(int index = 0; index < count; ++index) {
				try {
					palettes[index] = new Palette();
					ReadPalette(palettes[index]);
				} catch(Exception exception) {
					string message = string.Format("Error while loading palette {0} of {1}.",
						index, count);
					throw new Exception(message, exception);
				}
			}
			return palettes;
		}
		/// <summary>
		///		Gets the underlying <c>Stream</c>.
		/// </summary>
		public Stream BaseStream { get { return stream; } }
		private Stream stream;
	}
}