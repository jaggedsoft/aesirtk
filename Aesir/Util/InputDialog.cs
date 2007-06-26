using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace Aesir.Util {
	class InputDialog : Form {
		public InputDialog(string prompt) {
			this.prompt = prompt;
			promptLabel.Text = prompt;
			Controls.Add(promptLabel);
			Controls.Add(inputTextBox);
			acceptButton.Text = "OK";
			acceptButton.Click += delegate(object sender, EventArgs args) {
				DialogResult = DialogResult.OK;
				input = inputTextBox.Text;
				Close();
			};
			AcceptButton = acceptButton;
			Controls.Add(acceptButton);
			cancelButton.Text = "Cancel";
			cancelButton.Click += delegate(object sender, EventArgs args) {
				Close();
			};
			CancelButton = cancelButton;
			Controls.Add(cancelButton);
			Size = new Size(294, 124);
			promptLabel.Location = new Point(horizontalPadding, 15);
			promptLabel.Height = 13;
			inputTextBox.Location = new Point(horizontalPadding, 35);
			promptLabel.Width = inputTextBox.Width = Width - horizontalPadding * 2;
			acceptButton.Location = new Point(65, 65);
			cancelButton.Location = new Point(150, 65);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = MinimizeBox = false;
			Text = "Prompt";
			ShowInTaskbar = false;
			DialogResult = DialogResult.Cancel;
			StartPosition = FormStartPosition.CenterParent;
		}
		private const int horizontalPadding = 15;
		private TextBox inputTextBox = new TextBox();
		private Button acceptButton = new Button(), cancelButton = new Button();
		private Label promptLabel = new Label();
		public new DialogResult ShowDialog(IWin32Window owner) {
			base.ShowDialog(owner);
			return DialogResult;
		}
		private string input = string.Empty, prompt;
		public string Input {
			get { return input; }
		}
		public string Prompt {
			get { return prompt; }
		}
	}
}