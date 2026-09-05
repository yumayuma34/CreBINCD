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
                var result = MessageBox.Show(this, "変換を中止しますか？", "中止", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
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
