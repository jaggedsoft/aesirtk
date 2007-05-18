using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Configuration;
using Aesir.Nexus;
using Aesir.Util;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace Aesir {
	class GraphicBrowserForm : Form {
		public GraphicBrowserForm() {
			FormBorderStyle = FormBorderStyle.SizableToolWindow;
			Text = "Graphic Browser";
			tabControl = new TabControl();
			tabControl.Dock = DockStyle.Fill;
			tabControl.TabPages.Add(floorTileBrowser = new FloorTileBrowserTabPage(this));
			tabControl.TabPages.Add(objectTileBrowser = new ObjectTileBrowserTabPage(this));
			Controls.Add(tabControl);
			DataBindings.Add(new Binding("Size", settings, "FormSize", true,
				DataSourceUpdateMode.OnPropertyChanged));
			DataBindings.Add(new Binding("Location", settings, "FormLocation", true,
				DataSourceUpdateMode.OnPropertyChanged));
			settings.SettingsLoaded += delegate(object sender, SettingsLoadedEventArgs args) {
				splitterDistance = settings.SplitterDistance; };
		}
		protected override void OnMouseWheel(MouseEventArgs args) {
			base.OnMouseWheel(args);
			if(tabControl.SelectedTab == floorTileBrowser)
				floorTileBrowser.TileBrowserPanel.OnMouseWheel(args);
			else if(tabControl.SelectedTab == objectTileBrowser)
				objectTileBrowser.TileBrowserPanel.OnMouseWheel(args);
		}
		private TabControl tabControl;
		private FloorTileBrowserTabPage floorTileBrowser;
		private ObjectTileBrowserTabPage objectTileBrowser;
		protected override void OnFormClosing(FormClosingEventArgs args) {
			settings.SplitterDistance = splitterDistance;
			settings.Save();
			base.OnFormClosing(args);
		}
		private class CategoryPanel : Panel {
			public CategoryPanel() {
				SuspendLayout();
				listView.View = View.Details;
				listView.Columns.Add("Category");
				listView.Columns.Add("Count");
				listView.Columns[1].TextAlign = HorizontalAlignment.Right;
				listView.Columns[1].Width = 40;
				listView.Resize += new EventHandler(listView_Resize);
				listView_Resize(listView, EventArgs.Empty);
				listView.FullRowSelect = true;
				listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
				listViewHeader = new ListViewHeader(listView);
				listViewHeader.AllowResizeColumns = false;
				listView.Dock = DockStyle.Fill;
				Controls.Add(listView);
				ResumeLayout();
			}
			private void listView_Resize(object sender, EventArgs args) {
				listView.Columns[0].Width = listView.Width - listView.Columns[1].Width;
			}
			private ListView listView = new ListView();
			private ListViewHeader listViewHeader;
		}
		/// <summary>
		///		This abstract class forms the base for <c>FloorTileBrowserPanel</c> and
		///		<c>ObjectTileBrowserPanel</c>. A <c>TileBrowserPanel</c> presents the user with
		///		a scrollable view of tiles or objects which can be selected with the mouse.
		/// </summary>
		private abstract class TileBrowserPanel<TTile> : Panel where TTile : Tile, new() {
			protected int TileWidth { get { return Width / Tile.Width; } }
			protected int TileHeight { get { return Height / Tile.Height + 1; } }
			protected TileBrowserPanel() {
				DoubleBuffered = true;
			}
			protected abstract void OnScroll(int delta, int value);
			protected void InitializeScrollBar() {
				ScrollBar.ValueChanged += delegate(object sender, EventArgs args) {
					int delta = ScrollBar.Value - oldScrollBarValue;
					oldScrollBarValue = ScrollBar.Value;
					OnScroll(delta, ScrollBar.Value);
				};
				ScrollBar.Scroll += delegate(object sender, ScrollEventArgs args) {
					OnScroll(args.NewValue - args.OldValue, args.NewValue);
				};
			}
			internal virtual new void OnMouseWheel(MouseEventArgs args) {
				int delta = ScrollBar.SmallChange * (args.Delta / -120);
				ScrollBar.Value = MathUtil.Clamp(ScrollBar.Value + delta, 0, ScrollBar.Maximum);
			}
			protected abstract class TileView : CircularBuffer<TileHandle<TTile>[]> {
				protected override void Dispose(TileHandle<TTile>[] element) {
					if(element == null) return;
					foreach(TileHandle<TTile> tile in element) {
						if(tile != null) tile.Dispose();
					}
				}
			}
			private int oldScrollBarValue = 0;
			protected abstract ScrollBar ScrollBar { get; }
		}
		private class FloorTileBrowserPanel : TileBrowserPanel<FloorTile> {
			private new int TileWidth { get { return (Width - scrollBar.Width) / Tile.Width; } }
			protected override ScrollBar ScrollBar {
				get { return scrollBar; }
			}
			private class FloorTileView : TileView {
				public FloorTileView(FloorTileBrowserPanel panel) {
					this.panel = panel;
				}
				private FloorTileBrowserPanel panel;
				protected override TileHandle<FloorTile>[] Create(int index, object stateObj) {
					State state = (State)stateObj;
					TileHandle<FloorTile>[] element = new TileHandle<FloorTile>[state.Width];
					for(int elementIndex = 0; elementIndex < state.Width; ++elementIndex) {
						element[elementIndex] = Program.FloorTileManager.GetTile(
							state.Offset * state.Width + elementIndex + index * state.Width);
						((FloorTile)element[elementIndex]).Load += delegate(object sender, EventArgs args) {
							panel.Refresh();
						};
					}
					return element;
				}
				public struct State {
					private int width;
					private int offset;
					public State(int width, int offset) {
						this.width = width;
						this.offset = offset;
					}
					public int Width { get { return width; } }
					public int Offset { get { return offset; } }
				}
			}
			private FloorTileView tileView;
			private FloorTileView.State GetState() {
				return new FloorTileView.State(TileWidth, scrollBar.Value);
			}
			public FloorTileBrowserPanel() {
				tileView = new FloorTileView(this);
				ResizeRedraw = false;
				scrollBar.Dock = DockStyle.Right;
				scrollBar.Enabled = false; // Disable the scroll bar until we have a TileProvider
				scrollBar.LargeChange = 2;
				Controls.Add(scrollBar);
				InitializeScrollBar();
				ContextMenu = new ContextMenu(new MenuItem[]{ new MenuItem("Lol") });
			}
			protected override void OnScroll(int delta, int value) {
				tileView.Advance(delta, new FloorTileView.State(TileWidth, value));
				Refresh();
			}
			protected override void OnResize(EventArgs args) {
				base.OnResize(args);
				tileView.Rebuild(TileHeight, GetState());
				UpdateScrollBar();
			}
			protected override void OnMouseMove(MouseEventArgs args) {
				base.OnMouseMove(args);
				mouseTileLocation = new Point(args.X / Tile.Width, args.Y / Tile.Height);
				Refresh();
			}
			protected override void OnMouseDown(MouseEventArgs args) {
				base.OnMouseDown(args);
			}
			protected override void OnPaint(PaintEventArgs args) {
				base.OnPaint(args);
				for(int rowIndex = 0; rowIndex <= TileHeight; ++rowIndex) {
					TileHandle<FloorTile>[] row = tileView[rowIndex];
					for(int columnIndex = 0; columnIndex < TileWidth; ++columnIndex) {
						TileHandle<FloorTile> tileHandle  = row[columnIndex];
						Point point = new Point(columnIndex * Tile.Width, rowIndex * Tile.Height);
						if(tileHandle != null) ((FloorTile)tileHandle).Draw(args.Graphics, point);
					}
				}
				args.Graphics.DrawRectangle(Pens.Black, mouseTileLocation.X * Tile.Width,
					mouseTileLocation.Y * Tile.Height, Tile.Width, Tile.Height);
			}
			private Point mouseTileLocation = new Point();
			private void UpdateScrollBar() {
				scrollBar.Enabled = true;
				scrollBar.Maximum = 100; // TODO
			}
			private VScrollBar scrollBar = new VScrollBar();
		}
		private class ObjectTileBrowserPanel : TileBrowserPanel<ObjectTile> {
			protected override ScrollBar ScrollBar {
				get { return null; } // TODO
			}
			protected override void OnScroll(int delta, int value) {
			}
		}
		private class TileBrowserTabPage<TTile> : TabPage where TTile : Tile, new() {
			public TileBrowserTabPage(GraphicBrowserForm graphicBrowserForm,
				TileBrowserPanel<TTile> tileBrowserPanel) {

				splitContainer.Dock = DockStyle.Fill;
				categoryPanel.Dock = DockStyle.Fill;
				tileBrowserPanel.Dock = DockStyle.Fill;
				splitContainer.Panel1.Controls.Add(categoryPanel);
				splitContainer.Panel2.Controls.Add(tileBrowserPanel);
				splitContainer.FixedPanel = FixedPanel.Panel1;
				Controls.Add(splitContainer);
				this.graphicBrowserForm = graphicBrowserForm;
				this.tileBrowserPanel = tileBrowserPanel;
				graphicBrowserForm.splitterDistance = splitContainer.SplitterDistance;
				Enter += delegate(object sender, EventArgs args) {
					splitContainer.SplitterDistance = graphicBrowserForm.splitterDistance; };
				Leave += delegate(object sender, EventArgs args) {
					graphicBrowserForm.splitterDistance = splitContainer.SplitterDistance; };
			}
			protected GraphicBrowserForm graphicBrowserForm;
			private TileBrowserPanel<TTile> tileBrowserPanel;
			private SplitContainer splitContainer = new SplitContainer();
			private CategoryPanel categoryPanel = new CategoryPanel();
			public TileBrowserPanel<TTile> TileBrowserPanel {
				get { return tileBrowserPanel; }
			}
		}
		private int splitterDistance;
		private Settings.GraphicBrowserForm settings = Settings.GraphicBrowserForm.Default;
		private class FloorTileBrowserTabPage : TileBrowserTabPage<FloorTile> {
			public FloorTileBrowserTabPage(GraphicBrowserForm graphicBrowserForm)
				: base(graphicBrowserForm, new FloorTileBrowserPanel()) {
				Text = "Floor tiles";
			}
		}
		private class ObjectTileBrowserTabPage : TileBrowserTabPage<ObjectTile> {
			public ObjectTileBrowserTabPage(GraphicBrowserForm graphicBrowserForm)
				: base(graphicBrowserForm, new ObjectTileBrowserPanel()) {
				Text = "Object tiles";
			}
		}
	}
}