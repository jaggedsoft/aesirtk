using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Aesir.Util {
	class ListViewHeader : NativeWindow {
		[DllImport("user32.dll")]
		private static extern IntPtr GetWindow(IntPtr windowHandle, uint command);
		private enum GetWindow_Command { Child = 5 };
		private enum Message_Msg { LButtonDown = 0x0020, SetCursor = 0x0201 };
		public ListViewHeader(ListView listView) {
			listView.HandleCreated += delegate(object sender, EventArgs args) {
				IntPtr handle = GetWindow(listView.Handle, (int)GetWindow_Command.Child);
				AssignHandle(handle);
			};
		}
		private bool allowResizeColumns = true;
		public bool AllowResizeColumns { set { allowResizeColumns = value; } }
		~ListViewHeader() { ReleaseHandle(); }
		protected override void WndProc(ref Message message) {
			if(!allowResizeColumns) {
				// Block WM_SETCURSOR and WM_LBUTTONDOWN messages
				if(message.Msg == (int)Message_Msg.LButtonDown ||
					message.Msg == (int)Message_Msg.SetCursor) message.Msg = 0;
			}
			base.WndProc(ref message);
		}
	}
}