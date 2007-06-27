using System;
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
using System.Reflection;
using C5;

namespace Aesir {
	enum TileType { FloorTile, ObjectTile };
	static class LogUtil {
		public static void Log(string message) {
			Trace.WriteLine(string.Format("{{Thread {0}", Thread.CurrentThread.ManagedThreadId) + "} : " + message);
		}
	}
	/// <summary>
	///		A <c>TileHandle</c> is used to refer to a <c>Tile</c> that was retrieved from a
	///		<c>TileManager</c>. Although you can implicitly cast to a <c>Tile</c> to get the
	///		underlying <c>Tile</c>, it is more convenient to use the method wrappers provided
	///		by the <c>TileHandle</c> class.
	/// </summary>
	// This class is part of a reference counted tile management mechanism. The refcount for the
	// underlying tile is incremented in the constructor, and decremented in the Dispose method.
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
	[DebuggerDisplay("Index = {Index}")]
	class Tile : IDisposable {
		public event EventHandler RealRelease;
		public event EventHandler Release {
			add {
				LogUtil.Log(string.Format("{0} -> Added \"Release\" handler", this));
				RealRelease += value;
			}
			remove {
				LogUtil.Log(string.Format("{0} -> Removed \"Release\" handler", this));
				RealRelease -= value;
			}
		}
		public event EventHandler Load;
		internal void Create(Image image) {
			LogUtil.Log(string.Format("{0} -> Create()", this));
			lock(syncRoot) this.image = image;
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
			LogUtil.Log(string.Format("{0} -> OnRelease()", this));
			if(RealRelease != null) RealRelease(this, args);
		}
		public Tile(TileType tileType) {
			this.tileType = tileType;
			id = nextId++;
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
		private bool disposed = false, loaded = false;
		~Tile() { Dispose(false); }
		#endregion
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
		public bool Loaded {
			get { return loaded; }
		}
		public int Index {
			get { return index; }
			internal set { index = value; }
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
		private int id;
		private static int nextId = 0;
		public override string ToString() {
			return string.Format("Tile(Index = {0}, ID = {1})", index, id);
		}
	}
	class TileManagerException : Exception { } // TODO: Use this TileManagerException? Or not?
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
			loadingTiles.ItemsAdded += delegate(object sender, ItemCountEventArgs<KeyValuePair<int, LoadingTile>> args) {
				LogUtil.Log(string.Format("{0} -> Added to \"loadingTiles\"", args.Item.Value.tile));
			};
			loadingTiles.ItemsRemoved += delegate(object sender, ItemCountEventArgs<KeyValuePair<int, LoadingTile>> args) {
				LogUtil.Log(string.Format("{0} -> Removed from \"loadingTiles\"", args.Item.Value.tile));
			};
			releasedTiles.ItemsAdded += delegate(object sender, ItemCountEventArgs<int> args) {
				LogUtil.Log(string.Format("Tile(Index = {0}) -> Added to \"releasedTiles\"", args.Item));
			};
			releasedTiles.ItemsRemoved += delegate(object sender, ItemCountEventArgs<int> args) {
				LogUtil.Log(string.Format("Tile(Index = {0}) -> Removed from \"releasedTiles\"", args.Item));
			};
			tiles.ItemsAdded += delegate(object sender, ItemCountEventArgs<KeyValuePair<int, Tile>> args) {
				LogUtil.Log(string.Format("{0} -> Added to \"tiles\"", args.Item.Value));
			};
			tiles.ItemsRemoved += delegate(object sender, ItemCountEventArgs<KeyValuePair<int, Tile>> args) {
				LogUtil.Log(string.Format("{0} -> Removed from \"tiles\"", args.Item.Value));
			};

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

			blankTile = new Tile(tileType);
			// Use a clone because the Create method transfers ownership
			blankTile.Create((Image)BlankImage.Clone());
			blankTile.Index = 0;
			tiles.Add(0, blankTile);
			// Artificially increment the refcount for blankTile so it is never disposed until it
			// is garbage collected.
			++blankTile.Refcount;
		}
		public int TileCount {
			get { return graphicLoader.GraphicCount; }
		}
		public TileHandle GetTile(int index, int priority) {
			if(index < 0) {
				string message = "The specified index cannot be less than 0.";
				throw new ArgumentException(message, "index");
			}
			LogUtil.Log(string.Format("Tile(Index = {0}) -> GetTile()", index));
			LoadingTile loadingTile = null;
			// Check to see if the tile is already loaded or is currently loading
			lock(syncRoot) {
				if(tiles.Contains(index)) {
					LogUtil.Log(string.Format("Tile(Index = {0}) -> \tUsing previously loaded tile", index));
					if(releasedTiles.Contains(index)) {
						LogUtil.Log(string.Format("Tile(Index = {0}) -> \t\tRemoving from disposal queue", index));
						releasedTiles.Remove(index);
					}
					return new TileHandle(tiles[index]);
				} else if(loadingTiles.Contains(index)) {
					loadingTile = loadingTiles[index];
					LogUtil.Log(string.Format("Tile(Index = {0}) -> \tUsing loading tile", index));
					taskThread.PromoteTask(loadingTile.taskHandle, priority);
					return new TileHandle(loadingTile.tile);
				}
			}
			// The tile is not loaded, load it!
			Tile tile = new Tile(tileType);
			tile.Index = index;
			// The default release handler, tile_Release_premature, removes the tile from the list
			// of tiles that are in the process of loading (loadingTiles). This handler will be
			// invoked in a case where a tile is disposed before it is fully loaded. When the tile
			// is fully loaded, tile_Release_premature is replaced with tile_Release_full.
			EventHandler tile_Release_premature = delegate(object sender, EventArgs args) {
				Tile senderTile = (Tile)sender;
				Debug.Assert(!senderTile.Loaded, "If the tile has been loaded, this EventHandler" +
					" should have been uninstalled.");
				LogUtil.Log(string.Format("{0} -> Prematurely released", senderTile));
				lock(syncRoot) {
					if(loadingTiles.Contains(senderTile.Index)) {
						LoadingTile senderLoadingTile = loadingTiles[senderTile.Index];
						taskThread.CancelTask(senderLoadingTile.taskHandle);
						loadingTiles.Remove(senderTile.Index);
					} else {
						//Debug.Fail("What happen?");
						// TEMP: Debug.Fail("What happen?")
					}
				}
			};
			EventHandler tile_Release_full = delegate(object sender, EventArgs args) {
				Tile senderTile = (Tile)sender;
				Debug.Assert(senderTile.Loaded, "This EventHandler should only have been installed" +
					" after the tile has been loaded.");
				LogUtil.Log(string.Format("{0} -> Fully released", senderTile));
				lock(syncRoot) releasedTiles.Add(senderTile.Index);
			};
			EventHandler tile_Release = delegate(object sender, EventArgs args) {
				Tile senderTile = (Tile)sender;
				LogUtil.Log(string.Format("{0} -> [tile_Release] Loaded = {1}", senderTile, senderTile.Loaded));
				if(senderTile.Loaded) {
					LogUtil.Log(string.Format("{0} -> [tile_Release] Fully released", senderTile));
					lock(syncRoot) releasedTiles.Add(senderTile.Index);
				} else {
					LogUtil.Log(string.Format("{0} -> [tile_Release] Prematurely released", senderTile));
					lock(syncRoot) {
						if(loadingTiles.Contains(senderTile.Index)) {
							LoadingTile senderLoadingTile = loadingTiles[senderTile.Index];
							taskThread.CancelTask(senderLoadingTile.taskHandle);
							loadingTiles.Remove(senderTile.Index);
						} else {
							//Debug.Fail("What happen?");
							// TEMP: Debug.Fail("What happen?")
						}
					}
				}
			};
			EventHandler tile_Load = delegate(object sender, EventArgs args) {
				Tile senderTile = (Tile)sender;
				LogUtil.Log(string.Format("{0} -> [tile_Load] Loaded", senderTile));
				LogUtil.Log(string.Format("{0} -> [tile_Load] Loaded = {1}", senderTile, senderTile.Loaded));
				lock(syncRoot) {
					LogUtil.Log(string.Format("{0} -> [tile_Load] Removing from \"loadingTiles\"", senderTile));
					loadingTiles.Remove(senderTile.Index);
					LogUtil.Log(string.Format("{0} -> [tile_Load] Checking \"tiles\" for membership", senderTile));
					if(tiles.Contains(senderTile.Index)) {
						LogUtil.Log(string.Format("{0} -> [tile_Load] \"senderTile\" is in \"tiles\"!", senderTile));
						if(tiles[senderTile.Index].Refcount != 0) {
							Debug.Fail(""); // TODO: THIS IS A BIG BUG SHIT
						} else {
							Debug.Fail("LOLWHAT?");
							tiles[senderTile.Index].Dispose();
						}
					}
					LogUtil.Log(string.Format("{0} -> [tile_Load] Adding to \"tiles\"", senderTile));
					tiles[senderTile.Index] = senderTile;
				}
				//Console.WriteLine(((EventInfo)senderTile.GetType().GetMember("RealRelease")[0]));
				/*senderTile.Release -= tile_Release_premature;
				senderTile.Release += tile_Release_full;*/
			};
			//tile.Release += tile_Release_premature;
			tile.Release += tile_Release;
			tile.Load += tile_Load;
			ITaskHandle taskHandle = taskThread.AddTask(
				delegate() { return graphicLoader.LoadGraphic(index); },
				delegate(object arg) { tile.Create((Image)arg); },
				priority);
			loadingTile = new LoadingTile(taskHandle, tile);
			lock(syncRoot) loadingTiles.Add(tile.Index, loadingTile);
			return new TileHandle(tile);
		}
		private void ReleaseTiles() {
			lock(syncRoot) {
				foreach(int index in releasedTiles) {
					LogUtil.Log(string.Format("{0} -> [ReleaseTiles] Disposing", tiles[index]));
					tiles[index].Dispose();
					tiles.Remove(index);
				}
				releasedTiles.Clear();
			}
		}
		// Used to synchronize access to the various collections associated with the TileManager
		// instance, including releasedTiles, loadingTiles, and tiles.
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