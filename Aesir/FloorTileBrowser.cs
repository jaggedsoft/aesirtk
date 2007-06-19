using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Aesir.Util;
using System.Drawing;

namespace Aesir {
	class FloorTileBrowser : TileBrowser {
		public FloorTileBrowser(ITileProvider tileProvider) : base(tileProvider) {
		}
		protected override ViewerPanelBase CreateViewerPanel(CategoryPanel categoryPanel) {
			return new ViewerPanel(categoryPanel);
		}
		private class ViewerPanel : ViewerPanelBase {
			// This modified TileWidth accessor takes into account the scrollbar on the right side
			// of the panel.
			private new int TileWidth {
				get { return (Width - ScrollBar.Width) / Tile.Width; }
			}
			private class TileBuffer : TileBufferBase {
				public TileBuffer(ViewerPanel viewerPanel) : base(viewerPanel) { }
				protected override int GetTileIndex(object state, int index, int elementIndex) {
					return ((int)state) + (elementIndex + index * ElementCount);
				}
				public TileHandle this[int x, int y] {
					get { return this[y][x]; }
				}
			}
			private TileBuffer tileBuffer;
			private int TileOffset {
				get { return ScrollBar.Value * TileWidth; }
			}
			public ViewerPanel(CategoryPanel categoryPanel) : base(categoryPanel) {
				tileBuffer = new TileBuffer(this);
				ResizeRedraw = false;
				ScrollBar = new VScrollBar();
				ScrollBar.Dock = DockStyle.Right;
				ScrollBar.Enabled = false;
				ScrollBar.LargeChange = 2;
				Controls.Add(ScrollBar);
				tileBuffer.Resize(TileHeight, TileWidth, TileOffset);
				// Refresh the panel whenever the mouse is moved to reflect the highlight box
				MouseMove += delegate(object sender, MouseEventArgs args) { Refresh(); };
				// ContextMenu = new ContextMenu(new MenuItem[]{ new MenuItem("Lol") }); // TODO
			}
			protected override void OnScroll(int delta, int value) {
				if(TileProvider != null) {
					tileBuffer.Advance(delta, TileOffset);
					Refresh();
				}
			}
			protected override void OnResize(EventArgs args) {
				base.OnResize(args);
				if(TileProvider != null) {
					tileBuffer.Resize(TileHeight, TileWidth, TileOffset);
					UpdateScrollBar();
				}
			}
			protected override void OnMouseMove(MouseEventArgs args) {
				base.OnMouseMove(args);
				clientMousePosition = (Vector)args.Location;
			}
			private Vector clientMousePosition = new Vector(0, 0);
			protected override void OnMouseDown(MouseEventArgs args) {
				base.OnMouseDown(args);
				SelectedTile = tileBuffer[TileMousePosition.X, TileMousePosition.Y];
			}
			private Vector TileMousePosition {
				get { return (clientMousePosition / (Vector)Tile.Size); }
			}
			private TileHandle SelectedTile {
				get {
					if(Selection == null) return null;
					return Selection[0, 0].FloorTile;
				}
				set {
					if(value == null) Selection = null;
					else {
						TileGroup selection = new TileGroup(1, 1);
						selection[0, 0].FloorTile = value;
						Selection = selection;
					}
					Refresh();
				}
			}
			protected override void OnPaint(PaintEventArgs args) {
				base.OnPaint(args);
				Rectangle? selectedRectangle = null;
				for(int rowIndex = 0; rowIndex < TileHeight; ++rowIndex) {
					TileHandle[] row = tileBuffer[rowIndex];
					for(int columnIndex = 0; columnIndex < TileWidth; ++columnIndex) {
						TileHandle tile  = row[columnIndex];
						Point point = new Point(columnIndex * Tile.Width, rowIndex * Tile.Height);
						if(tile != null) {
							lock(tile.SyncRoot) {
								if(SelectedTile != null && tile.Index == SelectedTile.Index) {
									Tile.DrawInverted(tile, args.Graphics, point);
									selectedRectangle = new Rectangle(point, Tile.Size);
								} else Tile.Draw(tile, args.Graphics, point);
							}
						}
					}
				}
				args.Graphics.DrawRectangle(Pens.White, new Rectangle(
					(Point)(TileMousePosition * (Vector)Tile.Size), Tile.Size));
				if(selectedRectangle != null)
					args.Graphics.DrawRectangle(Pens.Black, selectedRectangle.Value);
			}
			private void UpdateScrollBar() {
				ScrollBar.Enabled = true;
				ScrollBar.Maximum = 100; // TODO: Scrollbar update stuff for FloorTile panel thing
			}
		}
	}
}
