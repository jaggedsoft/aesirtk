using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using Aesir.Util;
using Aesir.Nexus;

namespace Aesir {
	abstract class MapFileFormat {
		public MapFileFormat() { fileFormats.Add(this); }
		public static ICollection<MapFileFormat> FileFormats { get { return fileFormats; } }
		private static List<MapFileFormat> fileFormats = new List<MapFileFormat>();
		public virtual void Export(string fileName) { }
		public virtual void Import(string fileName) { }
		public virtual bool CanExport(string fileName) { return false; }
		public virtual bool CanImport(string fileName) { return false; }
	}
	class Map {
		public delegate void Visitor(Vector point, FloorTile floorTile, ObjectTile objectTile);
		private struct Cell {
			public FloorTile floorTile;
			public ObjectTile objectTile;
			public Cell(FloorTile floorTile, ObjectTile objectTile) {
				this.floorTile = floorTile;
				this.objectTile = objectTile;
			}
			public static readonly Cell Empty = new Cell(null, null);
		}
		private class Section {
			private Cell[,] cells = new Cell[Size, Size];
			public Cell[,] Cells { get { return cells; } }
			public override string ToString() {
				return "Offset:" + offset.ToString();
			}
			public Vector AbsoluteToRelative(Vector point) { return point - (offset * Size); }
			public Vector RelativeToAbsolute(Vector point) { return point + (offset * Size); }
			/// <param name="area">The area rectangle, in absolute tile coordinates.</param>
			public void AcceptVisitorInArea(Visitor visitor, Rectangle area) { }
			public void AcceptVisitor(Visitor visitor) {
				for(int y = 0; y < Size; ++y) {
					for(int x = 0; x < Size; ++x) {
						Cell cell = cells[x, y];
						Vector point = RelativeToAbsolute(new Vector(x, y));
						visitor(point, cell.floorTile, cell.objectTile);
					}
				}
			}
			/// <remarks>
			/// The offset is specified in sections; that is, to get the offset in terms of tiles,
			/// do offset * <c>Section.Size</c>.
			/// </remarks>
			public Vector Offset { get { return offset; } }
			private Vector offset;
			public Section(Vector offset) {
				this.offset = offset;
			}
			public const int Size = 5;
		}
		private Section center = new Section(new Vector(0, 0));
		Vector centerIndex = new Vector(0, 0);
		private Section[,] sections = new Section[1, 1];
		public Map() {
			sections[0, 0] = center;
		}
		/// <summary>
		/// This function will expand the map in a certain direction, resizing the <c>sections</c>
		/// array and modifying <c>centerIndex</c>.
		/// </summary>
		/// <param name="shift">
		/// A vector representing the direction to expand the map in. For example, if this vector
		/// is (1, 0), the map will be expanded horizontally to the right.
		/// </param>
		private Vector Expand(Vector shift) {
			Section[,] oldSections = sections;
			int oldWidth = oldSections.GetLength(0), oldHeight = oldSections.GetLength(1);
			sections = new Section[oldWidth + Math.Abs(shift.X), oldHeight + Math.Abs(shift.Y)];
			Vector translate = new Vector();
			if(shift.X < 0) translate.X = shift.X;
			if(shift.Y < 0) translate.Y = shift.Y;
			for(int y = 0; y < oldHeight; ++y) {
				for(int x = 0; x < oldWidth; ++x)
					sections[x + translate.X, y + translate.Y] = oldSections[x, y];
			}
			centerIndex += translate;
			return translate;
		}
		/// <summary>
		/// If the provided index is invalid (for example, if a component is negative), this
		/// function will expand the map so that it encompasses the index, and then return
		/// a new, valid index.
		/// </summary>
		private Vector EnsureIndex(Vector index) {
			Vector shift = new Vector();
			if(index.X >= sections.GetLength(0))
				shift += new Vector(index.X - sections.GetLength(0) + 1, 0);
			else if(index.X < 0) shift += new Vector(-index.X, 0);
			if(index.Y >= sections.GetLength(1))
				shift += new Vector(0, index.Y - sections.GetLength(1) + 1);
			else if(index.Y < 0) shift += new Vector(0, -index.Y);
			Vector translate = Expand(shift);
			index += translate;
			if(sections[index.X, index.Y] == null)
				sections[index.X, index.Y] = new Section(GetOffsetFromIndex(index));
			return index;
		}
		private bool IsValidIndex(Vector index) {
			return (index.X >= 0 && index.X < sections.GetLength(0)) &&
				(index.Y >= 0 && index.Y < sections.GetLength(1));
		}
		private Vector GetOffsetFromIndex(Vector index) { return index - centerIndex; }
		private Vector GetIndexFromOffset(Vector offset) { return offset + centerIndex; }
		private Vector GetIndexFromPoint(Vector point) {
			return GetIndexFromOffset(point / Section.Size);
		}
		private Section GetSectionFromPoint(Vector point) {
			Vector index = EnsureIndex(GetIndexFromPoint(point));
			return sections[index.X, index.Y];
		}
		public void AcceptVisitorInArea(Visitor visitor, Rectangle area) { }
		public void AcceptVisitor(Visitor visitor) {
			for(int y = 0; y < sections.GetLength(1); ++y) {
				for(int x = 0; x < sections.GetLength(0); ++x) {
					sections[x, y].AcceptVisitor(visitor);
				}
			}
		}
		delegate void CellPredicate(ref Cell cell);
		private void GetCell(int x, int y, CellPredicate predicate) {
			Vector point = new Vector(x, y);
			Section section = GetSectionFromPoint(point);
			Vector relativePoint = section.AbsoluteToRelative(point);
			predicate(ref section.Cells[relativePoint.X, relativePoint.Y]);
		}
		private Cell GetCell(int x, int y) {
			Cell resultCell = Cell.Empty;
			GetCell(x, y, delegate(ref Cell cell) { resultCell = cell; });
			return resultCell;
		}
		public void SetFloorTile(int x, int y, FloorTile floorTile) {
			GetCell(x, y, delegate(ref Cell cell) { cell.floorTile = floorTile; });
		}
		public void SetObjectTile(int x, int y, ObjectTile objectTile) {
			GetCell(x, y, delegate(ref Cell cell) { cell.objectTile = objectTile; });
		}
	}
}