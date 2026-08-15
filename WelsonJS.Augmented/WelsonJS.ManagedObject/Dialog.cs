using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WelsonJS.ManagedObject
{
    [ComVisible(true)]
    [Guid("ac1f8ba9-904a-434e-8a0a-9652a336a91b")]
    [ProgId("WelsonJS.Dialog")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class Dialog
    {
        public string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            Label textLabel = new Label() { Left = 50, Top = 20, Width = 400, Text = text };
            TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
            Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 70, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }

        public string OpenFileDialog(string filter = "All files (*.*)|*.*")
        {
            string filepath = string.Empty;

            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = filter;
                dialog.RestoreDirectory = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    filepath = dialog.FileName;
                }
            }

            return filepath;
        }

        public int Alert(string message, string title = "Dialog")
        {
            MessageBox.Show(message, title);

            return 0;
        }

        [ComVisible(true)]
        public bool Confirm(string message, string title = "Dialog")
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo) == DialogResult.Yes;
        }
    }
}
