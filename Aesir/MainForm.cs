using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Configuration;

namespace Aesir {
	class MainForm : Form {
		public MainForm() {
			graphicBrowserForm = new GraphicBrowserForm();
			mapPanel = new MapPanel(this);
			Controls.Add(mapPanel);
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
		private GraphicBrowserForm graphicBrowserForm;
		private StatusBar statusBar = new StatusBar();
		protected override void OnLayout(LayoutEventArgs args) {
			base.OnLayout(args);
			mapPanel.Location = new Point(0, 0);
			mapPanel.Size = ClientRectangle.Size - new Size(0, statusBar.Height);
		}
		public MapPanel MapPanel { get { return mapPanel; } }
		private MapPanel mapPanel;
		protected override void OnFormClosing(FormClosingEventArgs args) {
			settings.Save();
			graphicBrowserForm.Close();
			base.OnFormClosing(args);
		}
		private Settings settings = new Settings();
		private sealed class Settings : ApplicationSettingsBase {
			[UserScopedSetting()]
			[DefaultSettingValue("600, 600")]
			public Size FormSize {
				get { return (Size)this["FormSize"];  }
				set { this["FormSize"] = value;  }
			}
			[UserScopedSetting()]
			[DefaultSettingValue("400, 400")]
			public Point FormLocation {
				get { return (Point)this["FormLocation"]; }
				set { this["FormLocation"] = value;  }
			}
		}
	}
}