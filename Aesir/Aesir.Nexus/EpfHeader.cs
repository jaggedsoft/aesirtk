using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Aesir.Nexus {
	/// <summary>
	///		An EPF file header.
	/// </summary>
	/// <remarks>
	///		In most cases, you should use the <c>EpfReader</c> to access EPF files -- but in some
	///		cases, you may wish to precache EPF header information.
	/// </remarks>
	public class EpfHeader {
		private ushort graphicCount;
		/// <summary>
		///		Gets the number of graphics stored in the EPF file.
		/// </summary>
		public ushort GraphicCount { get { return graphicCount; } }
		private uint graphicInfoOffset; // The absolute offset to the array of EpfGraphicInfo structures
		internal uint GraphicInfoOffset { get { return graphicInfoOffset; } }
		public void Read(Stream stream) {
			BinaryReader binaryReader = new BinaryReader(stream);
			graphicCount = binaryReader.ReadUInt16();
			stream.Seek(6, SeekOrigin.Current); // Skip over unknown bytes
			graphicInfoOffset = binaryReader.ReadUInt32();
		}
		public EpfHeader(Stream stream) { Read(stream); }
		// The size of this structure, in bytes, as it is stored in an EPF file
		public const int DataSize = 12;
	}
}