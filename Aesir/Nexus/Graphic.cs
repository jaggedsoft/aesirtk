using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace Aesir.Nexus {
	/// <summary>
	/// Information about a graphic, such as the width, the height, the palette, and the offset to
	/// the actual graphic data.
	/// </summary>
	/// <remarks>
	/// In every EPF file there is an array of GraphicInfo structures. The offset of this array is
	/// specified by <c>GraphicCollectionHeader.InfoOffset</c>.
	/// </remarks>
	class GraphicInfo {
		/// <summary>
		/// Read GraphicInfo data from the stream.
		/// </summary>
		public static GraphicInfo FromStream(Stream stream) {
			BinaryReader binaryReader = new BinaryReader(stream);
			GraphicInfo info = new GraphicInfo();
			info.top = binaryReader.ReadUInt16();
			info.left = binaryReader.ReadUInt16();
			info.bottom = binaryReader.ReadUInt16();
			info.right = binaryReader.ReadUInt16();
			info.width = Math.Abs(info.right - info.left);
			info.height = Math.Abs(info.bottom - info.top);
			if(info.height == 0) info.height = Graphic.Height;
			if(info.width == 0) info.width = Graphic.Width;
			int startOffset = (int)binaryReader.ReadUInt32();
			int endOffset = (int)binaryReader.ReadUInt32();
			info.dataOffset = startOffset;
			return info;
		}
		/// <summary>The size of this structure as it is stored in an EPF file, in bytes.</summary>
		public const int DataSize = 16;
		public int Width { get { return width; } }
		public int Height { get { return height; } }
		public int DataOffset { get { return dataOffset; } }
		private int dataOffset, width, height;
		public ushort left, top, right, bottom;
	}
	/// <summary>
	/// The header of an EPF file. An EPF file contains graphic data such as tiles.
	/// </summary>
	class GraphicCollectionHeader {
		private ushort tileCount;
		private int infoOffset;
		/// <summary>
		/// The offset, specified relative to the end of the EPF header, to the array of
		/// GraphicInfo structures stored in this EPF file.
		/// </summary>
		public int InfoOffset { get { return infoOffset; } }
		/// <summary>The number of tiles stored in this EPF file.</summary>
		public ushort TileCount { get { return tileCount; } }
		public static GraphicCollectionHeader FromStream(Stream stream) {
			BinaryReader binaryReader = new BinaryReader(stream);
			GraphicCollectionHeader header = new GraphicCollectionHeader();
			header.tileCount = binaryReader.ReadUInt16();
			stream.Seek(6, SeekOrigin.Current); // Skip over unknown bytes
			header.infoOffset = (int)binaryReader.ReadUInt32();
			return header;
		}
		/// <summary>The size of this structure as it is stored in an EPF file, in bytes.</summary>
		public const int DataSize = 12;
	}
	class GraphicLoader {
		/// <summary>
		/// A graphic loader will usually gather its graphics from several EPF files, which are
		/// usually stored in archives. These EPF files are called sources. This interface provides
		/// sources to clients.
		/// </summary>
		public interface ISourceProvider {
			int SourceCount { get; }
			Stream GetSourceStream(int index);
		}
		/// <summary>
		///		This concrete source provider provides sources that are stored within an archive
		///		and numbered sequentially. For example, tiles are stored in a series of archives
		///		tile0.dat, tile1.dat, etc. Within these archives are the EPF files tile0.epf,
		///		tile1.epf, etc. In this case, the <c>PathPrefix</c> would be "tile".
		/// </summary>
		public class SimpleSourceProvider : ISourceProvider {
			public SimpleSourceProvider(int sourceCount, string sourceTag) {
				this.sourceCount = sourceCount;
				this.sourceTag = sourceTag;
			}
			private string sourceTag;
			public string SourceTag { get { return sourceTag; } }
			private int sourceCount;
			public int SourceCount { get { return sourceCount; } }
			public Stream GetSourceStream(int index) {
				Debug.Assert(index < sourceCount);
				string path = string.Format("{0}{1}.dat", sourceTag, index);
				FileStream stream = new FileStream(path, FileMode.Open);
				ArchiveInfo archive = new ArchiveInfo(stream);
				string graphicFileName = Path.GetFileNameWithoutExtension(path) + ".epf";
				stream.Seek(archive.GetFile(graphicFileName).Offset, SeekOrigin.Begin);
				return stream;
			}
		}
		public GraphicLoader(IPaletteProvider paletteProvider, PaletteTable paletteTable,
			ISourceProvider sourceProvider) {
			this.paletteProvider = paletteProvider;
			this.paletteTable = paletteTable;
			this.sourceProvider = sourceProvider;
			// Precache the EPF header information
			sourceHeaders = new GraphicCollectionHeader[sourceProvider.SourceCount];
			for(int index = 0; index < sourceProvider.SourceCount; ++index) {
				using(Stream stream = sourceProvider.GetSourceStream(index))
					sourceHeaders[index] = GraphicCollectionHeader.FromStream(stream);
			}
		}
		private GraphicCollectionHeader[] sourceHeaders;
		private ISourceProvider sourceProvider;
		private IPaletteProvider paletteProvider;
		private PaletteTable paletteTable;
		/// <remarks>
		/// The resultant image will always be <c>Graphic.width</c> by <c>Graphic.height</c>, and
		/// may contain transparency information in the case of object tiles.
		/// </remarks>
		public Image LoadGraphic(int index) {
			Bitmap bitmap = new Bitmap(Graphic.Width, Graphic.Height, PixelFormat.Format32bppArgb);
			using(Graphics graphics = Graphics.FromImage(bitmap)) {
				Rectangle rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
				graphics.FillRectangle(Brushes.Transparent, rectangle);
			}
			if(index == 0) return bitmap;

			GraphicCollectionHeader sourceHeader = sourceHeaders[0];
			int baseIndex = 0, sourceIndex = 0;
			while(true) {
				if(index < (baseIndex + sourceHeader.TileCount)) break;
				baseIndex += sourceHeader.TileCount;
				sourceHeader = sourceHeaders[++sourceIndex];
			}
			int localIndex = index - baseIndex;
			using(Stream sourceStream = sourceProvider.GetSourceStream(sourceIndex)) {
				// Skip over the EPF header; offsets are relative to the end of the header
				sourceStream.Seek(GraphicCollectionHeader.DataSize, SeekOrigin.Current);
				int baseOffset = (int)sourceStream.Position;
				int infoOffset = sourceHeader.InfoOffset + (GraphicInfo.DataSize * localIndex);
				sourceStream.Seek(infoOffset, SeekOrigin.Current);
				GraphicInfo info = GraphicInfo.FromStream(sourceStream);
				sourceStream.Seek(baseOffset, SeekOrigin.Begin);
				sourceStream.Seek(info.DataOffset, SeekOrigin.Current);
				int paletteIndex = paletteTable[index];
				Palette palette = paletteProvider.GetPalette(paletteIndex);

				BinaryReader binaryReader = new BinaryReader(sourceStream);
				if(index == 0) return bitmap;
				for(int y = 0; y < info.Height; ++y) {
					for(int x = 0; x < info.Width; ++x) {
						byte buffer = binaryReader.ReadByte();
						Color color;
						if(buffer == 0) color = Color.Transparent;
						else color = palette.Entries[buffer];
						bitmap.SetPixel(x + info.left, y + info.top, color);
					}
				}
			}
			return bitmap;
		}
	}
	class Graphic {
		public static readonly Size Size = new Size(48, 48);
		public static readonly int Width = Size.Width, Height = Size.Height;
	}
}