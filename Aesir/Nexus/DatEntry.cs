using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Aesir.Nexus {
	/// <summary>A file entry in a DAT file.</summary>
	public class DatEntry {
		private string name;
		private uint size, offset;
		private DatReader datReader;
		/// <summary>Get the size, in bytes, of this file.</summary>
		public uint Size { get { return size; } }
		/// <summary>Get the name (with extension) of this file.</summary>
		public string Name { get { return name; } }
		public DatEntry(string name, uint offset, uint size, DatReader datReader) {
			this.name = name;
			this.offset = offset;
			this.size = size;
			this.datReader = datReader;
			stream = new StreamWrapper(this);
		}
		/// <summary>
		/// Gets a stream wrapper that can be used to read data associated with this entry.
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		public Stream GetStreamWrapper(Stream stream) {
			return new StreamWrapper(stream);
		}
		private class StreamWrapper : Stream {
			private DatEntry datEntry;
			private Stream stream;
			public StreamWrapper(DatEntry datEntry, Stream stream) {
				this.datEntry = datEntry;
				this.stream = stream;
			}
			public override bool CanRead { get { return stream.CanRead; } }
			public override bool CanSeek { get { return stream.CanSeek; } }
			public override bool CanWrite { get { return false; } }
			public override void Flush() { stream.Flush(); }
			public override long Length { get { return entry.size; } }
			public override long Position {
				get { return stream.Position - entry.offset; }
				set { return stream.Position + entry.offset; }
			}
			public override int Read(byte[] buffer, int offset, int count) {
				/*if(Position + count >= entry.offset + entry.size)
					count -=*/
			}
			public override long Seek(long offset, SeekOrigin origin) {
				long absoluteOffset = 0;
				if(origin == SeekOrigin.Begin)
					absoluteOffset = offset + datEntry.offset;
				else if(origin == SeekOrigin.End)
					absoluteOffset = datEntry.offset + datEntry.size - offset;
				else if(origin == SeekOrigin.Current)
					absoluteOffset = stream.Position + offset;
				if(absoluteOffset < datEntry.offset || absoluteOffset > datEntry.offset + datEntry.size) {
				}
				stream.Seek(offset + datEntry.offset, SeekOrigin.Begin);
				else if(origin == SeekOrigin.Current)
					stream.Seek(offset, SeekOrigin.Begin);
			}

			public override void SetLength(long value) {
				throw new NotSupportedException();
			}

			public override void Write(byte[] buffer, int offset, int count) {
				throw new NotSupportedException();
			}
		}
	}
}