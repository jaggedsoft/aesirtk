using System;
using System.Collections.Generic;
using System.Text;

namespace Aesir.Util {
	abstract class CircularBuffer<T> {
		public void Advance(int amount) {
			headIndex += amount;
			while(headIndex < 0) headIndex += buffer.Count;
			headIndex %= buffer.Count;
		}
		public T this[int index] {
			get { return buffer[(index + headIndex) % buffer.Count]; }
			set { buffer[(index + headIndex) % buffer.Count] = value; }
		}
		public void Resize(int newCount) {
			while(buffer.Count > newCount) buffer.RemoveAt(0);
			for(int index = 0; index < buffer.Count; ++index) buffer[index] = Create();
			while(buffer.Count < newCount) buffer.Add(Create());
		}
		protected abstract T Create();
		private int headIndex;
		private List<T> buffer = new List<T>();
	}
}