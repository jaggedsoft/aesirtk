using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Configuration;

namespace Aesir {
	class MainForm : Form {
		public MainForm() {
			graphicBrowserForm = new GraphicBrowser();
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
			graphicBrowserForm.Show();
			graphicBrowserForm.Focus(); // TEMP
		}
		private GraphicBrowser graphicBrowserForm;
		private StatusBar statusBar = new StatusBar();
		protected override void OnLayout(LayoutEventArgs args) {
			base.OnLayout(args);
			mapView.Location = new Point(0, 0);
			mapView.Size = ClientRectangle.Size - new Size(0, statusBar.Height);
		}
		private Settings.MainForm settings = Settings.MainForm.Default;
		public MapView MapView {
			get { return mapView; }
		}
		private MapView mapView;
		protected override void OnFormClosing(FormClosingEventArgs args) {
			graphicBrowserForm.Close();
			settings.Save();
			base.OnFormClosing(args);
		}
	}
}