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

// TODO: Quit gracefully when we can't find the tile archives in the data path
// TODO: Implement priorities for tile loading
// TODO: Make it so that tiles are not immediately unloaded
namespace Aesir {
	abstract class Tile<TDerived> : IDisposable, ICloneable {
		private object syncRoot = new Object();
		public object SyncRoot { get { return syncRoot; } }
		public event EventHandler Load;
		public event EventHandler Death;
		public virtual void OnLoad(Image image) {
			this.image = image;
			if(Load != null) Load(this, EventArgs.Empty);
		}
		public virtual void OnDeath() {
			if(Death != null) Death(this, EventArgs.Empty);
		}
		public object Clone() {
			++refcount;
			return this;
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing) {
			if(disposing) --refcount;
			if(!disposing || refcount <= 0) {
				Debug.Assert(image != null);
				image.Dispose();
				image = null;
				OnDeath();
			}
		}
		public void Draw(Graphics graphics, Point point) {
			lock(SyncRoot)
				graphics.DrawImage(image, point);
		}
		~Tile() { Dispose(false); }
		public int index;
		protected Image image = TileManagerBase.NullImage;
		private int refcount = 0;
	}
	class TileManagerBase {
		protected delegate object DoWork();
		protected delegate void RunWorkerCompleted(object args);
		private struct Task {
			public DoWork doWork;
			public RunWorkerCompleted runWorkerCompleted;
		}
		private struct TaskResult {
			public object result;
			public Task task;
		}
		protected static void DoWorkAsync(DoWork doWork,
			RunWorkerCompleted runWorkerCompleted) {
			Task task = new Task();
			task.doWork = doWork;
			task.runWorkerCompleted = runWorkerCompleted;
			pendingTasks.Add(task);
			UpdateBackgroundWorker();
		}
		private static void UpdateBackgroundWorker() {
			if(!backgroundWorker.IsBusy && pendingTasks.Count > 0) {
				backgroundWorker.RunWorkerAsync(pendingTasks[0]);
				pendingTasks.RemoveAt(0);
			}
		}
		static TileManagerBase() {
			backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
			backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
				backgroundWorker_RunWorkerCompleted);
		}
		private static void  backgroundWorker_RunWorkerCompleted(object sender,
			RunWorkerCompletedEventArgs args) {
			TaskResult taskResult = (TaskResult)args.Result;
			taskResult.task.runWorkerCompleted(taskResult.result);
			UpdateBackgroundWorker();
		}
		private static void backgroundWorker_DoWork(object sender, DoWorkEventArgs args) {
			Task task = (Task)args.Argument;
			TaskResult taskResult = new TaskResult();
			taskResult.result = task.doWork();
			taskResult.task = task;
			args.Result = taskResult;
			if(backgroundWorker.CancellationPending) args.Cancel = true;
		}
		// TODO: Use a priority queue?
		private static List<Task> pendingTasks = new List<Task>();
		private static BackgroundWorker backgroundWorker = new BackgroundWorker();
		public static readonly Image NullImage = Bitmap.FromFile(@"null.png");
		public static readonly Image BlankImage = new Bitmap(Graphic.Width, Graphic.Height);
		protected const string DataPath = @"C:\Program Files\NexusTK\data\"; // TEMP
	}
	class TileManager<TTile> : TileManagerBase where TTile : Tile<TTile>, new() {
		public TileManager(string sourceName, int sourceCount) {
			string archivePath = Path.Combine(DataPath, "tile.dat");
			string paletteCollectionName = sourceName + "." + PaletteCollection.fileExtension,
				paletteTableName = sourceName + "." + PaletteTable.fileExtension;
			PaletteCollection paletteCollection;
			PaletteTable paletteTable;
			using(FileStream archiveStream = new FileStream(archivePath, FileMode.Open)) {
				ArchiveInfo archive = new ArchiveInfo(archiveStream);
				archiveStream.Seek(archive.GetFile(paletteCollectionName).Offset, SeekOrigin.Begin);
				paletteCollection = PaletteCollection.FromStream(archiveStream);
				archiveStream.Seek(archive.GetFile(paletteTableName).Offset, SeekOrigin.Begin);
				paletteTable = PaletteTable.FromStream(archiveStream);
			}
			GraphicLoader.SourceProvider sourceProvider = new GraphicLoader.SourceProvider(
				sourceCount, Path.Combine(DataPath, sourceName));
			graphicLoader = new GraphicLoader(paletteCollection, paletteTable, sourceProvider);

			BlankTile = new TTile();
			BlankTile.OnLoad(BlankImage);
			BlankTile.index = 0;
			tiles.Add(0, BlankTile);
		}
		public TTile GetTile(int index) {
			try {
				return (TTile)tiles[index].Clone();
			} catch(KeyNotFoundException) {
				TTile tile = new TTile();
				DoWorkAsync(
					delegate() { return graphicLoader.LoadGraphic(index); },
					delegate(object args) { tile.OnLoad((Image)args); }
				);
				tile.index = index;
				tile.Death += delegate(object sender, EventArgs args) {
					TTile senderTile = (TTile)sender;
					tiles.Remove(senderTile.index);
				};
				tiles.Add(index, tile);
				return (TTile)tile.Clone();
			}
		}
		public readonly TTile BlankTile;
		private GraphicLoader graphicLoader;
		protected static TileManager<TTile> defaultInstance;
		public static TileManager<TTile> Default { get { return defaultInstance; } }
		private Dictionary<int, TTile> tiles = new Dictionary<int, TTile>();
	}
	class ObjectTile : Tile<ObjectTile> { }
	class FloorTile : Tile<FloorTile> { }
	class FloorTileManager : TileManager<FloorTile> {
		private FloorTileManager() : base("tile", 16) { }
		public static void Init() { }
		static FloorTileManager() {
			defaultInstance = new FloorTileManager();
		}
	}
	class ObjectTileManager : TileManager<ObjectTile> {
		private ObjectTileManager() : base("tilec", 19) { }
		public static void Init() { }
		static ObjectTileManager() {
			defaultInstance = new ObjectTileManager();
		}
	}
}