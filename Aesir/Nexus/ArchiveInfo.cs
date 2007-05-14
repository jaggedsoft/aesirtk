using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Aesir.Nexus {
	/// <summary>
	/// Information about a Nexus archive. Nexus stores graphics data in simple uncompressed
	/// archives, which are usually located in the data directory of your NexusTK installation.
	/// </summary>
	/// <remarks>
	/// Use the <c>ArchiveInfo.GetEntry</c> method to get a file entry, and then use the
	/// <c>ArchiveInfo.Entry.Offset</c> property to determine where to seek to find the file data.
	/// </remarks>
	class ArchiveInfo {
		public class Entry {
			private int offset;
			/// <summary>The absolute offset to find this file in the archive.</summary>
			public int Offset { get { return offset; } }
			internal Entry(int offset) { this.offset = offset; }
		}
		private Dictionary<string, Entry> entries;
		public ICollection<string> EntryNames { get { return entries.Keys; } }
		public Entry GetFile(string targetEntryName) {
			if(entries == null) throw new InvalidOperationException();
			foreach(string entryName in entries.Keys) {
				if(entryName.ToLower() == targetEntryName.ToLower())
					return entries[entryName];
			}
			return null;
		}
		/// <summary>Read the archive header from the data stream.</summary>
		public void Read(Stream stream) {
			BinaryReader binaryReader = new BinaryReader(stream);
			entries = new Dictionary<string, Entry>();
			// Read the length-prefixed list of file entries
			int count = (int)(binaryReader.ReadUInt32() - 1);
			for(int index = 0; index < count; ++index) {
				int offset = (int)binaryReader.ReadUInt32();
				// File names are always 13 bytes, and null-terminated
				byte[] nameBuffer = binaryReader.ReadBytes(13);
				// Resize the array according to the null-terminator because Encoding.ASCII.GetString
				// does not take the null-terminator into account
				Array.Resize<byte>(ref nameBuffer, Array.IndexOf<byte>(nameBuffer, 0));
				string name = Encoding.ASCII.GetString(nameBuffer);
				entries.Add(name, new Entry(offset));
			}
		}
		public ArchiveInfo() { }
		public ArchiveInfo(string path) {
			using(FileStream stream = new FileStream(path, FileMode.Open))
				Read(stream);
		}
		public ArchiveInfo(Stream stream) {
			Read(stream);
		}
	}
}