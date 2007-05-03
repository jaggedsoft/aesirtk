using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;

namespace DbTool {
	interface IDbFile {
		void Save(string path);
		void Load(string path);
		DbEntry Expand();
		IList<DbEntry> Entries { get; }
	}
	class DbFile<TEntry> : IDbFile where TEntry : DbEntry, new() {
		public void Load(string path) {
			entries.Clear();
			lines.Clear();
			using(TextReader textReader = new StreamReader(path)) {
				while(true) {
					string line = textReader.ReadLine();
					if(line == null) break;
					line = line.Trim(' ');
					if(line.StartsWith("//")) {
						lines.Add(new CommentLine(line));
						continue;
					}
					TEntry entry = DbEntry.FromLine<TEntry>(line);
					entries.Add(entry);
					lines.Add(new EntryLine(entry));
				}
			}
		}
		public DbEntry Expand() {
			DbEntry entry = new TEntry();
			entries.Add(entry);
			return entry;
		}
		public void Save(string path) {
			using(TextWriter textWriter = new StreamWriter(path)) {
				List<DbEntry> pendingEntries = new List<DbEntry>(entries);
				foreach(Line line in lines) {
					if(line is CommentLine) {
						CommentLine commentLine = (CommentLine)line;
						textWriter.WriteLine(commentLine.Message);
					} else if(line is EntryLine) {
						EntryLine entryLine = (EntryLine)line;
						if(pendingEntries.Contains(entryLine.Entry)) {
							entryLine.Entry.Write(textWriter);
							pendingEntries.Remove(entryLine.Entry);
						}
					}
				}
				foreach(TEntry entry in pendingEntries) entry.Write(textWriter);
			}
		}
		private abstract class Line { }
		private class CommentLine : Line {
			private string message;
			public string Message { get { return message; } }
			public CommentLine(string message) { this.message = message; }
		}
		private class EntryLine : Line {
			private TEntry entry;
			public TEntry Entry { get { return entry; } }
			public EntryLine(TEntry entry) { this.entry = entry; }
		}
		private List<Line> lines = new List<Line>();
		private List<DbEntry> entries = new List<DbEntry>();
		public IList<DbEntry> Entries { get { return entries; } }
	}
}
