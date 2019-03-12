﻿using System;
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
        private static Tuple<String, String>[] DEPENDENCIES = {
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
            btnAudio.Enabled = false;
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
            btnAudio.Enabled = true;
            btnUpdate.Enabled = true;
        }

        private void Download(String url, DownloadType type) {
            lblStatus.Text = "Downloading...";

            String destination = type == DownloadType.Video
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
                : Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            destination = Path.Combine(destination, "YouTube");

            Directory.CreateDirectory(destination);

            try {
                Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = "youtube-dl.exe";

                process.StartInfo.Arguments = tbURL.Text + " -o " + destination + "\\%(title)s.%(ext)s";

                if (type == DownloadType.Audio) {
                    process.StartInfo.Arguments += " -x --audio-format mp3";
                }

                process.StartInfo.CreateNoWindow = true;

                process.EnableRaisingEvents = true;
                process.Exited += (s, e) => {
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

        private void tbURL_Click(object sender, EventArgs ea) {
            (sender as TextBox).SelectAll();
        }

        private void btnUpdate_Click(object sender, EventArgs e) {
            DownloadDependencies(true);
        }

        private void btnAudio_Click(object sender, EventArgs ea) {
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
