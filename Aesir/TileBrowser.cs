using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Aesir.Util;
using System.Diagnostics;
using System.Xml;
using System.IO;
using System.Collections;
using System.Drawing;
using C5;

namespace Aesir {
	/// <summary>
	///		<c>TileBrowser</c> is the abstract base class for <c>FloorTileBrowser</c> and
	///		<c>ObjectTileBrowser</c>. Tile browsers allow the user to select and filter tiles. In
	///		particular, the <c>TileBrowser</c> provides services related to categories, which can
	///		be used to filter tiles.
	/// </summary>
	abstract class TileBrowser : Panel {
		public TileBrowser(ITileProvider tileProvider) {
			this.tileProvider = tileProvider;
			categoryManager = new CategoryManager();
			viewerPanel = CreateViewerPanel(categoryManager, tileProvider);
			categoryPanel = new CategoryPanel(categoryManager, tileProvider, viewerPanel);
			try {
				categoryManager.Import(ConfigFileName);
			} catch(FileNotFoundException) { }
			// Setup the SplitContainer. The TileBrowser consists of two panes: CategoryPanel on the
			// left, ViewerPanelBase on the right.
			splitContainer.Panel1.Controls.Add(categoryPanel);
			categoryPanel.Dock = DockStyle.Fill;
			splitContainer.Panel2.Controls.Add(viewerPanel);
			viewerPanel.Dock = DockStyle.Fill;
			splitContainer.Dock = DockStyle.Fill;
			Controls.Add(splitContainer);
			ParentChanged += delegate(object sender, EventArgs args) {
				Form parentForm = FindForm();
				parentForm.FormClosing += delegate(
					object innerSender, FormClosingEventArgs innerArgs) {
					
					OnParentFormClosing();
				};
			};
		}
		private void OnParentFormClosing() {
			categoryManager.Export(ConfigFileName);
		}
		private string ConfigFileName {
			get { return string.Format("{0}Categories.xml", tileProvider.TileType); }
		}
		private CategoryManager categoryManager;
		private CategoryPanel categoryPanel;
		private SplitContainer splitContainer = new SplitContainer();
		private ITileProvider tileProvider;
		private ViewerPanelBase viewerPanel;
		// This factory is necessary to get an instance of a specialization of the abstract
		// ViewerPanelBase from a derived class.
		protected abstract ViewerPanelBase CreateViewerPanel(
			CategoryManager categoryManager, ITileProvider tileProvider);
		public event EventHandler SelectionChanged {
			add { viewerPanel.SelectionChanged += value; }
			remove { viewerPanel.SelectionChanged -= value; }
		}
		public TileGroup Selection {
			get { return viewerPanel.Selection; }
		}
		// Used to allow the CategoryPanel to communicate with the ViewerPanelBase
		private interface ICategoryConsumer {
			ICategory Category { set; }
		}
		/// <summary>
		///		A deriver of <c>ViewerPanelBase</c> should display tiles to the user and allow him or
		///		her to select them with the mouse. The <c>ViewerPanelBase</c> is positioned to the
		///		right of the <c>CategoryPanel</c> in the <c>TileBrowser</c>.
		/// </summary>
		/// <remarks>
		///		This class must be specialized by the deriver, and exposed through the factory method
		///		<c>CreateViewerPanel</c>.
		/// </remarks>
		protected abstract class ViewerPanelBase : Panel, ICategoryConsumer {
			protected int TileWidth {
				get { return Width / Tile.Width; }
			}
			protected int TileHeight {
				get { return Height / Tile.Height + 1; }
			}
			protected virtual int PriorityBase {
				get { return 0; }
			}
			protected ViewerPanelBase(CategoryManager categoryManager, ITileProvider tileProvider) {
				DoubleBuffered = true;
				Click += delegate(object sender, EventArgs args) { Focus(); };
				this.categoryManager = categoryManager;
				this.tileProvider = tileProvider;
			}
			private ITileProvider tileProvider;
			private CategoryManager categoryManager;
			protected ITileProvider TileProvider {
				get { return tileProvider; }
			}
			protected CategoryManager CategoryManager {
				get { return categoryManager; }
			}
			private ICategory category;
			ICategory ICategoryConsumer.Category {
				set {
					category = value;
					OnCategoryChanged(EventArgs.Empty);
				}
			}
			protected ICategory Category {
				get { return category; }
			}
			protected virtual void OnCategoryChanged(EventArgs args) { }
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
		}
		protected interface ICategory {
			int Count { get; }
			int this[int index] { get; }
		}
		protected class Category : ICategory {
			private TreeSet<int> indices = new TreeSet<int>();
			private string name;
			public string Name {
				get { return name; }
				set { name = value; }
			}
			public int Count {
				get { return indices.Count; }
			}
			public int this[int index] {
				get { return indices[index]; }
			}
			public IEnumerable<int> Indices {
				get { return indices; }
			}
			public void Add(int index) { indices.UpdateOrAdd(index); }
			public void Remove(int index) { indices.Remove(index); }
			public bool Contains(int index) { return indices.Contains(index); }
			internal Category(CategoryManager categoryManager, string name) {
				this.categoryManager = categoryManager;
				indices.CollectionChanged += delegate(object sender) {
					this.categoryManager.OnCategoriesChanged(EventArgs.Empty);
				};
				this.name = name;
			}
			private CategoryManager categoryManager;
			internal void Save(XmlElement element, XmlDocument document) {
				element.SetAttribute("name", name);
				StringBuilder innerTextBuilder = new StringBuilder();
				foreach(int index in indices)
					innerTextBuilder.Append(index.ToString() + ":");
				element.InnerText = innerTextBuilder.ToString();
			}
			internal static Category Load(CategoryManager categoryManager, XmlElement element) {
				Category category = new Category(categoryManager, element.GetAttribute("name"));
				string[] stringIndices = element.InnerText.Split(new char[] { ':' },
					StringSplitOptions.RemoveEmptyEntries);
				foreach(string stringIndex in stringIndices)
					category.indices.Add(int.Parse(stringIndex));
				return category;
			}
		}
		private class TilesCategory : ICategory {
			private int count;
			public TilesCategory(ITileProvider tileProvider) {
				count = tileProvider.TileCount;
			}
			int ICategory.Count {
				get { return count; }
			}
			int ICategory.this[int index] {
				get { return index; }
			}
		}
		protected class CategoryManager {
			internal CategoryManager() { }
			public void Export(string path) {
				XmlDocument document = new XmlDocument();
				XmlElement documentElement = document.CreateElement("Categories");
				foreach(Category category in categories) {
					XmlElement categoryElement = document.CreateElement("Category");
					category.Save(categoryElement, document);
					documentElement.AppendChild(categoryElement);
				}
				document.AppendChild(documentElement);
				document.Save(path);
			}
			public void Import(string path) {
				Import(path, false);
			}
			public void Import(string path, bool merge) {
				if(!merge) categories.Clear();
				XmlDocument document = new XmlDocument();
				document.Load(path);
				foreach(XmlNode childNode in document.DocumentElement.ChildNodes) {
					if(childNode is XmlElement && childNode.Name == "Category")
						categories.Add(Category.Load(this, (XmlElement)childNode));
				}
				OnCategoriesChanged(EventArgs.Empty);
			}
			public IEnumerable<Category> Categories {
				get { return categories; }
			}
			public event EventHandler CategoriesChanged;
			internal virtual void OnCategoriesChanged(EventArgs args) {
				if(CategoriesChanged != null) CategoriesChanged(this, args);
			}
			private List<Category> categories = new List<Category>();
			public Category CreateCategory(string name) {
				Category category = new Category(this, name);
				categories.Add(category);
				OnCategoriesChanged(EventArgs.Empty);
				return category;
			}
			public void RemoveCategory(Category category) {
				categories.Remove(category);
				OnCategoriesChanged(EventArgs.Empty);
			}
		}
		private class CategoryPanel : Panel {
			public CategoryPanel(CategoryManager categoryManager, ITileProvider tileProvider,
				ICategoryConsumer categoryConsumer) {

				this.categoryConsumer = categoryConsumer;
				this.categoryManager = categoryManager;
				SuspendLayout();
				listView.View = View.Details;
				listView.Columns.Add("Category");
				listView.Columns.Add("Count");
				listView.Columns[1].TextAlign = HorizontalAlignment.Right;
				listView.Columns[1].Width = CountColumnWidth;
				listView.Resize += delegate(object sender, EventArgs args) {
					// Expand the "Category" column to fill the new space, while keeping the size of
					// the "Count" column constant.
					listView.Columns[0].Width = listView.Width - listView.Columns[1].Width;
				};
				listView.FullRowSelect = true;
				listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
				listViewHeader = new ListViewHeader(listView);
				listViewHeader.AllowResizeColumns = false;
				listView.Dock = DockStyle.Fill;
				Controls.Add(listView);
				listView_tilesItem = new ListViewItem("Tiles");
				{
					TilesCategory tilesCategory = new TilesCategory(tileProvider);
					listView_tilesItem.Tag = tilesCategory;
					categoryConsumer.Category = tilesCategory;
				}
				listView_tilesItem.SubItems.Add(tileProvider.TileCount.ToString());
				listView.Items.Add(listView_tilesItem);
				listView_tilesItem.Selected = true;
				categoryManager.CategoriesChanged += delegate(object sender, EventArgs args) {
					UpdateListView();
				};
				listView.ItemSelectionChanged += listView_ItemSelectionChanged;
				listView.LabelEdit = true;
				listView.BeforeLabelEdit += new LabelEditEventHandler(listView_BeforeLabelEdit);
				listView.AfterLabelEdit += new LabelEditEventHandler(listView_AfterLabelEdit);
				listView.HideSelection = false;
				listView.MultiSelect = false;
				ContextMenu = new ContextMenu();
				ContextMenu.Popup += ContextMenu_Popup;
				ResumeLayout();
			}
			void ContextMenu_Popup(object sender, EventArgs args) {
				ContextMenu.MenuItems.Clear();
				if(listView.SelectedItems.Count > 0 &&
					listView.SelectedItems[0].Index != listView_tilesItem.Index) {

					ContextMenu.MenuItems.AddRange(new MenuItem[] {
						new MenuItem("Rename", ContextMenu_rename_Click),
						new MenuItem("Delete", ContextMenu_delete_Click)
					});
				}
			}
			private void ContextMenu_rename_Click(object sender, EventArgs args) {
				if(listView.SelectedItems.Count == 0) return;
				listView.SelectedItems[0].BeginEdit();
			}
			private void ContextMenu_delete_Click(object sender, EventArgs args) {
				if(listView.SelectedItems.Count == 0) return;
				ListViewItem selectedItem = listView.SelectedItems[0];
				categoryManager.RemoveCategory((Category)selectedItem.Tag);
				selectedItem.Remove();
			}
			private void listView_AfterLabelEdit(object sender, LabelEditEventArgs args) {
				// Update the category's name to reflect the user's label edit
				Category category = (Category)listView.Items[args.Item].Tag;
				category.Name = args.Label;
			}
			private void listView_BeforeLabelEdit(object sender, LabelEditEventArgs args) {
				// Make it so you can't edit the "Tiles" item
				if(args.Item == listView_tilesItem.Index) args.CancelEdit = true;
			}
			private void listView_ItemSelectionChanged(object sender,
				ListViewItemSelectionChangedEventArgs args) {

				categoryConsumer.Category = (ICategory)args.Item.Tag;
			}
			private ICategoryConsumer categoryConsumer;
			private CategoryManager categoryManager;
			private const int CountColumnWidth = 45; // The size of the "Count" column
			private ListViewHeader listViewHeader; // Used to disable resizing the columns
			private ListView listView = new ListView();
			// The "Tiles" item is a pseudo category that can be selected to display all tiles
			private ListViewItem listView_tilesItem;
			private void UpdateListView() {
				listView.Items.Clear();
				listView.Items.Add(listView_tilesItem);
				foreach(Category category in categoryManager.Categories) {
					ListViewItem categoryItem = new ListViewItem(category.Name);
					categoryItem.SubItems.Add(category.Count.ToString());
					categoryItem.Tag = category;
					listView.Items.Add(categoryItem);
				}
			}
		}
	}
	class FloorTileBrowser : TileBrowser {
		public FloorTileBrowser(ITileProvider tileProvider)
			: base(tileProvider) {
			Debug.Assert(tileProvider.TileType == TileType.FloorTile);
		}
		protected override ViewerPanelBase CreateViewerPanel(
			CategoryManager categoryManager, ITileProvider tileProvider) {
			return new ViewerPanel(categoryManager, tileProvider);
		}
		private class ViewerPanel : ViewerPanelBase {
			public ViewerPanel(CategoryManager categoryManager, ITileProvider tileProvider)
				: base(categoryManager, tileProvider) {
				buffer = new Buffer(this);
				scrollBar.Dock = DockStyle.Right;
				scrollBar.LargeChange = 2;
				Controls.Add(scrollBar);
				// Make the scroll bar respond to the mouse wheel
				MouseWheel += delegate(object sender, MouseEventArgs args) {
					int delta = scrollBar.SmallChange * (args.Delta / -120);
					scrollBar.Value = MathUtil.Clamp(scrollBar.Value + delta, 0, scrollBar.Maximum);
				};
				scrollBar.ValueChanged += scrollBar_ValueChanged;
				// Refresh the panel whenever the mouse is moved to reflect the highlight box
				MouseMove += delegate(object sender, MouseEventArgs args) { Refresh(); };
				ContextMenu = new ContextMenu();
				ContextMenu.Popup += ContextMenu_Popup;
			}
			private void ContextMenu_Popup(object sender, EventArgs args) {
				ContextMenu.MenuItems.Clear();
				MenuItem contextMenu_addItem = new MenuItem("Add to category"),
					contextMenu_removeItem = new MenuItem("Remove from category");
				int tileIndex = HoveringTile.Index;
				foreach(Category category in CategoryManager.Categories) {			
					MenuItem categoryItem = new MenuItem(category.Name);
					categoryItem.Tag = category;
					if(category.Contains(tileIndex)) {
						categoryItem.Click += delegate(object innerSender, EventArgs innerArgs) {
							((Category)((MenuItem)innerSender).Tag).Remove(tileIndex);
						};
						contextMenu_removeItem.MenuItems.Add(categoryItem);
					} else {
						categoryItem.Click += delegate(object innerSender, EventArgs innerArgs) {
							((Category)((MenuItem)innerSender).Tag).Add(tileIndex);
						};
						contextMenu_addItem.MenuItems.Add(categoryItem);
					}
				}
				{
					MenuItem newCategoryItem = new MenuItem("New category...");
					newCategoryItem.Click += delegate(object innerSender, EventArgs innerArgs) {
						InputDialog inputDialog = new InputDialog("Enter a name for the new category:");
						if(inputDialog.ShowDialog() == DialogResult.OK)
							CategoryManager.CreateCategory(inputDialog.Input).Add(tileIndex);
					};
					contextMenu_addItem.MenuItems.Add(newCategoryItem);
				}
				if(contextMenu_removeItem.MenuItems.Count == 0) {
					MenuItem menuItem = new MenuItem("No categories");
					menuItem.Enabled = false;
					contextMenu_removeItem.MenuItems.Add(menuItem);
				}
				ContextMenu.MenuItems.Add(contextMenu_addItem);
				ContextMenu.MenuItems.Add(contextMenu_removeItem);
			}
			private class Buffer : ScrollingBuffer<TileHandle[]> {
				public Buffer(ViewerPanel viewerPanel) {
					this.viewerPanel = viewerPanel;
				}
				private ViewerPanel viewerPanel;
				public TileHandle this[int x, int y] {
					get { return this[y][x]; }
				}
				protected override TileHandle[] Create(int absoluteIndex) {
					int rowIndex = absoluteIndex, columnIndex = 0;
					TileHandle[] row = new TileHandle[viewerPanel.TileWidth];
					for(; columnIndex < viewerPanel.TileWidth; ++columnIndex) {
						int index = rowIndex * Width + columnIndex;
						ICategory category = viewerPanel.Category;
						if(index < category.Count) {
							row[columnIndex] = viewerPanel.TileProvider.GetTile(category[index], 0);
							row[columnIndex].Load += delegate(object sender, EventArgs args) {
								viewerPanel.Refresh();
							};
						}
					}
					return row;
				}
				protected override void Dispose(TileHandle[] row) {
					foreach(TileHandle handle in row) {
						if(handle != null) handle.Dispose();
					}
				}
				private int Width {
					get { return viewerPanel.TileWidth; }
				}
				private int Height {
					get { return viewerPanel.TileHeight; }
				}
				public override int Size {
					get { return (int)Math.Ceiling((float)viewerPanel.Category.Count / (float)Width); }
				}
			}
			// This modified TileWidth accessor takes into account the scrollbar on the right side
			// of the panel.
			private new int TileWidth {
				get { return (Width - scrollBar.Width) / Tile.Width; }
			}
			private Buffer buffer;
			private ScrollBar scrollBar = new VScrollBar();
			void scrollBar_ValueChanged(object sender, EventArgs args) {
				buffer.Offset = scrollBar.Value;
				Refresh();
			}
			protected override void OnResize(EventArgs args) {
				base.OnResize(args);
				buffer.BufferSize = TileHeight;
				UpdateScrollBar();
			}
			protected override void OnMouseMove(MouseEventArgs args) {
				base.OnMouseMove(args);
				clientMousePosition = (Vector)args.Location;
			}
			private Vector clientMousePosition = new Vector(0, 0);
			protected override void OnMouseDown(MouseEventArgs args) {
				base.OnMouseDown(args);
				SelectedTile = buffer[TileMousePosition.X, TileMousePosition.Y];
			}
			private TileHandle HoveringTile {
				get { return buffer[TileMousePosition.X, TileMousePosition.Y]; }
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
					TileHandle[] row = buffer[rowIndex];
					if(row == null) continue;
					for(int columnIndex = 0; columnIndex < TileWidth; ++columnIndex) {
						TileHandle tile = row[columnIndex];
						if(tile != null) {
							Point point = new Point(columnIndex * Tile.Width, rowIndex * Tile.Height);
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
			protected override void OnCategoryChanged(EventArgs args) {
				SelectedTile = null;
				buffer.Rebuild();
				Refresh();
				UpdateScrollBar();
			}
			private void UpdateScrollBar() {
				scrollBar.Maximum = buffer.Size;
				scrollBar.Enabled = scrollBar.Maximum > Height;
			}
		}
	}
}