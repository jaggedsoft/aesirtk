using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Aesir.Util {
	abstract class CircularBuffer<T> : IEnumerable<T> {
		private int Clamp(int value) {
			while(value < 0) value += buffer.Count;
			return (value % buffer.Count);
		}
		public void Advance(int amount, object state) {
			int amountDirty = Math.Abs(amount) % buffer.Count;
			for(int index = 0; index < amountDirty; ++index) {
				int bufferIndex = 0;
				T element = default(T);
				if(amount > 0) {
					element = Create(buffer.Count - amountDirty + index, state);
					bufferIndex = Clamp(headIndex + index);
				} else if(amount < 0) {
					element = Create(amountDirty - index - 1, state);
					bufferIndex = Clamp(headIndex - index - 1);
				}
				Dispose(buffer[bufferIndex]);
				buffer[bufferIndex] = element;
			}
			headIndex = Clamp(headIndex + amount);
		}
		protected abstract T Create(int index, object state);
		protected virtual void Dispose(T element) { }
		public T this[int index] {
			get { return buffer[Clamp(index + headIndex)]; }
		}
		public void Rebuild(int count, object state) {
			foreach(T element in this) Dispose(element);
			headIndex = 0;
			while(buffer.Count > count) buffer.RemoveAt(0);
			for(int index = 0; index < buffer.Count; ++index)
				buffer[index] = Create(index, state);
			for(int index = buffer.Count; index < count; ++index)
				buffer.Add(Create(index, state));
		}
		public int Count {
			get { return buffer.Count; }
		}
		private int headIndex;
		private List<T> buffer = new List<T>();
		#region IEnumerable members
		public IEnumerator<T> GetEnumerator() { return buffer.GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
		#endregion
	}
}