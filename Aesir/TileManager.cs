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
using System.Configuration;

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
		~TileHandle() { Dispose(false); }
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
			if(tileHandle == null) throw new InvalidCastException("The TileHandle cannot be null.");
			return tileHandle.tile;
		}
		public override bool Equals(object obj) {
			return tile.Index == ((TileHandle<TTile>)obj).tile.Index;
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
				}
			}
		}
		private bool disposed = false;
		~Tile() { Dispose(false); }
		public void Draw(Graphics graphics, Point point) {
			if(disposed) throw new ObjectDisposedException("Tile");
			if(image == null)
				graphics.DrawImage(NullImage, point);
			else {
				lock(SyncRoot)
					graphics.DrawImage(image, point);
			}
		}
		public Image Image {
			get { return image; }
		}
		private int index;
		public int Index {
			get { return index; }
			internal set { index = value; }
		}
		protected Image image = null;
		private static readonly Image NullImage = new Bitmap(@"null.png");
		private int refcount = 0;
		internal int Refcount {
			get { return refcount; }
			set {
				refcount = value;
				if(refcount <= 0) OnRelease();
			}
		}
		public override string ToString() { return "{Index:" + index + "}"; }
		public static Size Size { get { return new Size(Width, Height); } }
		public const int Width = 48, Height = 48;
	}
	abstract class TileManagerBase {
		static TileManagerBase() {
			timer = new System.Threading.Timer(delegate(object state) {
				if(TimerTick != null) TimerTick(timer, EventArgs.Empty);
			}, null, 0, ReleasePeriod);
		}
		protected TileManagerBase() {
			TimerTick += delegate(object sender, EventArgs args) { Release(); };
		}
		protected abstract void Release();
		private static event EventHandler TimerTick;
		private static System.Threading.Timer timer;
		protected static TaskThread taskThread = new TaskThread();
		public static int ReleasePeriod {
			get { return settings.ReleasePeriod; }
		}
		private static Settings.TileManagerBase settings = Settings.TileManagerBase.Default;
		protected static readonly Image BlankImage = new Bitmap(Tile.Width, Tile.Height);
	}
	class TileManagerException : Exception { }
	class TileManager<TTile> : TileManagerBase where TTile : Tile, new() {
		public TileManager(string sourceTag, int sourceCount) {
			string dataPath = Settings.Global.Default.DataPath;
			string archivePath = Path.Combine(dataPath, "tile.dat");
			string paletteCollectionName = sourceTag + "." + PaletteCollection.fileExtension,
				paletteTableName = sourceTag + "." + PaletteTable.FileExtension;
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
			blankTileHandle = new TileHandle<TTile>(blankTile);

			defaultInstance = this;
		}
		public TileHandle<TTile> GetTile(int index) {
			return GetTile(index, 0);
		}
		public TileHandle<TTile> GetTile(int index, int priority) {
			Debug.Assert(index >= 0);
			lock(syncRoot) {
				if(tiles.ContainsKey(index)) {
					if(releasedTiles.Contains(index))
						releasedTiles.Remove(index);
					return new TileHandle<TTile>(tiles[index]);
				}
			}
			TTile tile = new TTile();
			tile.Index = index;
			tile.Release += delegate(object sender, EventArgs args) {
				TTile senderTile = (TTile)sender;
				lock(syncRoot) releasedTiles.Add(senderTile.Index);
			};
			lock(syncRoot) tiles.Add(index, tile);
			taskThread.DoTask(
				delegate() { return graphicLoader.LoadGraphic(index); },
				delegate(object args) { tile.Create((Image)args); },
				priority);
			return new TileHandle<TTile>(tile);
		}
		protected override void Release() {
			lock(syncRoot) {
				foreach(int index in releasedTiles) {
					tiles[index].Dispose();
					tiles.Remove(index);
				}
				releasedTiles.Clear();
			}
		}
		private object syncRoot = new Object();
		private List<int> releasedTiles = new List<int>();
		public int TileCount { get { return tiles.Count; } }
		private readonly TTile blankTile;
		private readonly TileHandle<TTile> blankTileHandle;
		private GraphicLoader graphicLoader;
		private static TileManager<TTile> defaultInstance;
		public static TileManager<TTile> Default { get { return defaultInstance; } }
		private Dictionary<int, TTile> tiles = new Dictionary<int, TTile>();
	}
	class ObjectTile : Tile { }
	class FloorTile : Tile { }
	class FloorTileManager : TileManager<FloorTile> {
		public FloorTileManager() : base("tile", 16) { }
	}
	class ObjectTileManager : TileManager<ObjectTile> {
		public ObjectTileManager() : base("tilec", 19) { }
	}
}