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
		public TileType? ActiveTileType {
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
		///		a scrollable view of tiles or objects that can be selected with the mouse.
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
			private ITileProvider tileProvider;
			public ITileProvider TileProvider {
				get { return tileProvider; }
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
			#region ScrollBar utilities
			// This utility code makes the ScrollBar for the panel respond to the mousewheel. Deriving
			// classes must expose their ScrollBar through the abstract ScrollBar property, call
			// InitializeScrollBar in their constructor (after the ScrollBar has been created), and
			// then handle scrolling events through the virtual OnScroll handler.
			protected virtual void OnScroll(int delta, int value) { }
			protected void InitializeScrollBar() {
				oldScrollBarValue = ScrollBar.Value;
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
			private int oldScrollBarValue; // The old scrollbar value is used to obtain a delta
			protected abstract ScrollBar ScrollBar { get; }
			#endregion
			/// <summary>
			///		<para>
			///			The <c>TileView</c>, a refinement of <c>CircularBuffer</c>, is used to hold
			///			references (handles) to the tiles that are being displayed in the panel.
			///			When a <c>TileView</c> is "advanced" using <c>Advance</c>, the tiles are
			///			shifted in a direction and new tiles are appended to the front or back of
			///			the buffer. A <c>TileView</c> may also be rebuilt using <c>Rebuild</c>, which
			///			can be used to resize the buffer.
			///		</para>
			///		<para>
			///			Most functions that manipulate the <c>TileView</c> are passed a state object,
			///			whose meaning depends on the semantics of the derived class, which must
			///			define the method <c>GetTileIndex</c>.
			///		</para>
			/// </summary>
			protected abstract class TileView : CircularBuffer<TileHandle[]> {
				public TileView(TileBrowserPanel panel) {
					this.panel = panel;
				}
				private TileBrowserPanel panel;
				protected TileBrowserPanel Panel {
					get { return panel; }
				}
				protected override void Dispose(TileHandle[] element) {
					if(element == null) return;
					foreach(TileHandle tile in element) {
						if(tile != null) tile.Dispose();
					}
				}
				protected override TileHandle[] Create(int index, object state) {
					TileHandle[] element = new TileHandle[elementCount];
					for(int elementIndex = 0; elementIndex < elementCount; ++elementIndex) {
						int tileIndex = GetTileIndex(state, index, elementIndex);
						// TODO: Priority for this shit?
						element[elementIndex] = panel.TileProvider.GetTile(tileIndex, 0);
						element[elementIndex].Load += delegate(object sender, EventArgs args) {
							panel.Refresh();
						};
					}
					return element;
				}
				protected abstract int GetTileIndex(object state, int index, int elementIndex);
				private int elementCount = 0;
				protected int ElementCount {
					get { return elementCount; }
				}
				private new void Rebuild(int count, object state) {
					base.Rebuild(count, state);
				}
				public void Resize(int count, int elementCount, object state) {
					this.elementCount = elementCount;
					Rebuild(count, state);
				}
				public new void Advance(int amount, object state) {
					if(amount > Count / 2) Rebuild(Count, state);
					else base.Advance(amount, state);
				}
			}
		}
		private class FloorTileBrowserPanel : TileBrowserPanel {
			// This modified TileWidth accessor takes into account the scrollbar on the right
			// side of the panel.
			private new int TileWidth {
				get { return (Width - scrollBar.Width) / Tile.Width; }
			}
			protected override ScrollBar ScrollBar {
				get { return scrollBar; }
			}
			private class FloorTileView : TileView {
				public FloorTileView(FloorTileBrowserPanel panel) : base(panel) { }
				protected override int GetTileIndex(object state, int index, int elementIndex) {
					return  ((int)state) + (elementIndex + index * ElementCount);
				}
				public TileHandle this[int x, int y] {
					get { return this[y][x]; }
				}
			}
			private FloorTileView tileView;
			private int TileOffset {
				get { return scrollBar.Value * TileWidth; }
			}
			public FloorTileBrowserPanel() {
				tileView = new FloorTileView(this);
				ResizeRedraw = false;
				scrollBar.Dock = DockStyle.Right;
				scrollBar.Enabled = false;
				scrollBar.LargeChange = 2;
				Controls.Add(scrollBar);
				InitializeScrollBar();
				// Refresh the panel whenever the mouse is moved to reflect the highlight box
				MouseMove += delegate(object sender, MouseEventArgs args) { Refresh(); };
				ContextMenu = new ContextMenu(new MenuItem[]{ new MenuItem("Lol") }); // TODO
			}
			protected override void OnScroll(int delta, int value) {
				if(TileProvider != null) {
					tileView.Advance(delta, TileOffset);
					Refresh();
				}
			}
			protected override void OnResize(EventArgs args) {
				base.OnResize(args);
				if(TileProvider != null) {
					tileView.Resize(TileHeight, TileWidth, TileOffset);
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
				SetSelectedTile(tileView[TileMousePosition.X, TileMousePosition.Y]);
			}
			private Vector TileMousePosition {
				get { return (clientMousePosition / ((Vector)Tile.Size)); }
			}
			private TileHandle selectedTile;
			private void SetSelectedTile(TileHandle selectedTile) {
				if(this.selectedTile != null) this.selectedTile.Dispose();
				this.selectedTile = (TileHandle)selectedTile.Clone();
				Refresh();
			}
			protected override void OnPaint(PaintEventArgs args) {
				base.OnPaint(args);
				Rectangle? selectedRectangle = null;
				for(int rowIndex = 0; rowIndex < TileHeight; ++rowIndex) {
					TileHandle[] row = tileView[rowIndex];
					for(int columnIndex = 0; columnIndex < TileWidth; ++columnIndex) {
						TileHandle tile  = row[columnIndex];
						Point point = new Point(columnIndex * Tile.Width, rowIndex * Tile.Height);
						if(tile != null) {
							lock(tile.SyncRoot) {
								tile.Draw(args.Graphics, point);
								if(selectedTile != null && tile.Index == selectedTile.Index)
									selectedRectangle = new Rectangle(point, Tile.Size);
							}
						}
					}
				}
				args.Graphics.DrawRectangle(Pens.Red, new Rectangle(
					(Point)(TileMousePosition * (Vector)Tile.Size), Tile.Size));
				if(selectedRectangle != null)
					args.Graphics.DrawRectangle(Pens.Blue, selectedRectangle.Value);
			}
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