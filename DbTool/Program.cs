using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace DbTool {
	class Program {
		[STAThread()]
		static void Main(string[] args) {
			DbEntries.Init();
			Application.Run(new MainForm());
		}
	}
}