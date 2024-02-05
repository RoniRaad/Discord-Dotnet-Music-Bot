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
            var audioStreams = streamManifest.GetAudioOnlyStreams();
            var bestAudioStream = audioStreams.GetWithHighestBitrate();

            using (var inputMemoryStream = new MemoryStream())
            {
                using (var stream = await youtubeClient.Videos.Streams.GetAsync(bestAudioStream))
                {
                    await stream.CopyToAsync(inputMemoryStream);
                    stream.Close();
                    stream.Dispose();

                    inputMemoryStream.Position = 0;
					Console.WriteLine($"Youtube stream read. Size: {inputMemoryStream.Length}");

					return await StreamHelpers.ConvertToDiscordAudioFormat(inputMemoryStream);
                }
            }
        }
    }
}
