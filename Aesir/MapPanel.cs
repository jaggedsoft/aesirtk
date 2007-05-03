using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using Aesir.Nexus;
using Aesir.Util;

namespace Aesir {
	class MapPanel : Panel {
		private delegate void RefreshDelegate();
		public MapPanel(MainForm mainForm) {
			DoubleBuffered = true;
			/*for(int y = 0; y < 40; ++y) {
				for(int x = 0; x < 40; ++x) {
					FloorTile tile = FloorTileManager.Default.GetTile(x + y * 40);
					map.SetFloorTile(x, y, tile);
					tile.Load += delegate(object sender, EventArgs args) {
						if(!IsHandleCreated) return;
						RefreshDelegate refreshDelegate = new RefreshDelegate(delegate() { Refresh(); });
						Invoke(refreshDelegate);
					};
				}
			}*/
			mainForm.MouseWheel += new MouseEventHandler(mainForm_MouseWheel);
			camera = new Camera(this);
		}
		void mainForm_MouseWheel(object sender, MouseEventArgs args) {
			camera.Zoom((float)args.Delta / (120 * 4), (Vector)args.Location);
			Refresh();
		}
		protected override void OnPaint(PaintEventArgs args) {
			base.OnPaint(args);
			GraphicsState savedState = args.Graphics.Save();
			camera.Apply(args.Graphics);
			map.AcceptVisitor(delegate(Vector point, FloorTile floorTile, ObjectTile objectTile) {
				if(floorTile != null)
					floorTile.Draw(args.Graphics, (Point)(point * (Vector)Graphic.Size));
			});
			args.Graphics.Restore(savedState);
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
			// TEMP
			//Vector center = new Vector(Width / 2, Height / 2);
			//Console.WriteLine(((Vector)args.Location - center) / camera.scale - camera.translate);
			Console.WriteLine(camera.ScreenToWorld(lastMousePosition));
		}
		private Vector lastMousePosition = new Vector(0, 0);
		private class Camera {
			public float scale = 1; // TEMP: Should be private
			public float Scale { set { scale = value; } }
			// Translation is expressed in world units (pixels)
			public Vector translate = new Vector(0, 0); // TEMP: Should be private
			public void Pan(int amountX, int amountY) {
				translate -= new Vector(amountX, amountY) / scale;
			}
			public const float ScaleMax = 1, ScaleMin = (float)0.5;
			public Vector ScreenToWorld(Vector point) {
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
			public Camera(MapPanel mapPanel) {
				this.mapPanel = mapPanel;
				mapPanel.Resize += delegate(object sender, EventArgs args) {
					center = new Vector(mapPanel.Width / 2, mapPanel.Height / 2); };
			}
			private MapPanel mapPanel;
		}
		protected override void OnResize(EventArgs args) {
			base.OnResize(args);
			Refresh();
		}
		private Camera camera;
		private Map map = new Map();
		public Map Map { get { return map; } }
	}
}
