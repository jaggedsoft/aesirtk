using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Aesir.Nexus {
	/// <summary>
	///		An entry in a DAT file that contains information about a specific file in a DAT archive.
	///	</summary>
	///	<see cref="Aesir.Nexus.DatHeader"/>
	/// <see cref="Aesir.Nexus.DatReader"/>
	public class DatEntry {
		private string name;
		private uint size, offset;
		/// <summary>
		///		Gets the size, in bytes, of this file.
		///	</summary>
		public uint Size { get { return size; } }
		/// <summary>
		///		Gets the name (with extension) of this file.
		///	</summary>
		public string Name { get { return name; } }
		/// <summary>
		///		Gets the absolute offset that indicates where this entry is located in the DAT file.
		/// </summary>
		public uint Offset { get { return offset; } }
		public DatEntry(string name, uint offset, uint size) {
			this.name = name;
			this.offset = offset;
			this.size = size;
		}
		/// <summary>
		///		Gets a "local stream" that can be used to read data associated with this entry.
		/// </summary>
		/// <param name="stream">
		///		The file stream upon which to build the "local stream" wrapper. This is usually
		///		<c>DatReader.BaseStream</c>. Note that this stream will be modified when you modify
		///		the resultant "local stream".
		/// </param>
		/// <returns>
		///		A "local stream" that can be used to read file data. Note that the lifetime of this
		///		"local stream" is tied to that of the provided stream. Also note that disposing the
		///		"local stream" will not dispose the original stream. This stream is read-only.
		/// </returns>
		/// <see cref="Aesir.Nexus.DatReader.BaseStream"/>
		public Stream GetLocalStream(Stream stream) {
			return new LocalStream(this, stream);
		}
		// This class essentially transforms absolute stream operations into relative stream
		// operations.
		private class LocalStream : Stream {
			private DatEntry entry;
			private Stream stream;
			public LocalStream(DatEntry entry, Stream stream) {
				this.entry = entry;
				this.stream = stream;
				stream.Position = ClampPosition(stream.Position);
			}
			private long ClampPosition(long position) {
				// Clamp the specified absolute position
				if(position >= entry.offset + entry.size) return entry.offset + entry.size - 1;
				else if(position < entry.offset) return entry.offset;
				return position;
			}
			public override bool CanRead { get { return stream.CanRead; } }
			public override bool CanSeek { get { return stream.CanSeek; } }
			public override bool CanWrite { get { return false; } }
			public override void Flush() { stream.Flush(); }
			public override long Length { get { return entry.size; } }
			public override long Position {
				get { return stream.Position - entry.offset; }
				set { stream.Position = ClampPosition(value + entry.offset); }
			}
			public override int Read(byte[] buffer, int offset, int count) {
				if(Position + count > Length) count = (int)(Length - Position - 1);
				return stream.Read(buffer, offset, count);
			}
			public override long Seek(long offset, SeekOrigin origin) {
				long absoluteOffset = 0;
				// Calculate the absolute offset using the seek origin
				if(origin == SeekOrigin.Begin)
					absoluteOffset = offset + entry.offset;
				else if(origin == SeekOrigin.End)
					absoluteOffset = entry.offset + entry.size - offset;
				else if(origin == SeekOrigin.Current)
					absoluteOffset = stream.Position + offset;
				absoluteOffset = ClampPosition(absoluteOffset);
				stream.Seek(absoluteOffset, SeekOrigin.Begin);
				return Position;
			}
			#region Unsupported operations
			public override void SetLength(long value) { throw new NotSupportedException(); }
			public override void Write(byte[] buffer, int offset, int count) {
				throw new NotSupportedException();
			}
			#endregion
		}
	}
}