using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using Aesir.Nexus;
using Aesir.Util;
using System.Configuration;
using C5;

namespace Aesir {
	enum TileType { FloorTile, ObjectTile };
	/// <summary>
	///		A <c>TileHandle</c> is used to refer to a <c>Tile</c> that was retrieved from a
	///		<c>TileManager</c>. Although you can implicitly cast to a <c>Tile</c> to get the
	///		underlying <c>Tile</c>, it is more convenient to use the method wrappers provided
	///		by the <c>TileHandle</c> class.
	/// </summary>
	// This class is part of a reference counted tile management mechanism. The refcount for the
	// underlying tile is incremented in the constructor, and decremented in the Dispose method.
	[DebuggerDisplay("Index = {Index}, TileType = {TileType}")]
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
		#region Wrappers for Tile members
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
		private Tile tile;
	}
	[DebuggerDisplay("Index = {Index}, TileType = {TileType}")]
	class Tile : IDisposable {
		public event EventHandler Release;
		public event EventHandler Load;
		internal void Create(Image image) {
			lock(SyncRoot) this.image = image;
			loaded = true;
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
		public Tile(TileType tileType, int index) {
			this.tileType = tileType;
			this.index = index;
		}
		#region IDisposable implementation
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing) {
			if(!disposed) {
				disposed = true;
				if(image != null) {
					image.Dispose();
					image = null;
				}
			}
		}
		private bool disposed = false;
		~Tile() { Dispose(false); }
		#endregion
		#region Static members
		public static Size Size {
			get { return new Size(Width, Height); }
		}
		public const int Width = 48, Height = 48;
		public static void Draw(Tile tile, Graphics graphics, Point point) {
			lock(tile.SyncRoot) {
				if(tile.Image != null) graphics.DrawImage(tile.Image, point);
				else graphics.FillRectangle(Brushes.Black, new Rectangle(point, Size));
			}
		}
		public static void DrawInverted(Tile tile, Graphics graphics, Point point) {
			lock(tile.SyncRoot) {
				if(tile.Image != null) GraphicsUtil.DrawImageInverted(graphics, tile.Image, point);
				else graphics.FillRectangle(Brushes.Black, new Rectangle(point, Size));
			}
		}
		#endregion
		public readonly object SyncRoot = new Object();
		private int index;
		protected Image image = null;
		private int refcount = 0;
		private TileType tileType;
		private bool loaded = false;
		public Image Image {
			get {
				if(disposed) throw new ObjectDisposedException("Tile");
				return image;
			}
		}
		public bool Loaded {
			get { return loaded; }
		}
		public int Index {
			get { return index; }
		}
		internal int Refcount {
			get { return refcount; }
			set {
				refcount = value;
				if(refcount <= 0) {
					// Do NOT dispose the object when the refcount reaches 0. Disposal is handled
					// in TileMaanger.ReleaseTiles. Raise the Release event, which will add this
					// tile to the TileManager.releasedTiles collection and schedule it for disposal.
					OnRelease(EventArgs.Empty);
				}
			}
		}
		public TileType TileType {
			get { return tileType; }
		}
	}
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
		// This timer is used to raise the ReleaseTimerTick event, which periodically releases tiles
		// that are no longer referenced. Tiles are not immediately released because there are cases
		// where the program releases a TileHandle, only to request it again a millisecond later --
		// for example, when the TileBrowser is resized.
		private static System.Threading.Timer releaseTimer;
		// The TaskThread is used to load tiles in the background. It is shared by all TileManager
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
			ReleaseTimerTick += delegate(object sender, EventArgs args) { ReleaseTiles(); };
			this.tileType = tileType;

			// TODO: Clean up the API for this shit?
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

			BlankTile = new Tile(tileType, 0);
			// Use a clone because the Create method transfers ownership
			BlankTile.Create((Image)BlankImage.Clone());
			tiles.Add(0, BlankTile);
			// Artificially increment the refcount for blankTile so it is never disposed until it
			// is garbage collected.
			++BlankTile.Refcount;
		}
		public int TileCount {
			get { return graphicLoader.GraphicCount; }
		}
		public TileHandle GetTile(int index, int priority) {
			if(index < 0) {
				string message = "The specified index cannot be less than 0.";
				throw new ArgumentException(message, "index");
			}
			LoadingTile loadingTile = null;
			// Check to see if the tile is already loaded or is currently loading
			lock(SyncRoot) {
				if(tiles.Contains(index)) {
					if(releasedTiles.Contains(index))
						releasedTiles.Remove(index);
					return new TileHandle(tiles[index]);
				} else if(loadingTiles.Contains(index)) {
					loadingTile = loadingTiles[index];
					taskThread.PromoteTask(loadingTile.taskHandle, priority);
					return new TileHandle(loadingTile.tile);
				}
			}
			// Load the tile
			Tile tile = new Tile(tileType, index);
			EventHandler tile_Release = delegate(object sender, EventArgs args) {
				Tile senderTile = (Tile)sender;
				if(senderTile.Loaded) {
					// Add the tile to the disposal queue
					lock(SyncRoot) releasedTiles.Add(senderTile.Index);
				} else {
					// The tile was released prematurely
					lock(SyncRoot) {
						try {
							LoadingTile senderLoadingTile = loadingTiles[senderTile.Index];
							taskThread.CancelTask(senderLoadingTile.taskHandle);
							loadingTiles.Remove(senderTile.Index);
						} catch(NoSuchItemException) {
							Debug.Fail("Prematurely released tile could not be found in the " +
								"loadingTiles collection.");
						}
					}
				}
			};
			EventHandler tile_Load = delegate(object sender, EventArgs args) {
				Tile senderTile = (Tile)sender;
				lock(SyncRoot) {
					loadingTiles.Remove(senderTile.Index);
					if(tiles.Contains(senderTile.Index)) {
						if(tiles[senderTile.Index].Refcount != 0) {
							Debug.Fail("A tile has been loaded twice, but the second copy cannot be" +
								" disposed because it is in use.");
						} else tiles[senderTile.Index].Dispose();
					}
					tiles[senderTile.Index] = senderTile;
				}
			};
			tile.Release += tile_Release;
			tile.Load += tile_Load;
			ITaskHandle taskHandle = taskThread.AddTask(
				delegate() { return graphicLoader.LoadGraphic(index); },
				delegate(object arg) { tile.Create((Image)arg); },
				priority);
			loadingTile = new LoadingTile(taskHandle, tile);
			lock(SyncRoot) loadingTiles.Add(tile.Index, loadingTile);
			return new TileHandle(tile);
		}
		private void ReleaseTiles() {
			lock(SyncRoot) {
				foreach(int index in releasedTiles) {
					tiles[index].Dispose();
					tiles.Remove(index);
				}
				releasedTiles.Clear();
			}
		}
		// Used to synchronize access to the various collections associated with the TileManager
		// instance, including releasedTiles, loadingTiles, and tiles.
		private readonly object SyncRoot = new Object();
		public int LoadedTileCount {
			get { return tiles.Count; }
		}
		public TileType TileType {
			get { return tileType; }
		}
		private readonly Tile BlankTile;
		private GraphicLoader graphicLoader;
		private class LoadingTile {
			public ITaskHandle taskHandle;
			public Tile tile;
			public LoadingTile(ITaskHandle taskHandle, Tile tile) {
				this.taskHandle = taskHandle;
				this.tile = tile;
			}
		}
		private IList<int> releasedTiles = new ArrayList<int>();
		private IDictionary<int, LoadingTile> loadingTiles = new HashDictionary<int, LoadingTile>();
		private IDictionary<int, Tile> tiles = new HashDictionary<int, Tile>();
	}
}