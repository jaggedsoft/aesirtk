using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace Aesir.Nexus {
	/// <remarks>
	///		In every EPF file there is an array of <c>GraphicInfo</c> structures. The offset of
	///		this array is specified by <c>GraphicCollectionHeader.InfoOffset</c>.
	/// </remarks>
	class GraphicInfo {
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
		/// The size of this structure as it is stored in an EPF file, in bytes
		public const int DataSize = 16;
		public int Width {
			get { return width; }
		}
		public int Height {
			get { return height; }
		}
		/// <summary>
		///		The offset to the actual graphic data, specified relative to the end of the EPF
		///		header.
		/// </summary>
		public int DataOffset {
			get { return dataOffset; }
		}
		private int dataOffset, width, height;
		public ushort left, top, right, bottom;
	}
	/// <summary>
	///		The header of an EPF file.
	/// </summary>
	class GraphicCollectionHeader {
		private ushort graphicCount;
		private int infoOffset;
		/// <summary>
		///		The offset, specified relative to the end of the EPF header, to the array of
		///		<c>GraphicInfo</c> structures stored in this EPF file.
		/// </summary>
		public int InfoOffset {
			get { return infoOffset; }
		}
		public ushort GraphicCount {
			get { return graphicCount; }
		}
		public static GraphicCollectionHeader FromStream(Stream stream) {
			BinaryReader binaryReader = new BinaryReader(stream);
			GraphicCollectionHeader header = new GraphicCollectionHeader();
			header.graphicCount = binaryReader.ReadUInt16();
			stream.Seek(6, SeekOrigin.Current); // Skip over unknown bytes
			header.infoOffset = (int)binaryReader.ReadUInt32();
			return header;
		}
		// The size of this structure as it is stored in an EPF file, in bytes
		public const int DataSize = 12;
	}
	class GraphicLoader {
		/// <summary>
		/// A graphic loader will usually load its graphics from several EPF files, which are
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
		///		tile1.epf, etc. In this case, the <c>SourceTag</c> would be "tile".
		/// </summary>
		public class SourceProvider : ISourceProvider {
			public SourceProvider(int sourceCount, string sourceTag) {
				this.sourceCount = sourceCount;
				this.sourceTag = sourceTag;
			}
			private string sourceTag;
			public string SourceTag {
				get { return sourceTag; }
			}
			private int sourceCount;
			public int SourceCount {
				get { return sourceCount; }
			}
			public Stream GetSourceStream(int index) {
				Debug.Assert(index < sourceCount);
				string path = string.Format("{0}{1}.dat", sourceTag, index);
				FileStream stream = new FileStream(path, FileMode.Open);
				ArchiveHeader archive = new ArchiveHeader(stream);
				string entryName = Path.GetFileNameWithoutExtension(path) + ".epf";
				// TODO: Throw an exception if the entry is null?
				stream.Seek(archive.GetEntry(entryName).Offset, SeekOrigin.Begin);
				return stream;
			}
		}
		public GraphicLoader(PaletteCollection paletteCollection, PaletteTable paletteTable,
			ISourceProvider sourceProvider) {
			this.paletteCollection = paletteCollection;
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
		private PaletteCollection paletteCollection;
		private PaletteTable paletteTable;
		/// <remarks>
		/// The resultant image will always be <c>Graphic.Width</c> by <c>Graphic.Height</c>, and
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
				if(index < (baseIndex + sourceHeader.GraphicCount)) break;
				baseIndex += sourceHeader.GraphicCount;
				sourceHeader = sourceHeaders[++sourceIndex];
			}
			int localIndex = index - baseIndex;
			using(Stream sourceStream = sourceProvider.GetSourceStream(sourceIndex)) {
				// Skip over the EPF header; offsets are relative to the end of the header
				sourceStream.Seek(GraphicCollectionHeader.DataSize, SeekOrigin.Current);
				// Save the offset that points to the data after the header
				int baseOffset = (int)sourceStream.Position;
				// Seek to the GraphicInfo structure and read it in
				int infoOffset = sourceHeader.InfoOffset + (GraphicInfo.DataSize * localIndex);
				sourceStream.Seek(infoOffset, SeekOrigin.Current);
				GraphicInfo info = GraphicInfo.FromStream(sourceStream);
				// Seek to the actual graphic data
				sourceStream.Seek(baseOffset + info.DataOffset, SeekOrigin.Begin);
				Palette palette = paletteCollection[paletteTable[index]];

				// Read the graphic data
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