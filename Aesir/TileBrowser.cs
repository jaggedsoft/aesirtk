using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Aesir.Util;
using System.Diagnostics;
using System.Xml;
using System.IO;

namespace Aesir {
	abstract class TileBrowser : Panel {
		public TileBrowser(ITileProvider tileProvider) {
			this.tileProvider = tileProvider;
			categoryPanel = new CategoryPanel(tileProvider);
			viewerPanel = CreateViewerPanel(categoryPanel);
			splitContainer.Panel1.Controls.Add(categoryPanel);
			categoryPanel.Dock = DockStyle.Fill;
			splitContainer.Panel2.Controls.Add(viewerPanel);
			viewerPanel.Dock = DockStyle.Fill;
			splitContainer.Dock = DockStyle.Fill;
			Controls.Add(splitContainer);
		}
		private CategoryPanel categoryPanel;
		private SplitContainer splitContainer = new SplitContainer();
		protected abstract ViewerPanelBase CreateViewerPanel(CategoryPanel categoryPanel);
		private ITileProvider tileProvider;
		private ViewerPanelBase viewerPanel;
		public event EventHandler SelectionChanged {
			add { viewerPanel.SelectionChanged += value; }
			remove { viewerPanel.SelectionChanged -= value; }
		}
		public TileGroup Selection {
			get { return viewerPanel.Selection; }
		}
		protected abstract class ViewerPanelBase : Panel {
			protected int TileWidth {
				get { return Width / Tile.Width; }
			}
			protected int TileHeight {
				get { return Height / Tile.Height + 1; }
			}
			protected virtual int PriorityBase {
				get { return 0; }
			}
			protected ViewerPanelBase(CategoryPanel categoryPanel) {
				DoubleBuffered = true;
				this.categoryPanel = categoryPanel;
			}
			private CategoryPanel categoryPanel;
			protected CategoryPanel CategoryPanel {
				get { return categoryPanel; }
			}
			public ITileProvider TileProvider {
				get { return categoryPanel.TileProvider; }
			}
			private TileGroup selection;
			public TileGroup Selection {
				get { return selection; }
				protected set {
					if(selection != null) selection.Dispose();
					selection = value;
					OnSelectionChanged(EventArgs.Empty);
				}
			}
			public event EventHandler SelectionChanged;
			protected virtual void OnSelectionChanged(EventArgs args) {
				if(SelectionChanged != null) SelectionChanged(this, args);
			}
			protected virtual void OnTileProviderChanged(EventArgs args) { Refresh(); }
			#region ScrollBar utilities
			// This utility code makes the ScrollBar for the panel respond to the mousewheel.
			protected virtual void OnScroll(int delta, int value) { }
			// Invoked by the parent control, TileBrowser
			internal virtual new void OnMouseWheel(MouseEventArgs args) {
				int delta = ScrollBar.SmallChange * (args.Delta / -120);
				ScrollBar.Value = MathUtil.Clamp(ScrollBar.Value + delta, 0, ScrollBar.Maximum);
			}
			private int oldScrollBarValue; // The old scrollbar value is used to obtain a delta
			private ScrollBar scrollBar;
			protected ScrollBar ScrollBar {
				get { return scrollBar; }
				set {
					Debug.Assert(value != null);
					scrollBar = value;
					oldScrollBarValue = scrollBar.Value;
					scrollBar.ValueChanged += delegate(object sender, EventArgs args) {
						int delta = scrollBar.Value - oldScrollBarValue;
						oldScrollBarValue = scrollBar.Value;
						OnScroll(delta, scrollBar.Value);
					};
				}
			}
			#endregion
			/// <summary>
			///		<para>
			///			The <c>TileBufferBase</c>, a refinement of <c>CircularBuffer</c>, is used to hold
			///			references (handles) to the tiles that are being displayed in the panel.
			///			When a <c>TileBufferBase</c> is "advanced" using <c>Advance</c>, the tiles are
			///			shifted in a direction and new tiles are appended to the front or back of
			///			the buffer. A <c>TileBufferBase</c> may also be rebuilt using <c>Rebuild</c>, which
			///			can be used to resize the buffer.
			///		</para>
			///		<para>
			///			Most functions that manipulate the <c>TileBufferBase</c> are passed a state object,
			///			whose meaning depends on the semantics of the derived class, which must
			///			define the method <c>GetTileIndex</c>.
			///		</para>
			/// </summary>
			protected abstract class TileBufferBase : CircularBuffer<TileHandle[]> {
				public TileBufferBase(ViewerPanelBase viewerPanel) {
					this.viewerPanel = viewerPanel;
				}
				private ViewerPanelBase viewerPanel;
				protected ViewerPanelBase ViewerPanel {
					get { return viewerPanel; }
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
						int localIndex = index - CacheFactor;
						int tileIndex = GetTileIndex(state, localIndex, elementIndex);
						if(tileIndex >= 0) {
							int priority = viewerPanel.PriorityBase;
							if(localIndex > Count) priority += Math.Abs(localIndex - Count);
							else if(localIndex < 0) priority += Math.Abs(localIndex - 0);
							element[elementIndex] = viewerPanel.TileProvider.GetTile(tileIndex, priority);
							element[elementIndex].Load += delegate(object sender, EventArgs args) {
								viewerPanel.Refresh();
							};
						}
					}
					return element;
				}
				protected abstract int GetTileIndex(object state, int index, int elementIndex);
				private int elementCount = 0;
				protected int ElementCount {
					get { return elementCount; }
				}
				private new void Rebuild(int count, object state) {
					base.Rebuild(count + CacheFactor * 2, state);
				}
				public void Resize(int count, int elementCount, object state) {
					this.elementCount = elementCount;
					Rebuild(count, state);
				}
				public new void Advance(int amount, object state) {
					if(amount > base.Count / 2) Rebuild(base.Count, state);
					else base.Advance(amount, state);
				}
				public new int Count {
					get { return base.Count - CacheFactor * 2; }
				}
				public new TileHandle[] this[int index] {
					get { return base[index + CacheFactor]; }
				}
				private const int CacheFactor = 4;
			}
		}
		protected class CategoryPanel : Panel {
			public CategoryPanel(ITileProvider baseTileProvider) {
				this.baseTileProvider = baseTileProvider;
				currentTileProvider = baseTileProvider;
				SuspendLayout();
				listView.View = View.Details;
				listView.Columns.Add("Category");
				listView.Columns.Add("Count");
				listView.Columns[1].TextAlign = HorizontalAlignment.Right;
				listView.Columns[1].Width = CountColumnWidth;
				listView.Resize += new EventHandler(listView_Resize);
				listView_Resize(listView, EventArgs.Empty);
				listView.FullRowSelect = true;
				listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
				listViewHeader = new ListViewHeader(listView);
				listViewHeader.AllowResizeColumns = false;
				listView.Dock = DockStyle.Fill;
				Controls.Add(listView);
				ResumeLayout();
				tilesItem = new ListViewItem("Tiles");
				tilesItem.SubItems.Add(baseTileProvider.TileCount.ToString());
				tilesItem.Tag = baseTileProvider;
				Load();
			}
			private ListViewItem tilesItem;
			public void Load() {
				if(!File.Exists(FileName)) {
					UpdateListView();
					return;
				}
				XmlDocument document = new XmlDocument();
				document.Load(FileName);
				UpdateListView();
			}
			public void Save() {
			}
			private string FileName {
				get { return baseTileProvider.TileType.ToString() + "-Categories.xml"; }
			}
			private const int CountColumnWidth = 45;
			private void listView_Resize(object sender, EventArgs args) {
				listView.Columns[0].Width = listView.Width - listView.Columns[1].Width;
			}
			private ListViewHeader listViewHeader;
			private ListView listView = new ListView();
			private ITileProvider baseTileProvider;
			private ITileProvider currentTileProvider;
			public ITileProvider TileProvider {
				get { return currentTileProvider; }
			}
			protected virtual void OnTileProviderChanged(EventArgs args) {
				if(TileProviderChanged != null) TileProviderChanged(this, args);
			}
			private class Category : ITileProvider {
				private List<int> tileIndices = new List<int>();
				public List<int> TileIndices {
					get { return tileIndices; }
				}
				public TileHandle GetTile(int index, int priority) {
					return baseTileProvider.GetTile(tileIndices[index], priority);
				}
				public TileType TileType {
					get { return baseTileProvider.TileType; }
				}
				public int TileCount {
					get { return tileIndices.Count; }
				}
				private ITileProvider baseTileProvider;
				public Category(ITileProvider baseTileProvider) {
					this.baseTileProvider = baseTileProvider;
				}
			}
			public ICollection<string> CategoryNames {
				get { return categories.Keys; }
			}
			public void AddTileToCategory(string categoryName, int tileIndex) {
				Category category;
				if(!categories.TryGetValue(categoryName, out category))
					categories[categoryName] = new Category(baseTileProvider);
				category.TileIndices.Add(tileIndex);
				UpdateListView();
			}
			private void UpdateListView() {
				listView.Items.Clear();
				listView.Items.Add(tilesItem);
				foreach(string categoryName in categories.Keys) {
					ListViewItem categoryItem = new ListViewItem(categoryName);
					Category category = categories[categoryName];
					categoryItem.SubItems.Add(category.TileIndices.Count.ToString());
					categoryItem.Tag = category;
					listView.Items.Add(categoryItem);
				}
			}
			private Dictionary<string, Category> categories = new Dictionary<string, Category>();
			public event EventHandler TileProviderChanged;
		}
	}
}