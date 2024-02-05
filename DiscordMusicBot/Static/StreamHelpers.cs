using Discord.Audio;
using System.Diagnostics;

namespace DiscordMusicBot.Static
{
    public static class StreamHelpers
    {
		public static async Task<Stream> ConvertToDiscordAudioFormat(Stream inputStream)
		{
			var outputMemoryStream = new MemoryStream();

			var processStartInfo = new ProcessStartInfo
			{
				FileName = "ffmpeg",
				Arguments = "-hide_banner -loglevel panic -i pipe:0 -ar 48000 -f s16le -acodec pcm_s16le pipe:1",
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};

			using (var process = new Process { StartInfo = processStartInfo })
			{
				process.Start();

				var inputTask = inputStream.CopyToAsync(process.StandardInput.BaseStream);
				var closeTask = inputTask.ContinueWith(task => process.StandardInput.Close());

				var outputTask = process.StandardOutput.BaseStream.CopyToAsync(outputMemoryStream);

				await Task.WhenAll(process.WaitForExitAsync(), inputTask, outputTask, closeTask);

				outputMemoryStream.Position = 0;

				return outputMemoryStream;
			}
		}

		public static async ValueTask StreamAudioToDiscordAsync(AudioOutStream pcmStream, Stream musicStream,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 4];
            int bytesRead;
			Console.WriteLine($"Streaming audio to discord.");

			while ((bytesRead = await musicStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await pcmStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
