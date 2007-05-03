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
	/// Use the <c>ArchiveInfo.GetFile</c> method to get a file entry, and then use the
	/// <c>ArchiveInfo.File.Offset</c> property to determine where to seek to find the file data.
	/// </remarks>
	class ArchiveInfo {
		public struct File {
			private int offset;
			/// <summary>The absolute offset to find this file in the archive.</summary>
			public int Offset { get { return offset; } }
			internal File(int offset) { this.offset = offset; }
		}
		private Dictionary<string, File> files;
		public ICollection<string> FileNames { get { return files.Keys; } }
		public File GetFile(string targetFileName) {
			if(files == null) throw new InvalidOperationException();
			foreach(string fileName in files.Keys) {
				if(fileName.ToLower() == targetFileName.ToLower())
					return files[fileName];
			}
			throw new Exception();
		}
		/// <summary>Read the archive header from the data stream.</summary>
		public void Read(Stream stream) {
			BinaryReader binaryReader = new BinaryReader(stream);
			files = new Dictionary<string, File>();
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
				files.Add(name, new File(offset));
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