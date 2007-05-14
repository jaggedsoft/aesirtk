using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Aesir.Nexus.Drawing;
using System.Drawing;
using System.Drawing.Imaging;

namespace Aesir.Nexus {
	/// <summary>
	///		This class allows you to read data from an EPF file, which is used to store graphics
	///		in Nexus.
	/// </summary>
	/// <remarks>
	///		Graphics in an EPF file are referred to by an index, according to the order in which they
	///		are stored. Note that the "local" index may not correspond to the "absolute" index. The
	///		"absolute" index is used by the <c>PaletteTable</c> class. It is best to use a
	///		<c>GraphicLoader</c> to load graphics, because it will convert "absolute" indices to
	///		"local" indices.
	/// </remarks>
	/// <example>
	///		Bitmap graphic;
	///		uint graphicIndex = 15;
	///		PaletteTable paletteTable;
	///		// First read the <c>PaletteTable</c> from the main archive
	///		using(DatReader datReader = new DatReader("tile.dat"))
	///			paletteTable = new PaletteTable(datReader, "tile");
	///		// Then set up the <c>EpfReader</c> and read the graphic
	///		using(DatReader datReader = new DatReader("tile0.dat")) {
	///			DatEntry epfEntry = datReader.GetEntry("tile0.epf");
	///			Stream epfStream = epfEntry.GetLocalStream(datReader.BaseStream);
	///			EpfReader epfReader = new EpfReader(epfStream);
	///			Palette palette = paletteTable[graphicIndex];
	///			graphic = epfReader.ReadGraphic(graphicIndex, palette);
	///		}
	///		// Now you can do something with <c>graphic</c>
	/// </example>
	/// <see cref="Aesir.Nexus.EpfHeader"/>
	/// <see cref="Aesir.Nexus.EpfGraphicInfo"/>
	public class EpfReader {
		public EpfReader(Stream stream) {
			this.stream = stream;
			header = new EpfHeader(stream);
		}
		public EpfReader(Stream stream, EpfHeader header) {
			this.stream = stream;
			this.header = header;
		}
		public EpfGraphicInfo ReadGraphicInfo(uint index) {
			EpfGraphicInfo graphicInfo = new EpfGraphicInfo();
			graphicInfo.Load(header, index, stream);
			return graphicInfo;
		}
		public Bitmap ReadGraphic(uint index, Palette palette) {
			EpfGraphicInfo graphicInfo;
			return ReadGraphic(index, palette, out graphicInfo);
		}
		public Bitmap ReadGraphic(uint index, Palette palette, out EpfGraphicInfo graphicInfo) {
			graphicInfo = new EpfGraphicInfo();
			uint offset = graphicInfo.Load(header, index, stream);
			stream.Seek(offset, SeekOrigin.Begin);
			BinaryReader binaryReader = new BinaryReader(stream);
			Bitmap bitmap = new Bitmap(graphicInfo.Width, graphicInfo.Height, PixelFormat.Format32bppArgb);
			using(Graphics graphics = Graphics.FromImage(bitmap)) {
				Rectangle rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
				graphics.FillRectangle(Brushes.Transparent, rectangle);
			}
			if(index == 0) return bitmap;
			for(int y = 0; y < graphicInfo.Height; ++y) {
				for(int x = 0; x < graphicInfo.Width; ++x) {
					byte buffer = binaryReader.ReadByte();
					Color color;
					if(buffer == 0) color = Color.Transparent;
					else color = palette.Entries[buffer];
					bitmap.SetPixel(x, y, color);
				}
			}
			return bitmap;
		}
		private EpfHeader header;
		/// <summary>
		///		Gets the number of graphics stored in this EPF file.
		/// </summary>
		public uint GraphicCount { get { return header.GraphicCount; } }
		/// <summary>
		///		Gets the underlying <c>Stream</c>.
		/// </summary>
		public Stream BaseStream { get { return stream; } }
		private Stream stream;
	}
}
