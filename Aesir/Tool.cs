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
		public enum DragMode { Pencil, Square, Line }
		public FloorTileTool() { }
		public override void Activate(MainForm mainForm) {
			mainForm.MapView.MouseMove += new MouseEventHandler(MapPanel_MouseMove);
			mainForm.MapView.MouseClick += new MouseEventHandler(MapPanel_MouseClick);
			mainForm.MapView.Paint += new PaintEventHandler(MapPanel_Paint);
		}
		public override void Deactivate(MainForm mainForm) {
			mainForm.MapView.MouseMove -= MapPanel_MouseMove;
			mainForm.MapView.MouseClick -= MapPanel_MouseClick;
			mainForm.MapView.Paint -= MapPanel_Paint;
		}
		private void MapPanel_Paint(object sender, PaintEventArgs args) { }
		private void MapPanel_MouseMove(object sender, MouseEventArgs args) { }
		void MapPanel_MouseClick(object sender, MouseEventArgs args) { }
	}
}