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
	class GraphicBrowserForm : Form {
		public GraphicBrowserForm() {
			FormBorderStyle = FormBorderStyle.SizableToolWindow;
			Text = "Graphic Browser";
			TabControl tabControl = new TabControl();
			tabControl.Dock = DockStyle.Fill;
			tabControl.TabPages.Add(new FloorTileBrowserTabPage(this));
			tabControl.TabPages.Add(new ObjectTileBrowserTabPage(this));
			Controls.Add(tabControl);
			DataBindings.Add(new Binding("Size", settings, "FormSize", true,
				DataSourceUpdateMode.OnPropertyChanged));
			DataBindings.Add(new Binding("Location", settings, "FormLocation", true,
				DataSourceUpdateMode.OnPropertyChanged));
			settings.SettingsLoaded += delegate(object sender, SettingsLoadedEventArgs args) {
				splitterDistance = settings.SplitterDistance; };
		}
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
		#region Tile providers
		private interface ITileProvider {
			int TileCount { get; }
			int GetTileIndex(int index);
		}
		private class SimpleTileProvider : ITileProvider {
			private int tileCount;
			public int TileCount { get { return tileCount; } }
			public int GetTileIndex(int index) {
				Debug.Assert(index < tileCount);
				return index;
			}
			public SimpleTileProvider(int tileCount) {
				this.tileCount = tileCount;
			}
		}
		private class CategoryTileProvider : ITileProvider {
			public int TileCount {
				get { throw new Exception("The method or operation is not implemented."); }
			}
			public int GetTileIndex(int index) {
				throw new Exception("The method or operation is not implemented.");
			}
		}
		#endregion
		private abstract class TileBrowserPanel<TTile> : Panel where TTile : Tile, new() {
			protected int TileWidth { get { return Width / Tile.Width; } }
			protected int TileHeight { get { return Height / Tile.Height; } }
		}
		private class FloorTileBrowserPanel : TileBrowserPanel<FloorTile> {
			private class Buffer : CircularBuffer<TileHandle<FloorTile>[]> {
				public Buffer(FloorTileBrowserPanel panel) {
					this.panel = panel;
				}
				private FloorTileBrowserPanel panel;
				protected override TileHandle<FloorTile>[] Factory() {
					return new TileHandle<FloorTile>[panel.TileWidth];
				}
				public void Rebuild() {
					Resize(panel.TileHeight + CacheRows * 2);
					for(int rowIndex = 0; rowIndex < panel.TileHeight + CacheRows * 2; ++rowIndex) {
						TileHandle<FloorTile>[] row = this[rowIndex + CacheRows];
						for(int columnIndex = 0; columnIndex < panel.TileWidth; ++columnIndex) {
							row[columnIndex] = FloorTileManager.Default.GetTile(100);
						}
					}
				}
			}
			private int baseIndex = 0;
			public FloorTileBrowserPanel() {
				buffer = new Buffer(this);
				// Because we have to resize the buffer when the control is resized (a costly
				// operation), do not redraw or update the control while it is being resized.
				ResizeRedraw = false;
				scrollBar.Dock = DockStyle.Right;
				scrollBar.Enabled = false; // Disable the scroll bar until we have a TileProvider
				scrollBar.Scroll += new ScrollEventHandler(scrollBar_Scroll);
				scrollBar.LargeChange = Tile.Height;
				Controls.Add(scrollBar);
				TileProvider = new SimpleTileProvider(500); // TEMP: A temporary tile provider
				buffer.Rebuild();
			}
			void scrollBar_Scroll(object sender, ScrollEventArgs args) {
			}
			protected override void OnResize(EventArgs args) {
				base.OnResize(args);
			}
			protected override void OnPaint(PaintEventArgs args) {
				base.OnPaint(args);
				for(int rowIndex = 0; rowIndex < TileHeight; ++rowIndex) {
					TileHandle<FloorTile>[] row = buffer[rowIndex + CacheRows];
					for(int columnIndex = 0; columnIndex < TileWidth; ++columnIndex) {
						TileHandle<FloorTile> floorTile = row[columnIndex];
						Point point = new Point(columnIndex * Tile.Width, rowIndex * Tile.Height);
						((FloorTile)floorTile).Draw(args.Graphics, point);
					}
				}
			}
			private void UpdateScrollInfo() {
				scrollBar.Enabled = true;
				scrollBar.Maximum = (tileProvider.TileCount / TileWidth) * Tile.Height;
			}
			private void OnTileProviderChanged(EventArgs args) {
				Debug.Assert(tileProvider != null);
				UpdateScrollInfo();
			}
			private ITileProvider tileProvider = null;
			public ITileProvider TileProvider {
				set {
					tileProvider = value;
					OnTileProviderChanged(EventArgs.Empty);
				}
			}
			private VScrollBar scrollBar = new VScrollBar();
			private Buffer buffer;
			private const int CacheRows = 2;
		}
		private class ObjectTileBrowserPanel : TileBrowserPanel<ObjectTile> { }
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
		}
		private int splitterDistance;
		private Settings settings = new Settings();
		private sealed class Settings : ApplicationSettingsBase {
			[UserScopedSetting()]
			[DefaultSettingValue("150")]
			public int SplitterDistance {
				get { return (int)this["SplitterDistance"]; }
				set { this["SplitterDistance"] = value; }
			}
			[UserScopedSetting()]
			[DefaultSettingValue("400, 400")]
			public Size FormSize {
				get { return (Size)this["FormSize"]; }
				set { this["FormSize"] = value; }
			}
			[UserScopedSetting()]
			[DefaultSettingValue("100, 100")]
			public Point FormLocation {
				get { return (Point)this["FormLocation"]; }
				set { this["FormLocation"] = value; }
			}
		}
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