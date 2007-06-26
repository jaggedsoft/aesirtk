using System;
using System.Collections.Generic;
using System.Text;

namespace Aesir.Util {
	abstract class ScrollingBuffer<T> : IEnumerable<T> {
		public ScrollingBuffer() {
			buffer = new Buffer(this);
		}
		private Buffer buffer;
		private class Buffer : CircularBuffer<T> {
			public Buffer(ScrollingBuffer<T> scrollingBuffer) {
				this.scrollingBuffer = scrollingBuffer;
			}
			private ScrollingBuffer<T> scrollingBuffer;
			protected override T Create(int index, object state) {
				int absoluteIndex = index + scrollingBuffer.offset;
				if(absoluteIndex >= 0 && absoluteIndex < scrollingBuffer.Size)
					return scrollingBuffer.Create(absoluteIndex);
				else return default(T);
			}
			protected override void Dispose(T element) {
				scrollingBuffer.Dispose(element);
			}
		}
		protected abstract T Create(int absoluteIndex);
		protected virtual void Dispose(T element) { }
		public int BufferSize {
			get { return buffer.Count; }
			set { buffer.Rebuild(value, offset); }
		}
		public abstract int Size { get; }
		public int Offset {
			get { return offset; }
			set {
				int delta = value - offset;
				offset = value;
				if(Math.Abs(delta) > buffer.Count / 2) Rebuild();
				else buffer.Advance(delta, offset);
			}
		}
		public void Rebuild() {
			buffer.Rebuild(buffer.Count, offset);
		}
		public T this[int index] {
			get { return buffer[index]; }
		}
		private int offset;
		#region IEnumerable members
		public IEnumerator<T> GetEnumerator() { return buffer.GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
		#endregion
	}
}