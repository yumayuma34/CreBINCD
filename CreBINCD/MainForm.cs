using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreBINCD
{
    public partial class MainForm : Form
    {
        private LogForm logForm;

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
                    "FFmpegが見つかりません\n\n" +
                    "以下の方法などによってFFmpegを導入する必要があります：\n" +
                    "・ffmpeg.exeをCreBINCD.exeと同じフォルダに配置する\n" +
                    "・\"winget install --id=Gyan.FFmpeg -e\" を使用してインストールする",
                    "FFmpegが必要です",
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

            var indices = lstFiles.SelectedIndices.Cast<int>().OrderByDescending(i => i).ToArray();
            foreach (var idx in indices)
            {
                lstFiles.Items.RemoveAt(idx);
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
            var items = lstFiles.Items.Cast<object>().ToList();
            var sel = new bool[items.Count];
            var indices = lstFiles.SelectedIndices.Cast<int>().ToArray();
            if (indices.Length == 0) return;
            foreach (var i in indices) sel[i] = true;

            for (int i = 1; i < items.Count; i++)
            {
                if (sel[i] && !sel[i - 1])
                {
                    var tmp = items[i - 1];
                    items[i - 1] = items[i];
                    items[i] = tmp;
                    sel[i - 1] = true;
                    sel[i] = false;
                }
            }

            lstFiles.BeginUpdate();
            try
            {
                lstFiles.Items.Clear();
                foreach (var it in items) lstFiles.Items.Add(it);
                for (int i = 0; i < sel.Length; i++)
                {
                    lstFiles.SetSelected(i, sel[i]);
                }
            }
            finally
            {
                lstFiles.EndUpdate();
            }
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            var items = lstFiles.Items.Cast<object>().ToList();
            var sel = new bool[items.Count];
            var indices = lstFiles.SelectedIndices.Cast<int>().ToArray();
            if (indices.Length == 0) return;
            foreach (var i in indices) sel[i] = true;

            for (int i = items.Count - 2; i >= 0; i--)
            {
                if (sel[i] && !sel[i + 1])
                {
                    var tmp = items[i + 1];
                    items[i + 1] = items[i];
                    items[i] = tmp;
                    sel[i + 1] = true;
                    sel[i] = false;
                }
            }

            lstFiles.BeginUpdate();
            try
            {
                lstFiles.Items.Clear();
                foreach (var it in items) lstFiles.Items.Add(it);
                for (int i = 0; i < sel.Length; i++) lstFiles.SetSelected(i, sel[i]);
            }
            finally
            {
                lstFiles.EndUpdate();
            }
        }

        private async void btnBuild_Click(object sender, EventArgs e)
        {
            if (lstFiles.Items.Count == 0)
            {
                MessageBox.Show("1つもファイルが追加されていません");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPath.Text))
            {
                MessageBox.Show("ファイルの出力先を指定してください");
                return;
            }

            string binPath = txtPath.Text;
            string cuePath = Path.ChangeExtension(binPath, ".cue");

            var fileList = lstFiles.Items.Cast<object>().Select(o => o.ToString()).ToList();

            if (logForm == null || logForm.IsDisposed)
            {
                logForm = new LogForm();
            }
            logForm.IsProcessing = true;
            logForm.Show(this);

            bool success = true;
            string finishMessage = "BIN/CUEファイルの作成が完了しました。";

            try
            {
                var wavFiles = new List<string>();

                await Task.Run(() =>
                {
                    Action cleanupTempWavs = () =>
                    {
                        foreach (var w in wavFiles.ToList())
                        {
                            try
                            {
                                if (w != null && w.EndsWith(".tmp.wav", System.StringComparison.OrdinalIgnoreCase) && File.Exists(w))
                                {
                                    File.Delete(w);
                                    logForm.AppendLog($"削除: {w}\r\n");
                                }
                            }
                            catch (Exception ex)
                            {
                                logForm.AppendLog($"削除に失敗しました: {w} ({ex.Message})\r\n");
                            }
                        }
                    };

                    foreach (var path in fileList)
                    {
                        if (logForm.CancelRequested)
                        {
                            success = false;
                            finishMessage = "キャンセルされました";
                            cleanupTempWavs();
                            return;
                        }

                        string wav;
                        var ext = Path.GetExtension(path);
                        if (ext != null && ext.Equals(".wav", System.StringComparison.OrdinalIgnoreCase))
                        {
                            wav = path;
                            logForm.AppendLog($"WAVを使用: {path}\r\n");
                        }
                        else
                        {
                            logForm.AppendLog($"変換中: {path}\r\n");
                            wav = AudioConverter.ConvertToWav(path);
                            logForm.AppendLog($" → WAV: {wav}\r\n");
                        }
                        if (!wavFiles.Contains(wav))
                        {
                            wavFiles.Add(wav);
                        }
                    }

                    if (logForm.CancelRequested)
                    {
                        success = false;
                        finishMessage = "キャンセルされました";
                        cleanupTempWavs();
                        return;
                    }

                    try
                    {
                        logForm.AppendLog($"BIN: {binPath}\r\n");
                        logForm.AppendLog($"CUE: {cuePath}\r\n");
                        BinCueBuilder.Build(binPath, cuePath, wavFiles);
                        logForm.AppendLog("BIN/CUEファイルの作成が完了しました\r\n");
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        finishMessage = $"エラーが発生しました: {ex.Message}";
                        logForm.AppendLog($"エラー: {ex.Message}\r\n");
                        cleanupTempWavs();
                        return;
                    }

                    foreach (var wav in wavFiles)
                    {
                        if (logForm.CancelRequested)
                        {
                            success = false;
                            finishMessage = "キャンセルされました";
                            cleanupTempWavs();
                            return;
                        }

                        if (wav.EndsWith(".tmp.wav", System.StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                File.Delete(wav);
                                logForm.AppendLog($"削除: {wav}\r\n");
                            }
                            catch (Exception ex)
                            {
                                logForm.AppendLog($"削除に失敗しました: {wav} ({ex.Message})\r\n");
                            }
                        }
                    }
                });
            }
            finally
            {
                if (logForm != null && !logForm.IsDisposed)
                {
                    logForm.IsProcessing = false;
                }
            }

            MessageBox.Show(this, finishMessage, success ? "成功" : "キャンセル/失敗", MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (logForm != null && !logForm.IsDisposed)
            {
                logForm.Close();
            }
        }
    }
}