using System;
using System.Windows.Forms;

namespace CreBINCD
{
    public partial class LogForm : Form
    {
        public LogForm()
        {
            InitializeComponent();
            this.FormClosing += LogForm_FormClosing;
        }

        public bool IsProcessing { get; set; }

        public bool CancelRequested { get; private set; }

        private void LogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsProcessing)
            {
                var result = MessageBox.Show(this, "Do you want to stop the process?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    CancelRequested = true;
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        public void AppendLog(string text)
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.Invoke((Action)(() => { if (!this.IsDisposed) txtLog.AppendText(text); }));
            }
            else
            {
                txtLog.AppendText(text);
            }
        }
    }
}
