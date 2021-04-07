using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YouTubeDownloader {
    enum DownloadType {
        Audio,
        Video,
    }

    public partial class Form1 : Form {
        private static Tuple<string[], string>[] DEPENDENCIES = {
            new Tuple<string[], string>(
                new string[]{ "youtube-dl.exe" },
                "https://youtube-dl.org/downloads/latest/youtube-dl.exe"),
            new Tuple<string[], string>(
                new string[]{ "ffmpeg.exe", "ffprobe.exe" },
                "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"),
        };

        public Form1() {
            InitializeComponent();
        }

        private void Form1_Shown(object sender, EventArgs e) {
            DownloadDependencies(false);
        }

        private async void DownloadDependencies(bool force) {
            Tuple<string[], string>[] dependencies = DEPENDENCIES
                .Where(dependency => force || dependency.Item1.Any(filename => !File.Exists(filename)))
                .ToArray();

            if (dependencies.Length == 0) {
                return;
            }

            btnUpdate.Enabled = false;
            btnVideo.Enabled = false;
            btnAudio.Enabled = false;
            lblStatus.Text = "Downloading dependencies...";
            pbDownload.Maximum = dependencies.Count() * 100;
            pbDownload.Visible = true;

            IEnumerable<Task> tasks = dependencies.Select(dependency => {
                string url = dependency.Item2;
                string destination = url.Split('/').Last();

                WebClient client = new WebClient();
                int progress = 0;

                client.DownloadProgressChanged += (s, e) => {
                    pbDownload.Value += e.ProgressPercentage - progress;
                    progress = e.ProgressPercentage;
                };

                return client.DownloadFileTaskAsync(new Uri(url), destination);
            });

            await Task.WhenAll(tasks);

            pbDownload.Visible = false;
            lblStatus.Text = "Installing dependencies...";

            foreach (Tuple<string[], string> dependency in dependencies) {
                string[] files = dependency.Item1;
                string archive = dependency.Item2.Split('/').Last();

                if (!archive.EndsWith(".zip")) {
                    continue;
                }

                using (ZipArchive zip = ZipFile.OpenRead(archive)) {
                    foreach (ZipArchiveEntry entry in zip.Entries.Where(e => files.Contains(e.Name))) {
                        entry.ExtractToFile(entry.Name, true);
                    }
                }

                File.Delete(archive);
            }

            lblStatus.Text = "Ready.";
            btnVideo.Enabled = true;
            btnAudio.Enabled = true;
            btnUpdate.Enabled = true;
        }

        private void Download(string url, DownloadType type) {
            string height = "1080";
            string ext = "mp4";

            lblStatus.Text = "Downloading...";
            btnUpdate.Enabled = false;
            btnVideo.Enabled = false;
            btnAudio.Enabled = false;
            rtbConsole.Clear();

            Environment.SpecialFolder folder =
                type == DownloadType.Video
                ? Environment.SpecialFolder.MyVideos
                : Environment.SpecialFolder.MyMusic;

            string destination = Path.Combine(Environment.GetFolderPath(folder), "YouTube");
            Directory.CreateDirectory(destination);

            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.FileName = "youtube-dl.exe";
            process.StartInfo.Arguments = url + " -o " + destination + "\\%(title)s.%(ext)s";

            if (type == DownloadType.Audio) {
                process.StartInfo.Arguments += " -x --audio-format mp3";
            } else if (type == DownloadType.Video) {
                process.StartInfo.Arguments += $" -f bestvideo[height<={height}][ext={ext}]+bestaudio --postprocessor-args \"-acodec mp3 -vcodec copy\"";
            }

            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (s, e) => rtbConsole_Append(e.Data);

            process.StartInfo.RedirectStandardError = true;
            process.ErrorDataReceived += (s, e) => rtbConsole_Append(e.Data);

            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => {
                Invoke(new Action(() => {
                    btnUpdate.Enabled = true;
                    btnVideo.Enabled = true;
                    btnAudio.Enabled = true;

                    rtbConsole.AppendText("Done.");
                    lblStatus.Text = process.ExitCode == 0 ? "Ready." : "Error!";
                }));
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        private void rtbConsole_Append(String text) {
            if (!String.IsNullOrEmpty(text)) {
                Invoke(new Action(() => rtbConsole.AppendText(text + '\n')));
            }
        }

        private void tbURL_Click(object sender, EventArgs e) {
            (sender as TextBox).SelectAll();
        }

        private void btnUpdate_Click(object sender, EventArgs e) {
            DownloadDependencies(true);
        }

        private void btnAudio_Click(object sender, EventArgs e) {
            if (tbURL.Text == "") {
                return;
            }

            Download(tbURL.Text, DownloadType.Audio);
        }

        private void btnVideo_Click(object sender, EventArgs e) {
            if (tbURL.Text == "") {
                return;
            }

            Download(tbURL.Text, DownloadType.Video);
        }
    }
}
