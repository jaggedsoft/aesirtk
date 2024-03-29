using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Aesir.Nexus {
	class ArchiveEntry {
		private int offset;
		private string name;
		public string Name {
			get { return name; }
		}
		/// <summary>
		///		The absolute offset to this file in the archive file.
		///	</summary>
		public int Offset {
			get { return offset; }
		}
		internal ArchiveEntry(string name, int offset) {
			this.name = name;
			this.offset = offset;
		}
	}
	/// <summary>
	///		Information about a Nexus archive. Nexus stores graphics data in simple uncompressed
	///		files, which have the file extension DAT.
	/// </summary>
	/// <remarks>
	///		Use the <c>ArchiveHeader.GetEntry</c> method to get a file entry, and then use the
	///		<c>ArchiveEntry.Offset</c> property to determine where to seek to find the file data.
	/// </remarks>
	class ArchiveHeader {
		private Dictionary<string, ArchiveEntry> entries;
		public ICollection<string> EntryNames {
			get { return entries.Keys; }
		}
		public IDictionary<string, ArchiveEntry> Entries {
			get { return entries; }
		}
		public ArchiveEntry GetEntry(string targetEntryName) {
			if(entries == null) throw new InvalidOperationException();
			foreach(string entryName in entries.Keys) {
				if(entryName.ToLower() == targetEntryName.ToLower())
					return entries[entryName];
			}
			return null;
		}
		public void Read(Stream stream) {
			BinaryReader binaryReader = new BinaryReader(stream);
			entries = new Dictionary<string, ArchiveEntry>();
			// Read the length-prefixed list of file entries
			// The count stored at the beginning of the file includes a bogus file entry (the last
			// one), whose name is a series of nulls, and whose size represents the size of the
			// entire archive. To get the number of non-bogus entries, subtract 1 from this count.
			int count = (int)(binaryReader.ReadUInt32() - 1);
			for(int index = 0; index < count; ++index) {
				int offset = (int)binaryReader.ReadUInt32();
				// File names are always 13 bytes, and null-terminated
				byte[] nameBuffer = binaryReader.ReadBytes(13);
				// Resize the array according to the null-terminator because Encoding.ASCII.GetString
				// does not take the null-terminator into account
				Array.Resize<byte>(ref nameBuffer, Array.IndexOf<byte>(nameBuffer, 0));
				string name = Encoding.ASCII.GetString(nameBuffer);
				entries.Add(name, new ArchiveEntry(name, offset));
			}
		}
		public ArchiveHeader() { }
		public ArchiveHeader(string path) {
			using(FileStream stream = new FileStream(path, FileMode.Open))
				Read(stream);
		}
		public ArchiveHeader(Stream stream) {
			Read(stream);
		}
	}
}