using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Aesir.Nexus;

namespace Aesir {
	abstract class Tool {
		public abstract void Activate(MainForm mainForm);
		public abstract void Deactivate(MainForm mainForm);
	}
	class FloorTileTool : Tool {
		public enum DragMode { None, Square, Line }
		public FloorTileTool() { }
		public override void Activate(MainForm mainForm) {
			mainForm.MapPanel.MouseMove += new MouseEventHandler(MapPanel_MouseMove);
			mainForm.MapPanel.MouseClick += new MouseEventHandler(MapPanel_MouseClick);
			mainForm.MapPanel.Paint += new PaintEventHandler(MapPanel_Paint);
		}
		public override void Deactivate(MainForm mainForm) {
			mainForm.MapPanel.MouseMove -= MapPanel_MouseMove;
			mainForm.MapPanel.MouseClick -= MapPanel_MouseClick;
			mainForm.MapPanel.Paint -= MapPanel_Paint;
		}
		private void MapPanel_Paint(object sender, PaintEventArgs args) { }
		private void MapPanel_MouseMove(object sender, MouseEventArgs args) { }
		void MapPanel_MouseClick(object sender, MouseEventArgs args) { }
	}
}