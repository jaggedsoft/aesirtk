using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Configuration;
using System.Diagnostics;
using Aesir.Util;

namespace Aesir {
	class MainForm : Form {
		public MainForm(ITileProvider floorTileProvider, ITileProvider objectTileProvider) {
			this.floorTileProvider = floorTileProvider;
			this.objectTileProvider = objectTileProvider;
//			tileBrowserForm = new TileBrowserForm(floorTileProvider, objectTileProvider);
			mapView = new MapView(this);
			Controls.Add(mapView);
			Text = "Aesir";
			DataBindings.Add(new Binding("Size", settings, "FormSize", true,
				DataSourceUpdateMode.OnPropertyChanged));
			DataBindings.Add(new Binding("Location", settings, "FormLocation", true,
				DataSourceUpdateMode.OnPropertyChanged));
			MainMenu menu = new MainMenu();
			MenuItem[] fileItems = new MenuItem[] {
				new MenuItem("New"),
				new MenuItem("Open"),
				new MenuItem("Save")
			};
			menu.MenuItems.Add(new MenuItem("&File", fileItems));
			Menu = menu;
			StatusBarPanel panel = new StatusBarPanel();
			panel.BorderStyle = StatusBarPanelBorderStyle.Sunken;
			panel.AutoSize = StatusBarPanelAutoSize.Spring;
			panel.Text = "Ready";
			statusBar.Panels.Add(panel);
			statusBar.ShowPanels = true;
			Controls.Add(statusBar);
			//tileBrowserForm.Show();

			toolStrip.Renderer = new ToolStripSystemRenderer();
			Label label = new Label();
			label.Text = "LOL";
			toolStrip.Items.Add(new ToolStripControlHost(label));
			toolStrip.Items.Add(new ToolStripButton("Pencil"));
			toolStrip.Items.Add(new ToolStripButton("Line"));
			toolStrip.Items.Add(new ToolStripButton("Square"));
			Controls.Add(toolStrip);

			Form test = new Form();
			FloorTileBrowser shit = new FloorTileBrowser(floorTileProvider);
			shit.Dock = DockStyle.Fill;
			test.Controls.Add(shit);
			test.Show();
			// TEMP: stuff
			/*mapView.PaintWorldOverlay += delegate(object sender, PaintEventArgs args) {
				Console.WriteLine(mapView.VectorToTile(mousePosition));
				args.Graphics.DrawRectangle(Pens.Blue, new Rectangle((Point)(mapView.VectorToTile(mousePosition) * (Vector)Tile.Size), Tile.Size));
			};
			mapView.MouseMove += delegate(object sender, MouseEventArgs args) {
				mousePosition = (Vector)args.Location;
				mapView.Refresh();
			};*/
		}
		//private Vector mousePosition; // TEMP
		private ToolStrip toolStrip = new ToolStrip();
		private MapView mapView;
		public ITileProvider floorTileProvider, objectTileProvider; // TEMP: SHOULD BE PRIVATE
		/*private TileBrowserForm tileBrowserForm;
		public TileBrowserForm TileBrowserForm {
			get { return tileBrowserForm; }
		}*/
		private StatusBar statusBar = new StatusBar();
		protected override void OnLayout(LayoutEventArgs args) {
			base.OnLayout(args);
			mapView.Location = new Point(0, toolStrip.Height);
			mapView.Size = ClientRectangle.Size - new Size(0, statusBar.Height + toolStrip.Height);
		}
		private Settings.MainForm settings = Settings.MainForm.Default;
		protected override void OnFormClosing(FormClosingEventArgs args) {
//			tileBrowserForm.Close();
			settings.Save();
			base.OnFormClosing(args);
		}
	}
}