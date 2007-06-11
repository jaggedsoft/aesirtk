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
	enum TileType { FloorTile, ObjectTile };
	/// <summary>
	///		A <c>TileHandle</c> is used to refer to a <c>Tile</c> that was retrieved from a
	///		<c>TileManager</c>.
	/// </summary>
	// This class is part of a reference counted tile management mechanism. In the constructor,
	// this class increments the reference count of the referenced tile. In the Dispose method,
	// this class decrements the reference count.
	class TileHandle : IDisposable, ICloneable {
		#region Refcounting and IDisposable implementation
		internal TileHandle(Tile tile) {
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
		private bool disposed = false;
		#endregion
		public object Clone() {
			return new TileHandle(tile);
		}
		public override bool Equals(object obj) {
			return Index == ((TileHandle)obj).Index;
		}
		public override int GetHashCode() {
			return tile.Index.GetHashCode();
		}
		public static implicit operator Tile(TileHandle handle) {
			if(handle == null) throw new InvalidCastException("Cannot cast a null TileHandle.");
			else return handle.tile;
		}
		#region Wrappers for Tile members
		public void Draw(Graphics graphics, Point point) {
			tile.Draw(graphics, point);
		}
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
		public object SyncRoot {
			get { return tile.SyncRoot; }
		}
		public TileType TileType {
			get { return tile.TileType; }
		}
		#endregion
		private Tile tile;
	}
	class Tile : IDisposable {
		public event EventHandler Load;
		public event EventHandler Release;
		internal void Create(Image image) {
			lock(syncRoot) this.image = image;
			OnLoad(EventArgs.Empty);
		}
		protected virtual void OnLoad(EventArgs args) {
			if(Load != null) {
				Load(this, args);
				Load = null;
			}
		}
		internal virtual void OnRelease(EventArgs args) {
			if(Release != null) Release(this, args);
		}
		public Tile(TileType tileType) {
			this.tileType = tileType;
		}
		#region IDisposable implementation
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
		#endregion
		public static Size Size {
			get { return new Size(Width, Height); }
		}
		public const int Width = 48, Height = 48;
		public void Draw(Graphics graphics, Point point) {
			lock(syncRoot) {
				if(image != null) {
					graphics.DrawImage(image, point);
					return;
				}
			}
			graphics.FillRectangle(Brushes.Black, new Rectangle(point, Size));
		}
		private readonly object syncRoot = new Object();
		public object SyncRoot {
			get { return syncRoot; }
		}
		private int index;
		protected Image image = null;
		private int refcount = 0;
		private TileType tileType;
		public Image Image {
			get {
				if(disposed) throw new ObjectDisposedException("Tile");
				return image;
			}
		}
		public int Index {
			get { return index; }
			internal set { index = value; }
		}
		internal int Refcount {
			get { return refcount; }
			set {
				refcount = value;
				if(refcount <= 0) OnRelease(EventArgs.Empty);
			}
		}
		public TileType TileType {
			get { return tileType; }
		}
		public override string ToString() { return "{Index:" + index + "}"; }
	}
	class TileManagerException : Exception { } // TODO: Use this?
	interface ITileProvider {
		TileHandle GetTile(int index, int priority);
		TileType TileType { get; }
		int TileCount { get; }
	}
	class TileManager : ITileProvider {
		#region Static members
		static TileManager() {
			releaseTimer = new System.Threading.Timer(delegate(object state) {
				if(ReleaseTimerTick != null) ReleaseTimerTick(releaseTimer, EventArgs.Empty);
			}, null, 0, ReleasePeriod);
		}
		private static event EventHandler ReleaseTimerTick;
		// This timer is used to delay the releasing of tiles
		private static System.Threading.Timer releaseTimer;
		// The taskThread is used to load tiles in the background. It is shared by all TileManager
		// instances.
		protected static TaskThread taskThread = new TaskThread();
		public static int ReleasePeriod {
			get { return settings.ReleasePeriod; }
		}
		private static Settings.TileManager settings = Settings.TileManager.Default;
		protected static readonly Image BlankImage = new Bitmap(Tile.Width, Tile.Height);
		#endregion
		private TileType tileType;
		public TileManager(TileType tileType, string sourceTag, int sourceCount) {
			ReleaseTimerTick += delegate(object sender, EventArgs args) { Release(); };
			this.tileType = tileType;

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

			blankTile = new Tile(tileType);
			// Use a clone because the Create method transfers ownership
			blankTile.Create((Image)BlankImage.Clone());
			blankTile.Index = 0;
			tiles.Add(0, blankTile);
			// Artificially increment the refcount for blankTile so it is never released
			++blankTile.Refcount;
		}
		public int TileCount {
			get { return graphicLoader.GraphicCount; }
		}
		public TileHandle GetTile(int index, int priority) {
			Debug.Assert(index >= 0);
			Tile tile = null;
			LoadingTile loadingTile = null;
			// Check to see if the tile is already loaded or is currently loading
			lock(syncRoot) {
				if(tiles.TryGetValue(index, out tile)) {
					if(releasedTiles.Contains(index))
						releasedTiles.Remove(index);
					return new TileHandle(tile);
				}
				if(loadingTiles.TryGetValue(index, out loadingTile)) {
					taskThread.PromoteTask(loadingTile.taskHandle, priority);
					return new TileHandle(loadingTile.tile);
				}
			}
			// The tile is not loaded; load it!
			tile = new Tile(tileType);
			tile.Index = index;
			// The default release handler, tile_PrematureRelease, removes the tile from the list
			// of tiles that are in the process of loading (loadingTiles). This handler will be
			// invoked in a case where a tile is disposed before it is fully loaded. When the tile
			// is fully loaded, tile_PrematureRelease is replaced with tile_FullRelease.
			EventHandler tile_PrematureRelease = delegate(object sender, EventArgs args) {
				Tile senderTile = (Tile)sender;
				lock(syncRoot) {
					taskThread.CancelTask(loadingTiles[senderTile.Index].taskHandle);
					loadingTiles.Remove(senderTile.Index);
				}
			};
			EventHandler tile_FullRelease = delegate(object sender, EventArgs args) {
				Tile senderTile = (Tile)sender;
				lock(syncRoot) releasedTiles.Add(senderTile.Index);
			};
			EventHandler tile_Load = delegate(object sender, EventArgs args) {
				Tile senderTile = (Tile)sender;
				lock(syncRoot) {
					loadingTiles.Remove(senderTile.Index);
					if(tiles.ContainsKey(senderTile.Index)) {
						if(tiles[senderTile.Index].Refcount != 0) {
							Debug.Fail(""); // TODO: Document?
						} else tiles[senderTile.Index].Dispose();
					}
					tiles[senderTile.Index] = senderTile;
				}
				senderTile.Release -= tile_PrematureRelease;
				senderTile.Release += tile_FullRelease;
			};
			tile.Release += tile_PrematureRelease;
			tile.Load += tile_Load;
			TaskThread.TaskHandle taskHandle = taskThread.AddTask(
				delegate() { return graphicLoader.LoadGraphic(index); },
				delegate(object args) { tile.Create((Image)args); },
				priority);
			loadingTile = new LoadingTile(taskHandle, tile);
			lock(syncRoot) loadingTiles.Add(tile.Index, loadingTile);
			return new TileHandle(tile);
		}
		private void Release() {
			lock(syncRoot) {
				foreach(int index in releasedTiles) {
					tiles[index].Dispose();
					tiles.Remove(index);
				}
				releasedTiles.Clear();
			}
		}
		private readonly object syncRoot = new Object();
		public int LoadedTileCount {
			get { return tiles.Count; }
		}
		public TileType TileType {
			get { return tileType; }
		}
		private readonly Tile blankTile;
		private GraphicLoader graphicLoader;
		private class LoadingTile {
			public TaskThread.TaskHandle taskHandle;
			public Tile tile;
			public LoadingTile(TaskThread.TaskHandle taskHandle, Tile tile) {
				this.taskHandle = taskHandle;
				this.tile = tile;
			}
		}
		private List<int> releasedTiles = new List<int>();
		private Dictionary<int, LoadingTile> loadingTiles = new Dictionary<int, LoadingTile>();
		private Dictionary<int, Tile> tiles = new Dictionary<int, Tile>();
	}
}