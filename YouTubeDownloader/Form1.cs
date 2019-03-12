using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace YouTubeDownloader {
    public partial class Form1 : Form {
        // TODO: Download ffmpeg.
        private String[] dependencies = {
            "https://yt-dl.org/downloads/latest/youtube-dl.exe",
        };

        public Form1() {
            InitializeComponent();
        }

        private void Form1_Shown(object sender, EventArgs e) {
            WebClient client = new WebClient();

            foreach (String url in dependencies) {
                String filename = url.Split('/').Last();

                if (File.Exists(filename) == true) {
                    continue;
                }

                // TODO: Progress bar and completed.
                client.DownloadFileAsync(new Uri(url), filename);
                pbDownload.Visible = true;
            }
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            pbDownload.Value = e.ProgressPercentage;
        }

        private void DownloadFileCompleted(object sender, EventArgs e) {
            pbDownload.Visible = false;
            pbDownload.Value = 0;
            lblStatus.Text = "Ready.";
        }

        private void tbURL_Click(object sender, EventArgs e) {
            (sender as TextBox).SelectAll();
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

        private void btnUpdate_Click(object sender, EventArgs e) {
            lblStatus.Text = "Updating...";

            try {
                Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = "youtube-dl.exe";
                process.StartInfo.Arguments = "-U";
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
