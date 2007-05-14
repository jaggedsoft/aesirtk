using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

namespace Aesir.Nexus {
	/// <summary>
	///		Information about a graphic in an EPF file.
	/// </summary>
	/// <see cref="Aesir.Nexus.EpfHeader"/>
	/// <see cref="Aesir.Nexus.EpfReader"/>
	public class EpfGraphicInfo {
		internal EpfGraphicInfo() { }
		internal uint Load(EpfHeader header, uint index, Stream stream) {
			BinaryReader binaryReader = new BinaryReader(stream);
			stream.Seek(header.GraphicInfoOffset, SeekOrigin.Begin);
			stream.Seek(EpfHeader.DataSize + DataSize * (index - 1), SeekOrigin.Current);
			return Read(binaryReader);
		}
		// The size, in bytes, of this structure in the EPF file
		private const uint DataSize = 16;
		private uint Read(BinaryReader binaryReader) {
			ushort top = binaryReader.ReadUInt16();
			ushort left = binaryReader.ReadUInt16();
			ushort bottom = binaryReader.ReadUInt16();
			ushort right = binaryReader.ReadUInt16();
			point = new Point(left, top);
			size = new Size(right - left, bottom - top);
			// HACK: This will break on anything but tile graphics
			if(size.Width == 0) size.Width = 48;
			if(size.Height == 0) size.Height = 48;
			uint startOffset = binaryReader.ReadUInt32();
			uint endOffset = binaryReader.ReadUInt32();
			return startOffset;
		}
		private Point point;
		private Size size;
		/// <summary>
		///		Gets the size of this graphic.
		/// </summary>
		public Size Size { get { return size; } }
		/// <summary>
		///		Gets the upper left corner of this graphic.
		/// </summary>
		public Point Point { get { return point; } }
		public int Width { get { return size.Width; } }
		public int Height { get { return size.Height; } }
	}
}