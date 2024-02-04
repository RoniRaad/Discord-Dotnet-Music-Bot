using AngleSharp.Dom;

namespace DiscordMusicBot.Modules
{
	public class SongMetadata
	{
        public string? Title { get; set; }
        public string? Author { get; set; }
        public Func<Task<Stream?>> GetStream { get; set; } = () => Task.FromResult<Stream?>(null);
    }
}