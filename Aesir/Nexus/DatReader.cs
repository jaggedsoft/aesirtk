using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;

namespace Aesir.Nexus {
	public class DatReader : IDisposable {
		public DatReader(string path) {
			fileStream = new FileStream(path, FileMode.Open);
			ReadHeader();
		}
		private void ReadHeader() {
			fileStream.Seek(0, SeekOrigin.Begin);
			BinaryReader binaryReader = new BinaryReader(fileStream);
			// Read the length-prefixed list of file entries
			uint count = (binaryReader.ReadUInt32() - 1);
			uint[] offsets = new uint[count];
			string[] names = new string[count];
			for(int index = 0; index < count; ++index) {
				offsets[index] = binaryReader.ReadUInt32();
				// File names are always 13 bytes, and null-terminated
				byte[] nameBuffer = binaryReader.ReadBytes(13);
				// Resize the array according to the null-terminator because Encoding.ASCII.GetString
				// does not take the null-terminator into account.
				Array.Resize<byte>(ref nameBuffer, Array.IndexOf<byte>(nameBuffer, 0));
				names[index] = Encoding.ASCII.GetString(nameBuffer);
			}
			uint size;
			for(int index = 0; index < count; ++index) {
				if(index < count - 1) size = offsets[index + 1] - offsets[index];
				else size = fileStream.Length - offsets[index];
				entries.Add(new DatEntry(names[index], offsets[index], size));
			}
		}
		/// <summary>Get a list of <c>DatEntry</c> objects contained in this archive.</summary>
		/// <see cref="Aesir.Nexus.DatEntry" />
		public IList<DatEntry> Entries { get { return entries; } }
		private List<DatEntry> entries = new List<DatEntry>();
		/// <summary>Find an entry given an entry file name.</summary>
		/// <param name="name">The case-insensitive name of an entry.</param>
		/// <returns>A reference to the specified entry, or <c>null</c>.</returns>
		public DatEntry FindEntry(string name) {
			name = name.ToLower();
			foreach(DatEntry entry in entries) {
				if(entry.Name.ToLower() == name)
					return entry;
			}
			return null;
		}
		protected void Dispose(bool disposing) {
			if(fileStream != null) {
				fileStream.Dispose();
				fileStream = null;
			}
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		~DatReader() { Dispose(false); }
		public bool CanRead { get { return fileStream.CanRead; } }
		internal FileStream FileStream { get { return fileStream; } }
		private FileStream fileStream = null;
	}
}