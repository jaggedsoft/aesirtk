using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ArchiveTool {
	class ArchiveInfo {
		public struct File {
			private int offset, size;
			public int Offset { get { return offset; } }
			public int Size { get { return size; } }
			internal File(int offset, int size) {
				this.offset = offset;
				this.size = size;
			}
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
		public void Read(Stream stream) {
			BinaryReader binaryReader = new BinaryReader(stream);
			files = new Dictionary<string, File>();
			int count = (int)(binaryReader.ReadUInt32() - 1);
			int[] offsets = new int[count];
			string[] fileNames = new string[count];
			for(int index = 0; index < count; ++index) {
				offsets[index] = (int)binaryReader.ReadUInt32();
				byte[] fileNameBuffer = binaryReader.ReadBytes(13);
				Array.Resize<byte>(ref fileNameBuffer, Array.IndexOf<byte>(fileNameBuffer, 0));
				fileNames[index] = Encoding.ASCII.GetString(fileNameBuffer);
			}
			for(int index = 0; index < count; ++index) {
				int size;
				if(index < count - 1) size = offsets[index + 1] - offsets[index];
				else size = (int)stream.Length - offsets[index];
				files.Add(fileNames[index], new File(offsets[index], size));
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
	class Program {
		private static void PrintHelp() {
			Console.WriteLine("To extract an archive to a directory use:");
			Console.WriteLine("\tArchiveTool x <ArchivePath> [<OutputDirectory>]");
			Console.WriteLine("To list the contents of an archive use:");
			Console.WriteLine("\tArchiveTool l <ArchivePath>");
		}
		static void Main(string[] args) {
			if(args.Length < 1) {
				PrintHelp();
				return;
			}
			if(args[0] == "l" || args[0] == "x") {
				if(args.Length < 2) {
					PrintHelp();
					return;
				}
				Stream stream = new FileStream(args[1], FileMode.Open);
				ArchiveInfo archive = new ArchiveInfo(stream);
				if(args[0] == "l") {
					foreach(string fileName in archive.FileNames)
						Console.WriteLine(fileName);
				} else if(args[0] == "x") {
					string outputDirectory;
					if(args.Length >= 2) outputDirectory = args[2];
					else outputDirectory = Path.ChangeExtension(args[1], string.Empty).TrimEnd('.');
					if(!Directory.Exists(outputDirectory))
						Directory.CreateDirectory(outputDirectory);
					foreach(string fileName in archive.FileNames) {
						ArchiveInfo.File file = archive.GetFile(fileName);
						stream.Seek(file.Offset, SeekOrigin.Begin);
						FileStream outputFileStream = new FileStream(
							Path.Combine(outputDirectory, fileName), FileMode.Create);
						for(int count = 0; count < file.Size; ++count) {
							int data = stream.ReadByte();
							if(data == -1) break;
							outputFileStream.WriteByte((byte)data);
						}
						outputFileStream.Dispose();
					}
				}
				stream.Dispose();
			} else PrintHelp();
		}
	}
}