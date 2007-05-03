using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace DbTool {
	class Program {
		[STAThread()]
		static void Main(string[] args) {
			Application.Run(new MainForm());
		}
	}
}