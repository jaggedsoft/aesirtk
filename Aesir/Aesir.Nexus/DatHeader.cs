using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Aesir.Nexus {
	/// <summary>
	///		A DAT archive header.
	/// </summary>
	/// <remarks>
	///		In most cases, you should use the <c>DatReader</c> to access DAT archives -- but in
	///		some cases, you may wish to precache DAT header information.
	/// </remarks>
	/// <see cref="Aesir.Nexus.DatEntry"/>
	/// <see cref="Aesir.Nexus.DatReader"/>
	public class DatHeader {
		private IList<DatEntry> entries = new List<DatEntry>();
		internal IList<DatEntry> Entries { get { return entries; } }
		public DatHeader(string path) { Read(path); }
		public DatHeader(Stream stream) { Read(stream); }
		public DatHeader() { }
		public void Read(Stream stream) {
			BinaryReader binaryReader = new BinaryReader(stream);
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
				// Determine the size using the difference between adjacent offsets
				if(index < count - 1) size = (uint)(offsets[index + 1] - offsets[index]);
				else size = (uint)(stream.Length - offsets[index]);
				entries.Add(new DatEntry(names[index], offsets[index], size));
			}
		}
		public void Read(string path) {
			using(FileStream fileStream = new FileStream(path, FileMode.Open))
				Read(fileStream);
		}
	}
}