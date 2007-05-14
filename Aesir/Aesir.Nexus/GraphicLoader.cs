using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using Aesir.Nexus.Drawing;

namespace Aesir.Nexus {
	/// <summary>
	///		A graphic loader loads graphics from a series of EPF files.
	/// </summary>
	/// <example>
	///		GraphicLoader.ISourceProvider sourceProvider =
	///			new GraphicLoader.SimpleSourceProvider("tile", 16);
	///		PaletteTable paletteTable;
	///		using(DatReader datReader = new DatReader("tile.dat"))
	///			paletteTable = new PaletteTable(datReader, "tile");
	///		GraphicLoader graphicLoader = new GraphicLoader(paletteTable, sourceProvider);
	///		Bitmap bitmap = graphicLoader.LoadGraphic(2051);
	/// </example>
	public class GraphicLoader {
		public interface ISourceProvider {
			/// <summary>
			///		Gets the number of sources this provider encapsulates.
			/// </summary>
			int SourceCount { get; }
			/// <summary>
			///		Gets a stream that can be used to read the specified EPF file.
			/// </summary>
			/// <param name="disposable">
			///		An <c>IDisposable</c> whose ownership will be passed to the caller. This may be null.
			///	</param>
			Stream GetSourceStream(uint index, out IDisposable disposable);
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
			public Stream GetSourceStream(uint index, out IDisposable disposable) {
				Debug.Assert(index < sourceCount);
				string path = string.Format("{0}{1}.dat", sourceTag, index);
				DatReader datReader = new DatReader(path);
				DatEntry datEntry;
				try {
					string epfName = Path.GetFileNameWithoutExtension(path) + ".epf";
					datEntry = datReader.GetEntry(epfName);
					if(datEntry == null) throw new Exception("Could not locate the EPF file.");
				} catch(Exception exception) {
					datReader.Dispose();
					throw exception;
				}
				disposable = datReader;
				return datEntry.GetLocalStream(datReader.BaseStream);
			}
		}
		/// <summary>
		///		Gets or sets a value that determines whether the <c>GraphicLoader</c> will cache
		///		the <c>EpfHeader</c>s for the sources.
		/// </summary>
		public bool CacheSourceHeaders {
			get { return cacheSourceHeaders; }
			set {
				cacheSourceHeaders = value;
				if(cacheSourceHeaders) LoadSourceHeaders();
				else sourceHeaders = null;
			}
		}
		private bool cacheSourceHeaders = false;
		private EpfHeader[] sourceHeaders;
		private void LoadSourceHeaders() {
			sourceHeaders = new EpfHeader[sourceProvider.SourceCount];
			for(uint index = 0; index < sourceProvider.SourceCount; ++index) {
				IDisposable disposable;
				Stream sourceStream = sourceProvider.GetSourceStream(index, out disposable);
				try {
					sourceHeaders[index] = new EpfHeader(sourceStream);
				} finally {
					if(disposable != null) disposable.Dispose();
					sourceStream.Dispose();
				}
			}
		}
		public GraphicLoader(PaletteTable paletteTable, ISourceProvider sourceProvider) {
			this.paletteTable = paletteTable;
			this.sourceProvider = sourceProvider;
			CacheSourceHeaders = true;
		}
		private ISourceProvider sourceProvider;
		private PaletteTable paletteTable;
		public Bitmap LoadGraphic(uint index) {
			EpfGraphicInfo graphicInfo;
			return LoadGraphic(index, out graphicInfo);
		}
		public Bitmap LoadGraphic(uint index, out EpfGraphicInfo graphicInfo) {
			EpfHeader sourceHeader = sourceHeaders[0];
			uint baseIndex = 0, sourceIndex = 0;
			while(true) {
				if(index < (baseIndex + sourceHeader.GraphicCount)) break;
				baseIndex += sourceHeader.GraphicCount;
				sourceHeader = sourceHeaders[++sourceIndex];
			}
			uint localIndex = index - baseIndex;
			IDisposable disposable;
			Stream sourceStream = sourceProvider.GetSourceStream(sourceIndex, out disposable);
			Bitmap bitmap;
			try {
				EpfReader epfReader = new EpfReader(sourceStream, sourceHeader);
				Palette palette = paletteTable[index];
				bitmap = epfReader.ReadGraphic(localIndex, palette, out graphicInfo);
			} finally {
				if(disposable != null) disposable.Dispose();
				sourceStream.Dispose();
			}
			return bitmap;
		}
	}
}