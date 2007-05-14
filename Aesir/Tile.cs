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
		internal void Create(Bitmap bitmap, Point point) {
			Bitmap tileBitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
			using(Graphics graphics = Graphics.FromImage(tileBitmap)) {
				graphics.DrawRectangle(Pens.Transparent, 0, 0, Width, Height);
				graphics.DrawImage(bitmap, point);
			}
			bitmap.Dispose();
			lock(SyncRoot) image = tileBitmap;
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
		private uint index;
		public uint Index {
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
		protected static readonly Bitmap BlankBitmap = new Bitmap(Tile.Width, Tile.Height);
	}
	class TileManager<TTile> : TileManagerBase where TTile : Tile, new() {
		public TileManager(string sourceTag, int sourceCount) {
			string dataPath = Settings.Default.DataPath;
			PaletteTable paletteTable;
			{
				string mainDatPath = Path.Combine(dataPath, "tile.dat");
				using(DatReader mainDatReader = new DatReader(mainDatPath))
					paletteTable = new PaletteTable(mainDatReader, sourceTag);
			}
			GraphicLoader.ISourceProvider sourceProvider =
				new GraphicLoader.SimpleSourceProvider(sourceCount, Path.Combine(dataPath, sourceTag));
			graphicLoader = new GraphicLoader(paletteTable, sourceProvider);

			blankTile = new TTile();
			blankTile.Create((Bitmap)BlankBitmap.Clone(), new Point(0, 0));
			blankTile.Index = 0;
			tiles.Add(0, blankTile);
		}
		private class TaskResult {
			public Bitmap bitmap;
			public EpfGraphicInfo graphicInfo;
		}
		public TileHandle<TTile> GetTile(uint index) {
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
				delegate() {
					TaskResult result = new TaskResult();
					result.bitmap = graphicLoader.LoadGraphic(index, out result.graphicInfo);
					return result;
				},
				delegate(object args) {
					TaskResult result = (TaskResult)args;
					tile.Create(result.bitmap, result.graphicInfo.Point);
				}
			);
			return new TileHandle<TTile>(tile);
		}
		private readonly TTile blankTile;
		private GraphicLoader graphicLoader;
		protected static TileManager<TTile> defaultInstance;
		public static TileManager<TTile> Default { get { return defaultInstance; } }
		private Dictionary<uint, TTile> tiles = new Dictionary<uint, TTile>();
	}
	class ObjectTile : Tile { }
	class FloorTile : Tile { }
	class FloorTileManager : TileManager<FloorTile> {
		private FloorTileManager() : base("tile", 16) { }
		public static void Init() { defaultInstance = new FloorTileManager();  }
	}
	class ObjectTileManager : TileManager<ObjectTile> {
		private ObjectTileManager() : base("tilec", 19) { }
		public static void Init() { defaultInstance = new ObjectTileManager();  }
	}
}