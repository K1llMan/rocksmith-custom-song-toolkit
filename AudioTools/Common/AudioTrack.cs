using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using ATL;

using AudioTools.Utils.Lufs;

namespace AudioTools.Common
{
    public class AudioTrack
    {
        #region Consts

        private const int PREVIEW_LENGTH = 30;

        #endregion Consts

        #region Properties

        public AudioMetadata Metadata { get; internal set; }

        public string FileName { get; internal set; }

        public int Duration { get; internal set; }

        public int Bitrate { get; internal set; }

        public int SampleRate { get; internal set; }

        public double IntegratedLoudness { get; set; }

        #endregion Properties

        #region Auxiliary functions

        private bool IsFFMpegAvailiable()
        {
            bool found = File.Exists("ffmpeg.exe");
            Console.WriteLine("\"ffmpeg.exe\" not found.");
            return found;
        }

        private Process GetFFMpegProcess(string args)
        {
            return new Process {
                StartInfo = {
                    FileName = "ffmpeg.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
        }

        private Stream GetWavStream(string fileName, DataReceivedEventHandler outputDataReceivedHandler = null, 
            DataReceivedEventHandler errorDataReceivedHandler = null, EventHandler exitedHandler = null)
        {
            Process process = GetFFMpegProcess($"-i \"{fileName}\" -acodec pcm_s32le -f wav -");
            if (process == null)
                return null;

            if (outputDataReceivedHandler != null)
            {
                process.OutputDataReceived += outputDataReceivedHandler;
            }

            if (errorDataReceivedHandler != null)
            {
                process.ErrorDataReceived += errorDataReceivedHandler;
            }

            if (exitedHandler != null)
            {
                process.EnableRaisingEvents = true;
                process.Exited += exitedHandler;
            }

            process.Start();

            if (outputDataReceivedHandler != null)
            {
                process.BeginOutputReadLine();
            }

            if (errorDataReceivedHandler != null)
            {
                process.BeginErrorReadLine();
            }

            return process.StandardOutput.BaseStream;
        }

        private double[][] GetFileData()
        {
            if (string.IsNullOrEmpty(FileName))
                return null;

            WavReader wavReader;
            if (Path.GetExtension(FileName).ToLower() == ".wav") {
                using (FileStream fileStream = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    wavReader = new WavReader(fileStream, Encoding.Default);
            }
            else
            {
                wavReader = new WavReader(GetWavStream(FileName), Encoding.Default);
            }

            return wavReader.GetSampleData();
        }

        private double UpdateLoudness(Action<double, double> progressUpdated = null)
        {
            if (!IsFFMpegAvailiable())
                return -9;

            double[][] data = GetFileData();

            R128LufsMeter r128LufsMeter = new R128LufsMeter();
            r128LufsMeter.Prepare(SampleRate, data.Length);
            Console.Write("Calculating input loudness...");
            r128LufsMeter.StartIntegrated();
            r128LufsMeter.ProcessBuffer(data, progressUpdated);
            r128LufsMeter.StopIntegrated();

            return r128LufsMeter.IntegratedLoudness;
        }

        #endregion Auxiliary functions

        #region Main functions

        public AudioTrack(string fileName, bool calcLoudness = false, Action<double, double> progressUpdated = null)
        {
            FileName = fileName;

            Track trackInfo = new Track(fileName);
            Duration = trackInfo.Duration;
            Bitrate = trackInfo.Bitrate;
            SampleRate = Convert.ToInt32(trackInfo.SampleRate);

            Metadata = new AudioMetadata {
                Album = trackInfo.Album,
                Artist = trackInfo.Artist,
                Title = trackInfo.Title,
                Year = trackInfo.Year
            };

            if (calcLoudness)
                IntegratedLoudness = UpdateLoudness(progressUpdated);
        }

        public void CalculateLoudness(Action<double, double> progressUpdated = null)
        {
            IntegratedLoudness = UpdateLoudness(progressUpdated);
        }

        public void SavePreview(string fileName, int from = 0)
        {
            if (!IsFFMpegAvailiable())
                return;

            Process process = GetFFMpegProcess($"-ss {Math.Min(from, Math.Max(Duration - PREVIEW_LENGTH, 0))} " +
                $"-t {Math.Min(Duration, PREVIEW_LENGTH)} -i \"{FileName}\" -acodec pcm_s32le -f wav \"{fileName.Replace(Path.GetExtension(fileName), ".wav")}\"");

            process.Start();
        }

        public void SaveMetadata()
        {
            Track trackInfo = new Track(FileName) {
                Artist = Metadata.Artist,
                Album = Metadata.Album,
                Title = Metadata.Title,
                Year = Metadata.Year
            };

            trackInfo.Save();
        }

        #endregion Main functions
    }
}
