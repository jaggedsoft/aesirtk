using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;

namespace Aesir.Nexus {
	/// <summary>
	///		This class allows you to read data from Nexus's DAT files, which are simple uncompressed
	///		archives.
	/// </summary>
	/// <see cref="Aesir.Nexus.DatHeader"/>
	/// <see cref="Aesir.Nexus.DatEntry"/>
	public class DatReader : IDisposable {
		public DatReader(string path) {
			stream = new FileStream(path, FileMode.Open);
			header = new DatHeader(stream);
		}
		public DatReader(string path, DatHeader header) {
			stream = new FileStream(path, FileMode.Open);
			this.header = header;
		}
		private DatHeader header;
		/// <summary>
		///		Gets a list of <c>DatEntry</c> objects contained in this archive.
		/// </summary>
		/// <see cref="Aesir.Nexus.DatEntry"/>
		public IList<DatEntry> Entries { get { return header.Entries; } }
		/// <summary>
		///		Gets a <c>DatEntry</c> given a file name.
		/// </summary>
		/// <param name="name">
		///		The case-insensitive file name of an entry.
		/// </param>
		/// <returns>
		///		A reference to the specified entry, or null if the entry was not found.
		/// </returns>
		public DatEntry GetEntry(string name) {
			name = name.ToLower();
			foreach(DatEntry entry in Entries) {
				if(entry.Name.ToLower() == name)
					return entry;
			}
			return null;
		}
		protected virtual void Dispose(bool disposing) {
			if(!disposed) {
				if(stream != null) {
					stream.Dispose();
					stream = null;
				}
				disposed = true;
			}
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		~DatReader() { Dispose(false); }
		private bool disposed = false;
		/// <summary>
		///		Gets the underlying <c>Stream</c>.
		/// </summary>
		public Stream BaseStream {
			get {
				if(disposed) throw new ObjectDisposedException("DatReader");
				return stream;
			}
		}
		private Stream stream;
	}
}