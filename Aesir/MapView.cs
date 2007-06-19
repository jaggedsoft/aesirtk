using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using Aesir.Nexus;
using Aesir.Util;

namespace Aesir {
	class MapView : Panel {
		//private delegate void RefreshDelegate();
		public MapView(MainForm mainForm) {
			DoubleBuffered = true;
			// TEMP RefreshDelegate refreshDelegate = new RefreshDelegate(delegate() { Refresh(); });
			/* for(int y = 0; y < 40; ++y) {
				for(int x = 0; x < 40; ++x) {
					TileHandle floorTile = mainForm.floorTileProvider.GetTile(x + y * 40, 1);
					mapDocument[x, y].FloorTile = floorTile;
					floorTile.Load += delegate(object sender, EventArgs args) {
						if(!IsHandleCreated) return;
						Invoke(refreshDelegate);
					};
				}
			}*/
			mainForm.MouseWheel += new MouseEventHandler(mainForm_MouseWheel);
			camera = new Camera(this);
		}
		public Vector TileMousePosition {
			get { return new Vector(); } // TODO: MapView.TileMousePosition
		}
		void mainForm_MouseWheel(object sender, MouseEventArgs args) {
			camera.Zoom((float)args.Delta / (120 * 4), (Vector)args.Location);
			Refresh();
		}
		/// <summary>
		///		Converts a point from client coordinates to world tile coordinates.
		/// </summary>
		public Vector VectorToTile(Vector point) {
			Vector world = camera.VectorToWorld(point);
			return new Vector(
				MathUtil.CeilingDivide(world.X, Tile.Width),
				MathUtil.CeilingDivide(world.Y, Tile.Height));
		}
		public event PaintEventHandler PaintClientOverlay;
		public event PaintEventHandler PaintWorldOverlay;
		protected override void OnPaint(PaintEventArgs args) {
			base.OnPaint(args);
			GraphicsState savedState = args.Graphics.Save();
			camera.Apply(args.Graphics);
			mapDocument.AcceptVisitor(delegate(Vector point, TileCell cell) {
				if(cell.FloorTile != null) {
					lock(cell.FloorTile.SyncRoot) {
						Point drawPoint = (Point)(point * (Vector)Tile.Size);
						Tile.Draw(cell.FloorTile, args.Graphics, drawPoint);
					}
				}
			});
			// TEMP: Purple box acts as a landmark for now
			args.Graphics.FillRectangle(Brushes.Purple, new Rectangle(new Point(0, 0), Tile.Size));
			if(PaintWorldOverlay != null) PaintWorldOverlay(this, args);
			args.Graphics.Restore(savedState);
			if(PaintClientOverlay != null) PaintClientOverlay(this, args);
		}
		enum PanState { Pending, Active, None };
		PanState panState = PanState.None;
		protected override void OnMouseDown(MouseEventArgs args) {
			base.OnMouseDown(args);
			if(args.Button == MouseButtons.Middle)
				panState = PanState.Pending;
		}
		protected override void OnMouseUp(MouseEventArgs args) {
			base.OnMouseUp(args);
			if(args.Button == MouseButtons.Middle) {
				if(panState == PanState.Pending) {
					// The user pressed and released the middle mouse button without dragging;
					// reset the zoom.
					camera.Scale = 1;
				}
				panState = PanState.None;
				Cursor = Cursors.Default;
			}
		}
		protected override void OnMouseMove(MouseEventArgs args) {
			base.OnMouseMove(args);
			if(panState != PanState.None) {
				if(panState == PanState.Pending) {
					panState = PanState.Active;
					Cursor = Cursors.SizeAll;
				}
				Vector mousePosition = (Vector)args.Location;
				Vector delta = lastMousePosition - mousePosition;
				camera.Pan(delta.X, delta.Y);
				Refresh();
			}
			lastMousePosition = (Vector)args.Location;
		}
		private Vector lastMousePosition = new Vector(0, 0);
		private class Camera {
			private float scale = 1;
			public float Scale {
				set { scale = MathUtil.Clamp(value, ScaleMin, ScaleMax); }
			}
			// Translation is expressed in absolute world units
			private Vector translate = new Vector(0, 0);
			public void Pan(int amountX, int amountY) {
				translate -= new Vector(amountX, amountY) / scale;
			}
			public const float ScaleMax = 1, ScaleMin = (float)0.5;
			// Converts a point from client coordinates to world coordinates
			public Vector VectorToWorld(Vector point) {
				return (point - center) / scale - translate;
			}
			public void Zoom(float amount, Vector target) {
				scale += amount;
				if(scale > ScaleMax) scale = ScaleMax;
				if(scale < ScaleMin) scale = ScaleMin;
			}
			public void Apply(Graphics graphics) {
				graphics.ScaleTransform(scale, scale, MatrixOrder.Append);
				Vector center = new Vector(mapPanel.Width / 2, mapPanel.Height / 2);
				Vector translate = (this.translate * scale + center);
				graphics.TranslateTransform(translate.X, translate.Y, MatrixOrder.Append);
			}
			private Vector center;
			public Camera(MapView mapPanel) {
				this.mapPanel = mapPanel;
				mapPanel.Resize += delegate(object sender, EventArgs args) {
					center = new Vector(mapPanel.Width / 2, mapPanel.Height / 2);
				};
			}
			private MapView mapPanel;
		}
		protected override void OnResize(EventArgs args) {
			base.OnResize(args);
			Refresh();
		}
		private Camera camera;
		private MapDocument mapDocument = new MapDocument();
		public MapDocument MapDocument {
			get { return mapDocument; }
		}
	}
}
