using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Collections;
using System.Threading;
using Aesir.Nexus;
using Aesir.Util;
using System.Drawing.Imaging;

// TODO: Quit gracefully when we can't find the tile archives in the data path
// TODO: Implement priorities for tile loading
// TODO: Make it so that tiles are not immediately unloaded
namespace Aesir {
	/// <summary>
	///		A <c>TileHandle</c> is used to refer to a <c>Tile</c> that was retrieved from a
	///		<c>TileManager</c>.
	/// </summary>
	// This class is part of a reference counted tile management mechanism. In the constructor,
	// this class increments the reference count of the referenced tile. In the Dispose method,
	// this class decrements the reference count.
	class TileHandle<TTile> : IDisposable, ICloneable where TTile : Tile {
		internal TileHandle(TTile tile) {
			this.tile = tile;
			++tile.Refcount;
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing) {
			if(!disposed) {
				disposed = true;
				--tile.Refcount;
			}
		}
		public object Clone() {
			return new TileHandle<TTile>(tile);
		}
		public static implicit operator Tile(TileHandle<TTile> tileHandle) {
			if(tileHandle == null) throw new InvalidCastException("The TileHandle was null.");
			return tileHandle.tile;
		}
		private bool disposed = false;
		private TTile tile;
	}
	abstract class Tile : IDisposable {
		private object syncRoot = new Object();
		public object SyncRoot { get { return syncRoot; } }
		/// <summary>
		///		This event handler is invoked when the tile is loaded. Note that loading is an
		///		asynchronous operation.
		/// </summary>
		public event EventHandler Load;
		/// <summary>
		///		This event handler is invoked when the tile is disposed or released. This event
		///		handler will not be invoked if the tile was never loaded in the first place.
		/// </summary>
		public event EventHandler Release;
		internal void Create(Image image) {
			this.image = image;
			if(Load != null) Load(this, EventArgs.Empty);
		}
		internal void OnRelease() {
			if(Release != null) Release(this, EventArgs.Empty);
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing) {
			if(!disposed) {
				if(image != null) {
					image.Dispose();
					image = null;
					OnRelease();
				}
			}
		}
		private bool disposed = false;
		public void Draw(Graphics graphics, Point point) {
			if(disposed) throw new ObjectDisposedException("Tile");
			lock(SyncRoot)
				graphics.DrawImage(image, point);
		}
		~Tile() { Dispose(false); }
		private int index;
		public int Index {
			get { return index; }
			internal set { index = value; }
		}
		protected Image image = NullImage;
		private static readonly Image NullImage = new Bitmap(@"null.png");
		private int refcount = 0;
		internal int Refcount {
			get { return refcount; }
			set {
				refcount = value;
				if(refcount <= 0) Dispose();
			}
		}
		public static Size Size { get { return new Size(Width, Height); } }
		public const int Width = 48, Height = 48;
	}
	abstract class TileManagerBase {
		protected static TaskThread taskThread = new TaskThread();
		protected static readonly Image BlankImage = new Bitmap(Tile.Width, Tile.Height);
	}
	class TileManager<TTile> : TileManagerBase where TTile : Tile, new() {
		public TileManager(string sourceTag, int sourceCount) {
			string dataPath = Settings.Default.DataPath;
			string archivePath = Path.Combine(dataPath, "tile.dat");
			string paletteCollectionName = sourceTag + "." + PaletteCollection.fileExtension,
				paletteTableName = sourceTag + "." + PaletteTable.fileExtension;
			PaletteCollection paletteCollection;
			PaletteTable paletteTable;
			using(FileStream archiveStream = new FileStream(archivePath, FileMode.Open)) {
				ArchiveInfo archive = new ArchiveInfo(archiveStream);
				archiveStream.Seek(archive.GetFile(paletteCollectionName).Offset, SeekOrigin.Begin);
				paletteCollection = PaletteCollection.FromStream(archiveStream);
				archiveStream.Seek(archive.GetFile(paletteTableName).Offset, SeekOrigin.Begin);
				paletteTable = PaletteTable.FromStream(archiveStream);
			}
			GraphicLoader.ISourceProvider sourceProvider =
				new GraphicLoader.SimpleSourceProvider(sourceCount, Path.Combine(dataPath, sourceTag));
			graphicLoader = new GraphicLoader(paletteCollection, paletteTable, sourceProvider);

			blankTile = new TTile();
			blankTile.Create((Image)BlankImage.Clone());
			blankTile.Index = 0;
			tiles.Add(0, blankTile);
		}
		public TileHandle<TTile> GetTile(int index) {
			if(tiles.ContainsKey(index))
				return new TileHandle<TTile>(tiles[index]);
			TTile tile = new TTile();
			tile.Index = index;
			tile.Release += delegate(object sender, EventArgs args) {
				TTile senderTile = (TTile)sender;
				tiles.Remove(senderTile.Index);
			};
			tiles.Add(index, tile);
			taskThread.DoTask(
				delegate() { return graphicLoader.LoadGraphic(index); },
				delegate(object args) { tile.Create((Image)args); }
			);
			return new TileHandle<TTile>(tile);
		}
		private readonly TTile blankTile;
		private GraphicLoader graphicLoader;
		protected static TileManager<TTile> defaultInstance;
		public static TileManager<TTile> Default { get { return defaultInstance; } }
		private Dictionary<int, TTile> tiles = new Dictionary<int, TTile>();
	}
	class ObjectTile : Tile { }
	class FloorTile : Tile { }
	class FloorTileManager : TileManager<FloorTile> {
		private FloorTileManager() : base("tile", 16) { }
		public static void Initialize() { defaultInstance = new FloorTileManager();  }
	}
	class ObjectTileManager : TileManager<ObjectTile> {
		private ObjectTileManager() : base("tilec", 19) { }
		public static void Initialize() { defaultInstance = new ObjectTileManager(); }
	}
}