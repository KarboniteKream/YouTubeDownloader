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
    public partial class Form1 : Form {
        private Tuple<String, String>[] DEPENDENCIES = {
            new Tuple<String, String>("youtube-dl.exe", "https://yt-dl.org/downloads/latest/youtube-dl.exe"),
            new Tuple<String, String>("ffmpeg.exe", "https://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-latest-win64-static.zip"),
        };

        public Form1() {
            InitializeComponent();
        }

        private void Form1_Shown(object sender, EventArgs ea) {
            DownloadDependencies(false);
        }

        private async void DownloadDependencies(bool force) {
            List<Tuple<String, String>> dependencies = DEPENDENCIES
                .Where(dependency => force || !File.Exists(dependency.Item1))
                .ToList();

            if (dependencies.Count == 0) {
                return;
            }

            btnUpdate.Enabled = false;
            btnDownload.Enabled = false;
            lblStatus.Text = "Downloading dependencies...";
            pbDownload.Maximum = dependencies.Count() * 100;
            pbDownload.Visible = true;

            IEnumerable<Task> tasks = dependencies.Select(dependency => {
                String url = dependency.Item2;
                String destination = url.Split('/').Last();

                WebClient client = new WebClient();
                int progress = 0;

                client.DownloadProgressChanged += (s, e) =>
                {
                    pbDownload.Value += e.ProgressPercentage - progress;
                    progress = e.ProgressPercentage;
                };

                return client.DownloadFileTaskAsync(new Uri(url), destination);
            });

            await Task.WhenAll(tasks);

            pbDownload.Visible = false;
            lblStatus.Text = "Installing dependencies...";

            foreach (Tuple<String, String> dependency in dependencies) {
                String filename = dependency.Item1;
                String archive = dependency.Item2.Split('/').Last();

                if (!archive.EndsWith(".zip")) {
                    continue;
                }

                using (ZipArchive zip = ZipFile.OpenRead(archive)) {
                    ZipArchiveEntry entry = zip.Entries.Where(e => e.Name == filename).First();
                    entry.ExtractToFile(filename, true);
                }

                File.Delete(archive);
            }

            lblStatus.Text = "Ready.";
            btnDownload.Enabled = true;
            btnUpdate.Enabled = true;
        }

        private void tbURL_Click(object sender, EventArgs e) {
            (sender as TextBox).SelectAll();
        }

        private void btnUpdate_Click(object sender, EventArgs e) {
            DownloadDependencies(true);
        }

        private void btnDownload_Click(object sender, EventArgs e) {
            if (tbURL.Text == "") {
                return;
            }

            lblStatus.Text = "Downloading...";

            String destination = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            // TODO: Create YouTube folder.

            try {
                Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = "youtube-dl.exe";
                process.StartInfo.Arguments = "-f bestaudio -x --audio-format mp3 " + tbURL.Text + " -o " + destination + "\\YouTube\\%(title)s.%(ext)s";
                process.StartInfo.CreateNoWindow = true;

                process.EnableRaisingEvents = true;
                process.Exited += (_sender, _e) => {
                    if (process.ExitCode == 0) {
                        lblStatus.Text = "Ready.";
                        return;
                    }

                    lblStatus.Text = "Error!";
                };

                process.Start();
                process.WaitForExit();
            } catch (Exception ex) {
                lblStatus.Text = "Error!";
                MessageBox.Show(ex.Message);
            }
        }
    }
}
