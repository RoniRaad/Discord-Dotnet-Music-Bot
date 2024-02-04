using Discord.Audio;

namespace DiscordMusicBot.Models
{
    public class LocalAudioState
    {
        public LocalAudioState(IAudioClient client, ulong channelId)
        {
            Client = client;
            ChannelId = channelId;
        }

        public IAudioClient Client { get; set; }
        public CancellationTokenSource? StreamCancellationTokenSource { get; set; }
        public ulong ChannelId { get; set; }
    }
}
