using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace DiscordMusicBot.Static
{
    public static class YoutubeClientExtensions
    {
		public static async Task<Stream> GetYoutubeStream(this YoutubeClient youtubeClient, string url)
        {
			Console.WriteLine($"Getting youtube stream. Url: {url}");

			var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(url);
			Console.WriteLine($"Got youtube stream manifest. Streams: {streamManifest.Streams.Count}");

			var audioStreams = streamManifest.GetAudioOnlyStreams();
			Console.WriteLine($"Got youtube audio streams. Streams: {streamManifest.Streams.Count}");

			var bestAudioStream = audioStreams.GetWithHighestBitrate();
			Console.WriteLine($"Got best audio stream Size: {bestAudioStream.Size}, Bitrate: {bestAudioStream.Bitrate}");

			const int bufferSize = 81920;
			var buffer = new byte[bufferSize];

			using (var stream = await youtubeClient.Videos.Streams.GetAsync(bestAudioStream))
			using (var inputMemoryStream = new MemoryStream())
			{
				int bytesRead;

				Console.WriteLine($"Downloading data from youtube stream...");
				while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
				{
					await inputMemoryStream.WriteAsync(buffer, 0, bytesRead);
				}

				inputMemoryStream.Position = 0;
				Console.WriteLine($"Youtube stream read. Size: {inputMemoryStream.Length}");

				return await StreamHelpers.ConvertToDiscordAudioFormat(inputMemoryStream);
			}
		}
    }
}
