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

// TODO: The priorities aren't working correctly, maybe PromoteTask is bugged?
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
		
		public override bool Equals(object obj) {
			return tile.Index == ((TileHandle<TTile>)obj).tile.Index;
		}
		public override int GetHashCode() {
			return tile.Index.GetHashCode();
		}
		public static implicit operator TTile(TileHandle<TTile> handle) {
			if(handle == null) throw new InvalidCastException("Cannot cast a null TileHandle.");
			else return handle.tile;
		}
		public static implicit operator Tile(TileHandle<TTile> handle) {
			if(handle == null) throw new InvalidCastException("Cannot cast a null TileHandle.");
			else return handle.tile;
		}
		#region Wrappers for Tile methods
		public int Index {
			get { return tile.Index; }
		}
		public Image Image {
			get { return tile.Image; }
		}
		public event EventHandler Load {
			add { tile.Load += value; }
			remove { tile.Load -= value; }
		}
		public event EventHandler Release {
			add { tile.Release += value; }
			remove { tile.Release -= value; }
		}
		public object SyncRoot {
			get { return tile.SyncRoot; }
		}
		#endregion
		private bool disposed = false;
		private TTile tile;
	}
	abstract class Tile : IDisposable {
		private object syncRoot = new Object();
		public object SyncRoot {
			get { return syncRoot; }
		}
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
			lock(syncRoot) this.image = image;
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
		public Image Image {
			get {
				if(disposed) throw new ObjectDisposedException("Tile");
				return image;
			}
		}
		private int index;
		public int Index {
			get { return index; }
			internal set { index = value; }
		}
		protected Image image = nullImage; // TODO: Did this cause a bug before?
		public static void LoadNullImage() {
			nullImage = new Bitmap("null.png");
		}
		private static Image nullImage;
		private int refcount = 0;
		internal int Refcount {
			get { return refcount; }
			set {
				refcount = value;
				if(refcount <= 0) OnRelease();
			}
		}
		public override string ToString() { return "{Index:" + index + "}"; }
		public static Size Size {
			get { return new Size(Width, Height); }
		}
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
		private static Settings.TileManager settings = Settings.TileManager.Default;
		protected static readonly Image BlankImage = new Bitmap(Tile.Width, Tile.Height);
	}
	class TileManagerException : Exception { } // TODO: use this?
	class TileManager<TTile> : TileManagerBase where TTile : Tile, new() {
		public TileManager(string sourceTag, int sourceCount) {
			string dataPath = Settings.Global.Default.DataPath;
			string archivePath = Path.Combine(dataPath, "tile.dat");
			string paletteCollectionName = sourceTag + "." + PaletteCollection.FileExtension,
				paletteTableName = sourceTag + "." + PaletteTable.FileExtension;
			PaletteCollection paletteCollection;
			PaletteTable paletteTable;
			using(FileStream archiveStream = new FileStream(archivePath, FileMode.Open)) {
				ArchiveHeader archive = new ArchiveHeader(archiveStream);
				archiveStream.Seek(archive.GetEntry(paletteCollectionName).Offset, SeekOrigin.Begin);
				paletteCollection = PaletteCollection.FromStream(archiveStream);
				archiveStream.Seek(archive.GetEntry(paletteTableName).Offset, SeekOrigin.Begin);
				paletteTable = PaletteTable.FromStream(archiveStream);
			}
			GraphicLoader.ISourceProvider sourceProvider =
				new GraphicLoader.SourceProvider(sourceCount, Path.Combine(dataPath, sourceTag));
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
				{
					TTile tile;
					if(tiles.TryGetValue(index, out tile)) {
						if(releasedTiles.Contains(index))
							releasedTiles.Remove(index);
						return new TileHandle<TTile>(tile);
					}
				}
				{
					LoadingTile loadingTile;
					if(loadingTiles.TryGetValue(index, out loadingTile)) {
						taskThread.PromoteTask(loadingTile.task, priority);
						return new TileHandle<TTile>(loadingTile.tile);
					}
				}
			}
			TTile pendingTile = new TTile();
			pendingTile.Index = index;
			EventHandler tile_Release = new EventHandler(delegate(object sender, EventArgs args) {
				TTile senderTile = (TTile)sender;
				lock(syncRoot) loadingTiles.Remove(senderTile.Index);
			});
			EventHandler tile_Load = new EventHandler(delegate(object sender, EventArgs args) {
				TTile senderTile = (TTile)sender;
				lock(syncRoot) {
					loadingTiles.Remove(senderTile.Index);
					tiles.Add(senderTile.Index, senderTile);
				}
				senderTile.Release -= tile_Release;
				senderTile.Release += delegate(object innerSender, EventArgs innerArgs) {
					lock(syncRoot) releasedTiles.Add(((TTile)innerSender).Index);
				};
			});
			TaskThread.Task task = taskThread.AddTask(
				delegate() { return graphicLoader.LoadGraphic(index); },
				delegate(object args) { pendingTile.Create((Image)args); },
				priority);
			LoadingTile pendingLoadingTile = new LoadingTile(task, pendingTile);
			lock(syncRoot) loadingTiles.Add(pendingTile.Index, pendingLoadingTile);
			return new TileHandle<TTile>(pendingTile);
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
		public int TileCount {
			get { return tiles.Count; }
		}
		private readonly TTile blankTile;
		private readonly TileHandle<TTile> blankTileHandle;
		private GraphicLoader graphicLoader;
		private static TileManager<TTile> defaultInstance;
		public static TileManager<TTile> Default {
			get { return defaultInstance; }
		}
		private class LoadingTile {
			public TaskThread.Task task;
			public TTile tile;
			public LoadingTile(TaskThread.Task task, TTile tile) {
				this.task = task;
				this.tile = tile;
			}
		}
		private Dictionary<int, LoadingTile> loadingTiles = new Dictionary<int, LoadingTile>();
		private List<int> releasedTiles = new List<int>();
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