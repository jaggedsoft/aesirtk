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
	/*class TileBrowserForm : Form {
		public TileBrowserForm(ITileProvider floorTileProvider, ITileProvider objectTileProvider) {
			this.floorTileProvider = floorTileProvider;
			this.objectTileProvider = objectTileProvider;
			SuspendLayout();
			FormBorderStyle = FormBorderStyle.SizableToolWindow;
			Text = "Tile Browser";
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
		public event EventHandler SelectionChanged {
			add {
				floorTileBrowser.SelectionChanged += value;
				objectTileBrowser.SelectionChanged += value;
			}
			remove {
				floorTileBrowser.SelectionChanged -= value;
				objectTileBrowser.SelectionChanged -= value;
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
				SelectedTile = tileView[TileMousePosition.X, TileMousePosition.Y];
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
					TileHandle[] row = tileView[rowIndex];
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
			public TileBrowser(TileBrowserForm tileBrowserForm,
				TileBrowserPanel tileBrowserPanel, ITileProvider tileProvider) {

				categoryPanel = new CategoryPanel(tileProvider);
				splitContainer.Dock = DockStyle.Fill;
				categoryPanel.Dock = DockStyle.Fill;
				tileBrowserPanel.Dock = DockStyle.Fill;
				splitContainer.Panel1.Controls.Add(categoryPanel);
				splitContainer.Panel2.Controls.Add(tileBrowserPanel);
				splitContainer.FixedPanel = FixedPanel.Panel1;
				Controls.Add(splitContainer);
				this.tileBrowserForm = tileBrowserForm;
				this.tileBrowserPanel = tileBrowserPanel;
				tileBrowserForm.splitterDistance = splitContainer.SplitterDistance;
				Enter += delegate(object sender, EventArgs args) {
					splitContainer.SplitterDistance = tileBrowserForm.splitterDistance;
				};
				Leave += delegate(object sender, EventArgs args) {
					tileBrowserForm.splitterDistance = splitContainer.SplitterDistance;
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
			protected TileBrowserForm tileBrowserForm;
			private TileBrowserPanel tileBrowserPanel;
			private SplitContainer splitContainer = new SplitContainer();
			private CategoryPanel categoryPanel;
		}
		private int splitterDistance;
		private Settings.TileBrowserForm settings = Settings.TileBrowserForm.Default;
		private class FloorTileBrowser : TileBrowser {
			public FloorTileBrowser(TileBrowserForm tileBrowserForm)
				: base(tileBrowserForm, new FloorTileBrowserPanel(),
				tileBrowserForm.floorTileProvider) {

				Text = "Floor tiles";
			}
		}
		private class ObjectTileBrowser : TileBrowser {
			public ObjectTileBrowser(TileBrowserForm tileBrowserForm)
				: base(tileBrowserForm, new ObjectTileBrowserPanel(),
				tileBrowserForm.objectTileProvider) {

				Text = "Object tiles";
			}
		}
	}*/
}