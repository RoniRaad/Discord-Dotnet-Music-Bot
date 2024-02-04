using AngleSharp.Dom;
using DiscordMusicBot.Modules;

namespace DiscordMusicBot.Models
{
    public class GuildState
    {
        public LocalAudioState? AudioState { get; set; }
        public Queue<SongMetadata> SongQueue { get; set; } = new();
        public SemaphoreSlim PlaySemaphore { get; set; } = new(1);
    }
}
