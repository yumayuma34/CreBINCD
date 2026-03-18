using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace CreBINCD
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private bool IsFFmpegAvailable()
        {
            try
            {
                var p = new System.Diagnostics.Process();
                p.StartInfo.FileName = "ffmpeg";
                p.StartInfo.Arguments = "-version";
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();
                p.WaitForExit();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            string ffmpegLocal = Path.Combine(Application.StartupPath, "ffmpeg.exe");

            if (!File.Exists(ffmpegLocal) && !IsFFmpegAvailable())
            {
                MessageBox.Show(
                    "FFmpeg cannot be found\n\n" +
                    "Please do one of the following:\n" +
                    "・Place ffmpeg.exe in the same folder as CreBINCD.exe\n" +
                    "・Install using `winget install --id=Gyan.FFmpeg -e`",
                    "FFmpeg is required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                this.Close();
                return;
            }
        }

        private void btnAddFiles_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true;
                ofd.Filter = "Audio File|*.wav;*.mp3;*.flac;*.ogg;*.m4a;*.aac;*.wma|All File|*.*";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach (var f in ofd.FileNames)
                    {
                        lstFiles.Items.Add(f);
                    }
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItems.Count == 0)
                return;

            while (lstFiles.SelectedItems.Count > 0)
            {
                lstFiles.Items.Remove(lstFiles.SelectedItems[0]);
            }
        }

        private void btnBrowseOutput_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "BIN File|*.bin";
                sfd.FileName = "disc.bin";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = sfd.FileName;
                }
            }
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            int index = lstFiles.SelectedIndex;
            if (index > 0)
            {
                var item = lstFiles.Items[index];
                lstFiles.Items.RemoveAt(index);
                lstFiles.Items.Insert(index - 1, item);
                lstFiles.SelectedIndex = index - 1;
            }
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            int index = lstFiles.SelectedIndex;
            if (index >= 0 && index < lstFiles.Items.Count - 1)
            {
                var item = lstFiles.Items[index];
                lstFiles.Items.RemoveAt(index);
                lstFiles.Items.Insert(index + 1, item);
                lstFiles.SelectedIndex = index + 1;
            }
        }

        private void btnBuild_Click(object sender, EventArgs e)
        {
            if (lstFiles.Items.Count == 0)
            {
                MessageBox.Show("No files have been added");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPath.Text))
            {
                MessageBox.Show("Please specify the output destination");
                return;
            }

            string binPath = txtPath.Text;
            string cuePath = Path.ChangeExtension(binPath, ".cue");

            txtLog.AppendText($"BIN: {binPath}\r\n");
            txtLog.AppendText($"CUE: {cuePath}\r\n");

            var wavFiles = new List<string>();

            foreach (string path in lstFiles.Items)
            {
                txtLog.AppendText($"Converting: {path}\r\n");
                string wav = AudioConverter.ConvertToWav(path);
                wavFiles.Add(wav);
                txtLog.AppendText($" → WAV: {wav}\r\n");
            }

            BinCueBuilder.Build(binPath, cuePath, wavFiles);

            txtLog.AppendText("BIN/CUE creation complete\r\n");

            foreach (string wav in wavFiles)
            {
                if (wav.EndsWith(".tmp.wav", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Delete(wav);
                        txtLog.AppendText($"Delete: {wav}\r\n");
                    }
                    catch (Exception ex)
                    {
                        txtLog.AppendText($"Deletion failed: {wav} ({ex.Message})\r\n");
                    }
                }
            }

            MessageBox.Show("The creation of the BIN/CUE files is complete.");
        }
    }
}