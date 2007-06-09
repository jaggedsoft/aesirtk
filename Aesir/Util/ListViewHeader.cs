using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Aesir.Util {
	// Code adapted from http://www.codeproject.com/cs/miscctrl/myListViewNoSize.asp by Chris Morgan
	class ListViewHeader : NativeWindow {
		[DllImport("user32.dll")]
		private static extern IntPtr GetWindow(IntPtr windowHandle, uint command);
		private const int GetWindow_Child = 5;
		private const int Msg_LButtonDown = 0x0020, Msg_SetCursor = 0x0201;
		public ListViewHeader(ListView listView) {
			listView.HandleCreated += delegate(object sender, EventArgs args) {
				IntPtr handle = GetWindow(listView.Handle, GetWindow_Child);
				AssignHandle(handle);
			};
		}
		private bool allowResizeColumns = true;
		public bool AllowResizeColumns {
			set { allowResizeColumns = value; }
			get { return allowResizeColumns; }
		}
		~ListViewHeader() { ReleaseHandle(); }
		protected override void WndProc(ref Message message) {
			if(!allowResizeColumns) {
				// Block WM_SETCURSOR and WM_LBUTTONDOWN messages
				if(message.Msg == (int)Msg_LButtonDown ||
					message.Msg == (int)Msg_SetCursor) message.Msg = 0;
			}
			base.WndProc(ref message);
		}
	}
}