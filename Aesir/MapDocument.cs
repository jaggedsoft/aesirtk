using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using Aesir.Util;
using Aesir.Nexus;

namespace Aesir {
	class MapDocument {
		public delegate void Visitor(Vector tileLocation, TileCell cell);
		private class Section {
			private TileCell[,] cells = new TileCell[Size, Size];
			public TileCell[,] Cells {
				get { return cells; }
			}
			public override string ToString() {
				return "Offset:" + offset.ToString();
			}
			public Vector VectorToRelative(Vector absolutePoint) {
				return absolutePoint - (offset * Size);
			}
			public Vector VectorToAbsolute(Vector relativePoint) {
				return relativePoint + (offset * Size);
			}
			public void AcceptVisitorInArea(Visitor visitor, Rectangle area) { }
			public void AcceptVisitor(Visitor visitor) {
				for(int y = 0; y < Size; ++y) {
					for(int x = 0; x < Size; ++x)
						visitor(VectorToAbsolute(new Vector(x, y)), cells[x, y]);
				}
			}
			public Vector Offset {
				get { return offset; }
			}
			private Vector offset;
			public Section(Vector offset) {
				this.offset = offset;
				for(int y = 0; y < Size; ++y) {
					for(int x = 0; x < Size; ++x)
						cells[x, y] = new TileCell();
				}
			}
			public const int Size = 5;
		}
		private Section center = new Section(new Vector(0, 0));
		Vector centerIndex = new Vector(0, 0);
		private Section[,] sections = new Section[1, 1];
		public int SectionWidth {
			get { return sections.GetLength(0); }
		}
		public int SectionHeight {
			get { return sections.GetLength(1); }
		}
		public MapDocument() {
			sections[0, 0] = center;
		}
		/// <summary>
		///		This function will expand the map in a certain direction, resizing the <c>sections</c>
		///		array and modifying <c>centerIndex</c>.
		/// </summary>
		/// <param name="shift">
		///		A vector representing the direction to expand the map in. For example, if this vector
		///		is (1, 0), the map will be expanded horizontally to the right.
		/// </param>
		/// <remarks>
		///		If any element of the shift vector is negative (that is, if you are expanding upwards
		///		or to the left), the <c>sections</c> array will be shifted in the opposite direction
		///		to make room for the new row or column. The <c>centerIndex</c> will be modified to
		///		accomodate this change.
		/// </remarks>
		/// <returns>
		///		The amount the <c>sections</c> array was shifted.
		/// </returns>
		private Vector Expand(Vector shift) {
			Section[,] oldSections = sections;
			int oldWidth = SectionWidth, oldHeight = SectionHeight;
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
		///		If the provided index is invalid (for example, if a component is negative), this
		///		function will expand the map so that it encompasses the index, and then return a
		///		new, valid index. This function will also initialize the section at the sepcified
		///		index if that section has not yet been initialized.
		/// </summary>
		/// <param name="index">
		///		The index into the <c>sections</c> array to be validated.
		/// </param>
		/// <returns>
		///		An index into the <c>sections</c> array that is valid.
		/// </returns>
		private Vector ValidateIndex(Vector index) {
			Vector shift = new Vector();
			if(index.X >= SectionWidth) shift += new Vector(index.X - SectionWidth + 1, 0);
			else if(index.X < 0) shift += new Vector(-index.X, 0);
			if(index.Y >= SectionHeight) shift += new Vector(0, index.Y - SectionHeight + 1);
			else if(index.Y < 0) shift += new Vector(0, -index.Y);
			Vector translate = Expand(shift);
			index += translate;
			if(sections[index.X, index.Y] == null)
				sections[index.X, index.Y] = new Section(GetOffsetFromIndex(index));
			return index;
		}
		private bool IsValidIndex(Vector index) {
			return (index.X >= 0 && index.X < SectionWidth) &&
				(index.Y >= 0 && index.Y < SectionHeight);
		}
		private Vector GetOffsetFromIndex(Vector index) { return index - centerIndex; }
		private Vector GetIndexFromOffset(Vector index) { return index + centerIndex; }
		private Vector GetIndexFromPoint(Vector point) {
			return GetIndexFromOffset(point / Section.Size);
		}
		private Section GetSectionFromPoint(Vector point) {
			Vector index = ValidateIndex(GetIndexFromPoint(point));
			return sections[index.X, index.Y];
		}
		public void AcceptVisitorInArea(Visitor visitor, Rectangle area) { }
		public void AcceptVisitor(Visitor visitor) {
			for(int y = 0; y < SectionHeight; ++y) {
				for(int x = 0; x < SectionWidth; ++x)
					sections[x, y].AcceptVisitor(visitor);
			}
		}
		public TileCell this[int x, int y] {
			get {
				Vector point = new Vector(x, y);
				Section section = GetSectionFromPoint(point);
				Vector relativePoint = section.VectorToRelative(point);
				return section.Cells[relativePoint.X, relativePoint.Y];
			}
		}
	}
}