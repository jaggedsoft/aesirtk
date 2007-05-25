using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Aesir.Nexus {
	class ObjectInfo {
		private int height;
		private int[] indices;
		/// <value>
		///		The height of this object slice, in tiles.
		///	</value>
		public int Height {
			get { return height; }
		}
		public int[] Indices {
			get { return indices; }
		}
		public static ObjectInfo[] ReadCollection(Stream stream) {
			BinaryReader binaryReader = new BinaryReader(stream);
			int count = (int)binaryReader.ReadUInt16();
			ObjectInfo[] infoCollection = new ObjectInfo[count];
			infoCollection[0] = new ObjectInfo();
			infoCollection[0].height = 0;
			infoCollection[0].indices = new int[0];
			stream.Seek(9, SeekOrigin.Current);
			for(int index = 0; index < count; ++index) {
				ObjectInfo info = infoCollection[index] = new ObjectInfo();
				stream.Seek(1, SeekOrigin.Current);
				info.height = binaryReader.ReadByte();
				info.indices = new int[info.height];
				for(int subIndex = 0; subIndex < info.height; ++subIndex)
					info.indices[subIndex] = (int)binaryReader.ReadUInt16();
				Array.Reverse(info.indices);
				stream.Seek(5, SeekOrigin.Current);
			}
			return infoCollection;
		}
	}
}