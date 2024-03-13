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

			var stream = await youtubeClient.Videos.Streams.GetAsync(bestAudioStream);

			return await StreamHelpers.ConvertToDiscordAudioFormat(stream);
		}
    }
}
