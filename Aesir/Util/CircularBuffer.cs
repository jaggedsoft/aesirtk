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
		public void Resize(int count) {
			while(buffer.Count > count) buffer.RemoveAt(0);
			for(int index = 0; index < buffer.Count; ++index) buffer[index] = Factory();
			while(buffer.Count < count) buffer.Add(Factory());
		}
		public int Count { get { return buffer.Count; } }
		protected abstract T Factory();
		private int headIndex;
		private List<T> buffer = new List<T>();
	}
	class SimpleCircularBuffer<T> : CircularBuffer<T> where T : new() {
		protected override T Factory() { return new T(); }
	}
}