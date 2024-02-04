using SoundCloudExplode;

namespace DiscordMusicBot.Static
{
    public static class SoundCloudClientExtensions
    {
        public static async Task<Stream?> GetSoundCloudStream(this SoundCloudClient soundCloudClient, string url)
        {
            var downloadUrl = await soundCloudClient.Tracks.GetDownloadUrlAsync(url);

            if (downloadUrl is not null)
            {
				using var httpClient = new HttpClient();
				using var inputMemoryStream = new MemoryStream();
				using var stream = await httpClient.GetStreamAsync(downloadUrl);

				await stream.CopyToAsync(inputMemoryStream);
				stream.Close();
				stream.Dispose();

				inputMemoryStream.Position = 0;

				return await StreamHelpers.ConvertToDiscordAudioFormat(inputMemoryStream);
			}

            return new MemoryStream();
        }
    }
}
