using Discord.Audio;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using FFMpegCore;
using SoundCloudExplode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace DiscordMusicBot.Static
{
    public static class StreamHelpers
    {
        public static async Task<Stream> ConvertToDiscordAudioFormat(Stream inputStream)
        {
            var outputMemoryStream = new MemoryStream();

            await FFMpegArguments.FromPipeInput(new StreamPipeSource(inputStream))
            .OutputToPipe(new StreamPipeSink(outputMemoryStream), options => options
                .WithCustomArgument("-hide_banner")
                .WithAudioSamplingRate(48000)
                .WithCustomArgument("-f s16le")
                .ForceFormat("s16le"))
            .WithLogLevel(FFMpegLogLevel.Panic)
            .ProcessAsynchronously();

            outputMemoryStream.Position = 0;

			Console.WriteLine($"Converted to discord audio format. Size: {outputMemoryStream.Length}");

			return outputMemoryStream;
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
