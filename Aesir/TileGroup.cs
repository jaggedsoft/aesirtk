using System;
using System.Collections.Generic;
using System.Text;

namespace Aesir {
	class TileGroup<TTile> : IDisposable where TTile : Tile {
		public bool Contains(TileHandle<TTile> targetHandle) {
			foreach(TileHandle<TTile> handle in buffer) {
				if(targetHandle == handle) return true;
			}
			return false;
		}
		public TileGroup(TileHandle<TTile>[,] buffer) {
			this.buffer = new TileHandle<TTile>[buffer.GetLength(0), buffer.GetLength(1)];
			for(int y = 0; y < buffer.GetLength(1); ++y) {
				for(int x = 0; x < buffer.GetLength(0); ++x)
					this.buffer[x, y] = (TileHandle<TTile>)buffer[x, y].Clone();
			}
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		~TileGroup() { Dispose(false); }
		protected virtual void Dispose(bool disposing) {
			foreach(TileHandle<TTile> handle in buffer)
				handle.Dispose();
		}
		public TileHandle<TTile> this[int x, int y] {
			get { return buffer[x, y]; }
			set { buffer[x, y] = value; }
		}
		private TileHandle<TTile>[,] buffer;
	}
}