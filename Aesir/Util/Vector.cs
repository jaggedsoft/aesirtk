using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Aesir.Util {
	struct Vector : ICloneable {
		public Vector(int x, int y) {
			this.x = x;
			this.y = y;
		}
		public object Clone() { return MemberwiseClone(); }
		public override bool Equals(object obj) {
			if(obj.GetType() != typeof(Vector)) return false;
			return ((Vector)obj) == this;
		}
		public override int GetHashCode() {
			throw new NotImplementedException();
		}
		public override string ToString() {
			return String.Format("({0}, {1})", x, y);
		}
		public int X {
			get { return x; }
			set { x = value; }
		}
		public int Y {
			get { return y; }
			set { y = value; }
		}
		private int x;
		private int y;
		public static Vector Abs(Vector vector) {
			return new Vector(Math.Abs(vector.x), Math.Abs(vector.y));
		}
		public static Vector Sign(Vector vector) {
			return new Vector(Math.Sign(vector.x), Math.Sign(vector.y));
		}
		#region Logical operators
		public static bool operator ==(Vector left, Vector right) {
			return (left.x == right.x) && (left.y == right.y);
		}
		public static bool operator !=(Vector left, Vector right) {
			return (left.x != right.x) || (left.y != right.y);
		}
		#endregion
		#region Arithmetic operators
		public static Vector operator -(Vector left, Vector right) {
			return new Vector(left.x - right.x, left.y - right.y);
		}
		public static Vector operator +(Vector left, Vector right) {
			return new Vector(left.x + right.x, left.y + right.y);
		}
		public static Vector operator *(Vector left, Vector right) {
			return new Vector(left.x * right.x, left.y * right.y);
		}
		public static Vector operator /(Vector left, Vector right) {
			return new Vector(left.x / right.x, left.y / right.y);
		}
		public static Vector operator -(Vector vector) {
			return new Vector(-vector.x, -vector.y);
		}
		#endregion
		#region Scalar arithmetic operators
		public static Vector operator *(Vector vector, int scalar) {
			return new Vector(vector.x * scalar, vector.y * scalar);
		}
		public static Vector operator *(Vector vector, float scalar) {
			return new Vector((int)(vector.x * scalar), (int)(vector.y * scalar));
		}
		public static Vector operator /(Vector vector, int scalar) {
			return new Vector(vector.x / scalar, vector.y / scalar);
		}
		public static Vector operator /(Vector vector, float scalar) {
			return new Vector((int)(vector.x / scalar), (int)(vector.y / scalar));
		}
		#endregion
		#region Conversion operators
		public static explicit operator Point(Vector vector) {
			return new Point(vector.x, vector.y);
		}
		public static explicit operator Size(Vector vector) {
			return new Size(vector.x, vector.y);
		}
		public static explicit operator Vector(Point point) {
			return new Vector(point.X, point.Y);
		}
		public static explicit operator Vector(Size size) {
			return new Vector(size.Width, size.Height);
		}
		#endregion
	}
}
