using System;
using System.Diagnostics;
using System.IO;
using Zorro.Recorder;

namespace TestUnityPlugin
{
    public class Conversor
    {
        public void EncodeVideo(string inputFilePath, string outputFolderPath, string fileName)
        {
            // Create a new directory with the name of the file
            string targetDirectory = Path.Combine(outputFolderPath, fileName);
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }
            // Set the output file path within the new directory
            string outputFile = Path.Combine(targetDirectory, "output.webm");

            string ffmpegPath = FfmpegEncoder.ExecutablePath;
            string arguments = $"-i \"{inputFilePath}\" " +
                   "-map 0:a -map 0:v " + // Ensure audio stream comes first
                   "-c:v libvpx -cpu-used -5 -deadline realtime -speed 1 -preset ultrafast " +
                   "-c:a libvorbis -ac 2 -ar 24000 " +
                   "-vf scale=420:420 " + // Scale the video
                   "-pix_fmt yuv420p " + // Set pixel format
                   $"\"{outputFile}\"";

            Process process = new Process();
            process.StartInfo.FileName = ffmpegPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);
            HelmetText.Instance.SetHelmetText("Starting Conversion", 1f);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
            if (process.ExitCode == 0) // Check if FFmpeg process was successful
            {
                try
                {
                    File.Delete(inputFilePath); // Delete the original file
                    HelmetText.Instance.SetHelmetText("Conversion complete, conversion folder cleaned up", 2.5f);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error deleting file: " + ex.Message);
                }
            }
            else
            {
                HelmetText.Instance.SetHelmetText("Error during conversion.", 2.5f);
            }
        }
        public void AnalyzeVideo(string inputFilePath, string outputFolderPath)
        {
            string ffprobePath = FfmpegEncoder.ExecutablePath.Replace("ffmpeg", "ffprobe");
            string jsonOutputPath = Path.Combine(outputFolderPath, "video_analysis.json");
            string arguments = $"-v quiet -print_format json -show_format -show_streams \"{inputFilePath}\"";

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffprobePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    RedirectStandardInput = false
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            File.WriteAllText(jsonOutputPath, output);
        }
    }
}
