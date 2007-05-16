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
		#region Index providers
		private interface IIndexProvider {
			/// <summary>
			///		Gets the number of tiles this <c>IIndexProvider</c> provides.
			/// </summary>
			int Count { get; }
			/// <summary>
			///		Transform a "local" index into an "absolute" index.
			/// </summary>
			int this[int localIndex] { get; }
		}
		/// <summary>
		///		This simple index provider does not perform any special mapping from "local" indices
		///		to "absolute" indices; the indexer implementation simply returns the provided index.
		/// </summary>
		private class SimpleIndexProvider : IIndexProvider {
			private int count;
			public int Count { get { return count; } }
			public int this[int localIndex] {
				get {
					Debug.Assert(localIndex < count && localIndex >= 0);
					return localIndex;
				}
			}
			public SimpleIndexProvider(int count) {
				this.count = count;
			}
		}
		#endregion
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
			// With the Buffer and the IRowProvider, the orientation of a "row" depends on whether
			// we are dealing with a FloorTileBrowserPanel or an ObjectTileBrowserPanel. For floor
			// tiles, a row is horizontal. For object tiles, a row is vertical. In both cases, a
			// "row" is aligned perpendicularly to the scroll bar.
			protected interface IRowProvider {
				int GetIndex(int rowIndex, int columnIndex);
			}
			protected class TileView : CircularBuffer<TileHandle<TTile>[]> {
				public TileView(TileBrowserPanel<TTile> panel, IRowProvider rowProvider) {
					this.panel = panel;
					this.rowProvider = rowProvider;
				}
				private TileBrowserPanel<TTile> panel;
				private IRowProvider rowProvider;
				protected override TileHandle<TTile>[] Factory() {
					return new TileHandle<TTile>[ColumnCount];
				}
				private int ColumnCount {
					get {
						if(panel.Orientation == Orientation.Vertical)
							return panel.TileWidth;
						else return panel.TileHeight;
					}
				}
				private int RowCount {
					get {
						if(panel.Orientation == Orientation.Vertical)
							return panel.TileHeight;
						else return panel.TileWidth;
					}
				}
				private int TotalRowCount {
					get { return RowCount + RowCache * 2; }
				}
				private void UpdateRow(int rowIndex) {
					TileHandle<TTile>[] row = base[rowIndex];
					for(int columnIndex = 0; columnIndex < row.Length; ++columnIndex) {
						int index = rowProvider.GetIndex(rowIndex - RowCache, columnIndex);
						if(index >= 0 && index < panel.indexProvider.Count) {
							index = panel.indexProvider[index];
							row[columnIndex] = TileManager<TTile>.Default.GetTile(index);
						}
					}
				}
				private void Dispose() {
					foreach(TileHandle<TTile>[] row in this) {
						foreach(TileHandle<TTile> tile in row) {
							if(tile != null) tile.Dispose();
						}
					}
				}
				public void Draw(Graphics graphics) {
					for(int rowIndex = 0; rowIndex < RowCount; ++rowIndex) {
						for(int columnIndex = 0; columnIndex < ColumnCount; ++columnIndex) {
							TileHandle<TTile> tile = this[rowIndex][columnIndex];
							Point point;
							if(panel.Orientation == Orientation.Vertical)
								point = new Point(columnIndex, rowIndex);
							else point = new Point(rowIndex, columnIndex);
							point = new Point(point.X * Tile.Width, point.Y * Tile.Height);
							if(tile != null) ((TTile)tile).Draw(graphics, point);
						}
					}
				}
				public new void Advance(int amount) {
					base.Advance(amount);
					if(amount > RowCount) {
						Rebuild();
						return;
					}
					if(amount > 0) {
						for(int index = TotalRowCount - amount; index < TotalRowCount; ++index)
							UpdateRow(index);
					} else {
						for(int index = 0; index < Math.Abs(amount); ++index)
							UpdateRow(index);
					}
				}
				public void Rebuild() {
					Dispose();
					Resize(TotalRowCount);
					for(int rowIndex = 0; rowIndex < TotalRowCount; ++rowIndex)
						UpdateRow(rowIndex);
				}
				public new TileHandle<TTile>[] this[int index] {
					get { return base[index + RowCache]; }
					set { base[index + RowCache] = value; }
				}
			}
			protected TileView tileView;
			private IIndexProvider indexProvider;
			protected virtual void OnIndexProviderChanged(EventArgs args) { }
			public IIndexProvider IndexProvider {
				get { return indexProvider; }
				set {
					indexProvider = value;
					OnIndexProviderChanged(EventArgs.Empty);
				}
			}
			protected abstract Orientation Orientation { get; }
			private const int RowCache = 2;
		}
		private class FloorTileBrowserPanel : TileBrowserPanel<FloorTile> {
			protected override Orientation Orientation {
				get { return Orientation.Vertical; }
			}
			private class RowProvider : IRowProvider {
				public RowProvider(FloorTileBrowserPanel panel) {
					this.panel = panel;
				}
				private FloorTileBrowserPanel panel;
				public int GetIndex(int rowIndex, int columnIndex) {
					int baseIndex = 0;
					baseIndex = panel.scrollBar.Value * panel.TileWidth;
					return baseIndex + rowIndex * panel.TileWidth + columnIndex;
				}
			}
			public FloorTileBrowserPanel() {
				tileView = new TileView(this, new RowProvider(this));
				ResizeRedraw = false;
				scrollBar.Dock = DockStyle.Right;
				scrollBar.Enabled = false; // Disable the scroll bar until we have a TileProvider
				scrollBar.Scroll += new ScrollEventHandler(scrollBar_Scroll);
				scrollBar.LargeChange = 4;
				Controls.Add(scrollBar);
				IndexProvider = new SimpleIndexProvider(500); // TEMP: A temporary tile provider
				tileView.Rebuild();
			}
			void scrollBar_Scroll(object sender, ScrollEventArgs args) {
				int delta = args.NewValue - args.OldValue;
				tileView.Advance(delta);
				Refresh();
			}
			protected override void OnResize(EventArgs args) {
				base.OnResize(args);
				tileView.Rebuild();
				UpdateScrollBar();
			}
			protected override void OnPaint(PaintEventArgs args) {
				base.OnPaint(args);
				tileView.Draw(args.Graphics);
				/*for(int rowIndex = CacheRows - 1; rowIndex <= TileHeight + 1; ++rowIndex) {
					TileHandle<FloorTile>[] row = buffer[rowIndex];
					for(int columnIndex = 0; columnIndex < TileWidth; ++columnIndex) {
						TileHandle<FloorTile> floorTile = row[columnIndex];
						Point point = new Point(columnIndex * Tile.Width, (rowIndex - CacheRows) * Tile.Height);
						if(floorTile != null) ((FloorTile)floorTile).Draw(args.Graphics, point);
					}
				}*/
			}
			private void UpdateScrollBar() {
				scrollBar.Enabled = true;
				scrollBar.Maximum = (IndexProvider.Count / TileWidth);
			}
			protected override void OnIndexProviderChanged(EventArgs args) {
				base.OnIndexProviderChanged(args);
				UpdateScrollBar();
			}
			private VScrollBar scrollBar = new VScrollBar();
		}
		private class ObjectTileBrowserPanel : TileBrowserPanel<ObjectTile> {
			protected override Orientation Orientation {
				get { return Orientation.Horizontal; }
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