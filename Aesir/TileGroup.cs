using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Aesir {
	class TileCell : IDisposable {
		private TileHandle<FloorTile> floorTile;
		private TileHandle<ObjectTile> objectTile;
		public TileHandle<FloorTile> FloorTile {
			set { floorTile = value; }
			get { return floorTile; }
		}
		public TileHandle<ObjectTile> ObjectTile {
			get { return objectTile; }
			set { objectTile = value; }
		}
		public TTile GetTile<TTile>() where TTile : Tile {
			if(typeof(TTile) == typeof(FloorTile)) return (TTile)FloorTile;
			else if(typeof(TTile) == typeof(ObjectTile)) return (TTile)ObjectTile;
			Debug.Fail("Called TileCell.GetHandle with an invalid type parameter");
			return null;
		}
		public void Dispose() { /* TODO */ }
		public TileCell() {
			floorTile = null;
			objectTile = null;
		}
		public TileCell(TileHandle<FloorTile> floorTile, TileHandle<ObjectTile> objectTile) {
			this.floorTile = floorTile;
			this.objectTile = objectTile;
		}
	}
	class TileGroup : IDisposable {
		public bool Contains<TTile>(TileHandle<TTile> target) where TTile : Tile {
			foreach(TileCell cell in buffer) {
				TTile tile = cell.GetTile<TTile>();
				if(tile != null && tile.Index == ((TTile)target).Index) return true;
			}
			return false;
		}
		public TileGroup(int width, int height) {
			buffer = new TileCell[width, height];
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