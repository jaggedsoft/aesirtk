using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Aesir {
	class TileCell : IDisposable {
		private TileHandle floorTile;
		private TileHandle objectTile;
		public TileHandle FloorTile {
			set {
				Debug.Assert(value.TileType == TileType.FloorTile);
				floorTile = value;
			}
			get { return floorTile; }
		}
		public TileHandle ObjectTile {
			set {
				Debug.Assert(value.TileType == TileType.ObjectTile);
				objectTile = value;
			}
			get { return objectTile; }
		}
		public void Dispose() {
			if(floorTile != null) floorTile.Dispose();
			if(objectTile != null) objectTile.Dispose();
		}
		public TileCell() {
			floorTile = null;
			objectTile = null;
		}
		public TileCell(TileHandle floorTile, TileHandle objectTile) {
			Debug.Assert(floorTile.TileType == TileType.FloorTile);
			Debug.Assert(objectTile.TileType == TileType.ObjectTile);
			this.floorTile = floorTile;
			this.objectTile = objectTile;
		}
	}
	class TileGroup : IDisposable {
		public TileGroup(int width, int height) {
			buffer = new TileCell[width, height];
			for(int y = 0; y < height; ++y) {
				for(int x = 0; x < width; ++x)
					buffer[x, y] = new TileCell();
			}
		}
		public int Width {
			get { return buffer.GetLength(0); }
		}
		public int Height {
			get { return buffer.GetLength(1); }
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		~TileGroup() { Dispose(false); }
		protected virtual void Dispose(bool disposing) {
			foreach(TileCell cell in buffer)
				cell.Dispose();
		}
		public TileCell this[int x, int y] {
			get { return buffer[x, y]; }
		}
		private TileCell[,] buffer;
	}
}