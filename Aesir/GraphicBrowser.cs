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

namespace Aesir {
	class GraphicBrowser : Form {
		public GraphicBrowser(ITileProvider floorTileProvider, ITileProvider objectTileProvider) {
			this.floorTileProvider = floorTileProvider;
			this.objectTileProvider = objectTileProvider;
			SuspendLayout();
			FormBorderStyle = FormBorderStyle.SizableToolWindow;
			Text = "Graphic Browser";
			tabControl = new TabControl();
			tabControl.Dock = DockStyle.Fill;
			tabControl.TabPages.Add(floorTileBrowser = new FloorTileBrowser(this));
			tabControl.TabPages.Add(objectTileBrowser = new ObjectTileBrowser(this));
			Controls.Add(tabControl);
			ResumeLayout();
			DataBindings.Add(new Binding("Size", settings, "FormSize", true,
				DataSourceUpdateMode.OnPropertyChanged));
			DataBindings.Add(new Binding("Location", settings, "FormLocation", true,
				DataSourceUpdateMode.OnPropertyChanged));
			settings.SettingsLoaded += delegate(object sender, SettingsLoadedEventArgs args) {
				splitterDistance = settings.SplitterDistance;
			};
		}
		private ITileProvider floorTileProvider, objectTileProvider;
		protected override void OnMouseWheel(MouseEventArgs args) {
			base.OnMouseWheel(args);
			switch(ActiveTileType) {
			case TileType.FloorTile: floorTileBrowser.OnMouseWheel(args); break;
			case TileType.ObjectTile: objectTileBrowser.OnMouseWheel(args); break;
			}
		}
		private TabControl tabControl;
		private TileBrowser floorTileBrowser, objectTileBrowser;
		public TileType? ActiveTileType { // TODO: Are nullables .NET 2?
			get {
				if(tabControl.SelectedTab == floorTileBrowser)
					return TileType.FloorTile;
				else if(tabControl.SelectedTab == objectTileBrowser)
					return TileType.ObjectTile;
				return null;
			}
		}
		public TileGroup Selection {
			get {
				switch(ActiveTileType) {
				case TileType.FloorTile: return floorTileBrowser.Selection;
				case TileType.ObjectTile: return objectTileBrowser.Selection;
				default: return null;
				}
			}
		}
		protected override void OnFormClosing(FormClosingEventArgs args) {
			settings.SplitterDistance = splitterDistance;
			settings.Save();
			base.OnFormClosing(args);
		}
		private class CategoryPanel : Panel {
			public CategoryPanel(ITileProvider baseTileProvider) {
				this.baseTileProvider = baseTileProvider;
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
			public ITileProvider TileProvider {
				get { return baseTileProvider; } // TEMP
			}
			public event EventHandler TileProviderChanged;
			private ITileProvider baseTileProvider;
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
		private abstract class TileBrowserPanel : Panel {
			protected int TileWidth {
				get { return Width / Tile.Width; }
			}
			protected int TileHeight {
				get { return Height / Tile.Height + 1; }
			}
			protected TileBrowserPanel() {
				DoubleBuffered = true;
			}
			protected ITileProvider tileProvider;
			public ITileProvider TileProvider {
				set {
					tileProvider = value;
					OnTileProviderChanged(EventArgs.Empty);
				}
			}
			private TileGroup selection;
			public TileGroup Selection {
				get { return selection; }
				protected set {
					selection = value;
					OnSelectionChanged(EventArgs.Empty);
				}
			}
			public event EventHandler SelectionChanged;
			protected virtual void OnSelectionChanged(EventArgs args) {
				if(SelectionChanged != null) SelectionChanged(this, args);
			}
			protected virtual void OnTileProviderChanged(EventArgs args) { }
			protected abstract void OnScroll(int delta, int value);
			protected void InitializeScrollBar() {
				ScrollBar.ValueChanged += delegate(object sender, EventArgs args) {
					int delta = ScrollBar.Value - oldScrollBarValue;
					oldScrollBarValue = ScrollBar.Value;
					OnScroll(delta, ScrollBar.Value);
				};
			}
			internal virtual new void OnMouseWheel(MouseEventArgs args) {
				int delta = ScrollBar.SmallChange * (args.Delta / -120);
				ScrollBar.Value = MathUtil.Clamp(ScrollBar.Value + delta, 0, ScrollBar.Maximum);
			}
			protected abstract class TileView : CircularBuffer<TileHandle[]> {
				protected override void Dispose(TileHandle[] element) {
					if(element == null) return;
					foreach(TileHandle tile in element) {
						if(tile != null) tile.Dispose();
					}
				}
				public new void Advance(int amount, object state) {
					if(amount > Count / 2) Rebuild(Count, state);
					else base.Advance(amount, state);
				}
			}
			private int oldScrollBarValue = 0;

			protected abstract ScrollBar ScrollBar { get; }
		}
		private class FloorTileBrowserPanel : TileBrowserPanel {
			private new int TileWidth {
				get { return (Width - scrollBar.Width) / Tile.Width; }
			}
			protected override ScrollBar ScrollBar {
				get { return scrollBar; }
			}
			private class FloorTileView : TileView {
				public FloorTileView(FloorTileBrowserPanel panel) {
					this.panel = panel;
				}
				private FloorTileBrowserPanel panel;
				protected override TileHandle[] Create(int index, object stateObj) {
					State state = (State)stateObj;
					TileHandle[] element = new TileHandle[state.Width];
					for(int elementIndex = 0; elementIndex < state.Width; ++elementIndex) {
						element[elementIndex] = panel.tileProvider.GetTile(
							state.Offset * state.Width + elementIndex + index * state.Width, 0);
						element[elementIndex].Load += delegate(object sender, EventArgs args) {
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
					public int Width {
						get { return width; }
					}
					public int Offset {
						get { return offset; }
					}
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
				scrollBar.Enabled = false;
				scrollBar.LargeChange = 2;
				Controls.Add(scrollBar);
				InitializeScrollBar();
				ContextMenu = new ContextMenu(new MenuItem[]{ new MenuItem("Lol") }); // TODO
			}
			protected override void OnScroll(int delta, int value) {
				if(tileProvider != null) {
					tileView.Advance(delta, new FloorTileView.State(TileWidth, value));
					Refresh();
				}
			}
			protected override void OnResize(EventArgs args) {
				base.OnResize(args);
				if(tileProvider != null) {
					tileView.Rebuild(TileHeight, GetState());
					UpdateScrollBar();
				}
			}
			protected override void OnMouseMove(MouseEventArgs args) {
				base.OnMouseMove(args);
				mouseTileLocation = new Point(args.X / Tile.Width, args.Y / Tile.Height);
				Refresh();
			}
			protected override void OnMouseDown(MouseEventArgs args) {
				base.OnMouseDown(args);
				inputState = new InputState_MouseDrag((Vector)args.Location);
			}
			protected override void OnMouseUp(MouseEventArgs args) {
				base.OnMouseUp(args);
				if(inputState is InputState_MouseDrag) {
					Vector initialLocation = ((InputState_MouseDrag)inputState).InitialLocation,
						location = (Vector)args.Location;
					initialLocation /= (Vector)Tile.Size;
					location /= (Vector)Tile.Size;
					Vector locationDelta = location - initialLocation;
					TileGroup selection = new TileGroup(
						Math.Abs(locationDelta.X), Math.Abs(locationDelta.Y));
					for(int y = initialLocation.Y; y != location.Y; y += Math.Sign(locationDelta.Y)) {
						for(int x = initialLocation.X; x != location.X; x += Math.Sign(locationDelta.X))
							selection[x, y].FloorTile = (TileHandle)tileView[y][x].Clone();
					}
					inputState = null;
				}
			}
			private object inputState;
			private class InputState_MouseDrag {
				private Vector initialLocation;
				public Vector InitialLocation {
					get { return initialLocation; }
				}
				public InputState_MouseDrag(Vector initialLocation) {
					this.initialLocation = initialLocation;
				}
			}
			protected override void OnPaint(PaintEventArgs args) {
				base.OnPaint(args);
				for(int rowIndex = 0; rowIndex <= TileHeight; ++rowIndex) {
					TileHandle[] row = tileView[rowIndex];
					for(int columnIndex = 0; columnIndex < TileWidth; ++columnIndex) {
						TileHandle tile  = row[columnIndex];
						Point point = new Point(columnIndex * Tile.Width, rowIndex * Tile.Height);
						if(tile != null) {
							lock(tile.SyncRoot) {
								//if(tile.Image != null) {
									tile.Draw(args.Graphics, point);
								//}
							}
								//args.Graphics.DrawImage(tile.Image, point); // TEMP
						}
					}
				}
				args.Graphics.DrawRectangle(Pens.Black, mouseTileLocation.X * Tile.Width,
					mouseTileLocation.Y * Tile.Height, Tile.Width, Tile.Height);
			}
			private Point mouseTileLocation = new Point();
			private void UpdateScrollBar() {
				scrollBar.Enabled = true;
				scrollBar.Maximum = 100; // TODO: Scrollbar update stuff for FloorTile panel thing
			}
			private VScrollBar scrollBar = new VScrollBar();
		}
		private class ObjectTileBrowserPanel : TileBrowserPanel {
			protected override ScrollBar ScrollBar {
				get { return null; } // TODO: ScrollBar for ObjectTileBrowserPanel
			}
			protected override void OnScroll(int delta, int value) {
			}
		}
		private class TileBrowser : TabPage {
			public TileBrowser(GraphicBrowser graphicBrowser,
				TileBrowserPanel tileBrowserPanel, ITileProvider tileProvider) {

				categoryPanel = new CategoryPanel(tileProvider);
				splitContainer.Dock = DockStyle.Fill;
				categoryPanel.Dock = DockStyle.Fill;
				tileBrowserPanel.Dock = DockStyle.Fill;
				splitContainer.Panel1.Controls.Add(categoryPanel);
				splitContainer.Panel2.Controls.Add(tileBrowserPanel);
				splitContainer.FixedPanel = FixedPanel.Panel1;
				Controls.Add(splitContainer);
				this.graphicBrowser = graphicBrowser;
				this.tileBrowserPanel = tileBrowserPanel;
				graphicBrowser.splitterDistance = splitContainer.SplitterDistance;
				Enter += delegate(object sender, EventArgs args) {
					splitContainer.SplitterDistance = graphicBrowser.splitterDistance;
				};
				Leave += delegate(object sender, EventArgs args) {
					graphicBrowser.splitterDistance = splitContainer.SplitterDistance;
				};
				tileBrowserPanel.TileProvider = categoryPanel.TileProvider;
				categoryPanel.TileProviderChanged += delegate(object sender, EventArgs args) {
					tileBrowserPanel.TileProvider = categoryPanel.TileProvider;
				};
			}
			public new void OnMouseWheel(MouseEventArgs args) {
				tileBrowserPanel.OnMouseWheel(args);
			}
			public TileGroup Selection {
				get { return tileBrowserPanel.Selection; }
			}
			public event EventHandler SelectionChanged {
				add { tileBrowserPanel.SelectionChanged += value; }
				remove { tileBrowserPanel.SelectionChanged -= value; }
			}
			protected GraphicBrowser graphicBrowser;
			private TileBrowserPanel tileBrowserPanel;
			private SplitContainer splitContainer = new SplitContainer();
			private CategoryPanel categoryPanel;
		}
		private int splitterDistance;
		private Settings.GraphicBrowser settings = Settings.GraphicBrowser.Default;
		private class FloorTileBrowser : TileBrowser {
			public FloorTileBrowser(GraphicBrowser graphicBrowser)
				: base(graphicBrowser, new FloorTileBrowserPanel(),
				graphicBrowser.floorTileProvider) {

				Text = "Floor tiles";
			}
		}
		private class ObjectTileBrowser : TileBrowser {
			public ObjectTileBrowser(GraphicBrowser graphicBrowser)
				: base(graphicBrowser, new ObjectTileBrowserPanel(),
				graphicBrowser.objectTileProvider) {

				Text = "Object tiles";
			}
		}
	}
}