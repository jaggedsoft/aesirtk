using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Configuration;

namespace DbTool {
	class MainForm : Form {
		public MainForm() {
			defaultInstance = this;
			UpdateText();
			listBox.Dock = DockStyle.Fill;
			listBox.SelectedValueChanged += new EventHandler(listBox_SelectedValueChanged);
			splitContainer.Panel1.Controls.Add(listBox);
			propertyGrid.Dock = DockStyle.Fill;
			propertyGrid.HelpVisible = false;
			propertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(
				propertyGrid_PropertyValueChanged);
			splitContainer.Panel2.Controls.Add(propertyGrid);
			splitContainer.Dock = DockStyle.Fill;
			Controls.Add(splitContainer);
			MenuItem fileMenu = new MenuItem("&File");
			fileMenu.MenuItems.Add(new MenuItem("&Open", open_Click));
			fileMenu.MenuItems.Add(new MenuItem("&Save", save_Click));
			fileMenu.MenuItems.Add(new MenuItem("E&xit",
				delegate(object sender, EventArgs args) { Close(); }));
			MenuItem editMenu = new MenuItem("&Edit");
			editMenu.MenuItems.Add(new MenuItem("&Add entry", addEntry_Click));
			editMenu.MenuItems.Add(new MenuItem("&Remove entry", removeEntry_Click));
			Menu = new MainMenu(new MenuItem[] { fileMenu, editMenu });
			DataBindings.Add(new Binding("Size", settings, "FormSize"));
			DataBindings.Add(new Binding("Location", settings, "FormLocation"));
		}
		void propertyGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs args) {
			UpdateListBox();
		}
		private void removeEntry_Click(object sender, EventArgs args) {
			if(listBox.SelectedValue != null && file != null) {
				file.Entries.Remove((DbEntry)listBox.SelectedValue);
				listBox.SelectedIndex = 0;
				UpdateListBox();
			}
		}
		private void UpdateListBox() {
			((CurrencyManager)BindingContext[listBox.DataSource]).Refresh();
		}
		private void listBox_SelectedValueChanged(object sender, EventArgs args) {
			propertyGrid.SelectedObject = listBox.SelectedValue;
		}
		private void UpdateText() {
			Text = "DbTool";
			if(file != null)
				Text = Path.GetFileName(filePath) + " - " + Text;
		}
		private void OpenFile(string filePath) {
			this.filePath = filePath;
			file = new DbFile();
			listBox.DataSource = file.Entries;
			UpdateText();
			try { file.Load(filePath); } catch(Exception exception) {
				MessageBox.Show("There was an error while trying to load the file.\r\n" +
					exception.Message, "Error");
			}
			UpdateListBox();
			if(file.Entries.Count > 0) {
				listBox.SelectedIndex = 0;
				propertyGrid.SelectedObject = file.Entries[0];
			}
			listBox.Refresh();
		}
		private void open_Click(object sender, EventArgs args) {
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = openFileDialogFilter;
			DialogResult result = dialog.ShowDialog();
			if(result == DialogResult.Cancel) return;
			OpenFile(dialog.FileName);
		}
		private void addEntry_Click(object sender, EventArgs args) {
			if(file != null) {
				DbEntry entry = file.Expand();
				UpdateListBox();
				listBox.SelectedIndex = file.Entries.IndexOf(entry);
			}
		}
		private void save_Click(object sender, EventArgs args) {
			if(file == null) return;
			try {
				if(file != null) file.Save(filePath);
			} catch(Exception exception) {
				MessageBox.Show("There was an error while trying to save the file.\r\n" +
					exception.Message, "Error");
			}
		}
		protected override void OnFormClosing(FormClosingEventArgs args) {
			base.OnFormClosing(args);
			settings.FormLocation = Location;
			settings.FormSize = Size;
			settings.Save();
		}
		private static string openFileDialogFilter;
		static MainForm() {
			if(DbEntry.Names.Count > 0) {
				StringBuilder builder = new StringBuilder();
				foreach(string key in DbEntry.Names)
					builder.Append(key + "|" + key + "|");
				builder.Remove(builder.Length - 1, 1);
				openFileDialogFilter = builder.ToString();
			}
		}
		private IDbFile file = null;
		private string filePath = "";
		private PropertyGrid propertyGrid = new PropertyGrid();
		private SplitContainer splitContainer = new SplitContainer();
		private ListBox listBox = new ListBox();
		private static MainForm defaultInstance;
		public static MainForm Default { get { return defaultInstance; } }
		private Settings settings = new Settings();
		private class Settings : ApplicationSettingsBase {
			[UserScopedSetting()]
			[DefaultSettingValue("100, 100")]
			public Point FormLocation {
				get { return (Point)this["FormLocation"]; }
				set { this["FormLocation"] = value; }
			}
			[UserScopedSetting()]
			[DefaultSettingValue("400, 400")]
			public Size FormSize {
				get { return (Size)this["FormSize"]; }
				set { this["FormSize"] = value; }
			}
		}
	}
}