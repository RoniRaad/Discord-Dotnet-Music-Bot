using AngleSharp.Dom;
using Discord;
using Discord.Audio;
using Discord.Commands;
using DiscordMusicBot.Models;
using DiscordMusicBot.Static;
using SoundCloudExplode;
using SoundCloudExplode.Search;
using System.Text;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace DiscordMusicBot.Modules
{
    public class MusicCommandModule : ModuleBase<SocketCommandContext>
    {
        public static Dictionary<ulong, GuildState> GuildStates = new();

        // Make these static to prevent socket exhaustion
        private static readonly YoutubeClient _youtubeClient = new YoutubeClient();
        private static readonly SoundCloudClient _soundCloudClient = new SoundCloudClient();

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Plays a youtube or soundcloud link in your channel.")]
        public async Task PlayCommand([Remainder][Summary("The URL to play or search term")] string songInput)
        {
            Console.WriteLine($"Recieved play command from user. Input: {songInput}");
            var channel = (Context.User as IGuildUser)?.VoiceChannel;
            var guild = channel?.Guild;

            if (channel is null || guild is null) { return; }

            if (!GuildStates.TryGetValue(guild.Id, out var guildState))
            {
                guildState = new();
                GuildStates.Add(guild.Id, guildState);
            }

            try
            {
                var queuedUrl = await AddSongToQueue(guildState, songInput);
                var currentQueueCount = guildState.SongQueue.Count;
                if (guildState.AudioState?.StreamCancellationTokenSource is not null)
                    currentQueueCount++;

                var responseMessage = BuildPlayResponseMessage(queuedUrl, currentQueueCount);

                await Context.Channel.SendMessageAsync(responseMessage);

                await PlaySong(guildState, channel);
            }
            catch { }
        }

        [Command("stop")]
        [Summary("Skips the currently playing song.")]
        public Task StopCommand()
        {
            var channel = (Context.User as IGuildUser)?.VoiceChannel;
            var guild = channel?.Guild;

            if (channel is null || guild is null) { return Task.CompletedTask; }

            if (GuildStates.TryGetValue(guild.Id, out var guildState))
            {
                var cancellationToken = guildState?.AudioState?.StreamCancellationTokenSource;

                cancellationToken?.Cancel();
            }

			return Task.CompletedTask;
		}

		[Command("remove")]
		[Summary("Removes the number from the queue.")]
		public Task RemoveCommand(int songNumber)
		{
			var channel = (Context.User as IGuildUser)?.VoiceChannel;
			var guild = channel?.Guild;

			if (channel is null || guild is null) { return Task.CompletedTask; }

			if (GuildStates.TryGetValue(guild.Id, out var guildState))
			{
                var removeIndex = songNumber - 1;
                if (guildState.SongQueue.Count < removeIndex || removeIndex < 0)
                    return Task.CompletedTask;

                var newQueue = new Queue<SongMetadata>();
                var songQueueList = guildState.SongQueue.ToList();
                songQueueList.RemoveAt(removeIndex);
                songQueueList.ForEach(newQueue.Enqueue);
				guildState.SongQueue = newQueue;
			}

			return Task.CompletedTask;
		}

		[Command("queue", RunMode = RunMode.Async)]
		[Summary("Gets the current queue and all its songs.")]
		public async Task GetQueueCommand()
		{
            const string lineFormat = "{0}. {1} by {2}";
			var channel = (Context.User as IGuildUser)?.VoiceChannel;
			var guild = channel?.Guild;

			if (channel is null || guild is null) { return; }

			if (GuildStates.TryGetValue(guild.Id, out var guildState))
			{
                var queueItems = guildState.SongQueue.ToList();
                var responseMessageBuilder = new StringBuilder();

                for ( var i = 0; i < queueItems.Count; i++)
                {
                    var formattedString = string.Format(lineFormat, i, queueItems[i].Title, queueItems[i].Author);
					responseMessageBuilder.AppendLine(formattedString);
				}

				await Context.Channel.SendMessageAsync(responseMessageBuilder.ToString(), flags: MessageFlags.SuppressEmbeds);
			}

			return;
		}

		[Command("skip")]
        [Summary("Skips the currently playing song.")]
        public async Task SkipCommand()
        {
            await StopCommand();
        }

		[Command("search", RunMode = RunMode.Async)]
		[Summary("Searches youtube for videos.")]
		public async Task SearchCommand([Remainder][Summary("The search term")] string searchTerm)
		{
            var videos = await _youtubeClient.Search.GetResultsAsync(searchTerm)
                .Take(5)
                .ToListAsync();

            var responseBuilder = new StringBuilder();
            videos.ForEach(x =>
            {
				responseBuilder.AppendLine($"{x.Title}");
				responseBuilder.AppendLine($"!play {x.Url}");
				responseBuilder.AppendLine("");
			});

			await Context.Channel.SendMessageAsync(responseBuilder.ToString(), flags: MessageFlags.SuppressEmbeds);
		}

		[Command("searchsoundcloud", RunMode = RunMode.Async)]
		[Summary("Searches soundcloud for songs.")]
		public async Task SearchSoundCloudCommand([Remainder][Summary("The search term")] string searchTerm)
		{
            var tracks = await _soundCloudClient.Search.GetTracksAsync(searchTerm, limit: 5, offset: 0)
                .Take(5)
                .ToListAsync();

			var responseBuilder = new StringBuilder();
			tracks.ForEach(x =>
			{
				responseBuilder.AppendLine($"{x.Title}");
				responseBuilder.AppendLine($"!play {x.Url}");
				responseBuilder.AppendLine("");
			});

			await Context.Channel.SendMessageAsync(responseBuilder.ToString(), flags: MessageFlags.SuppressEmbeds);
		}

		public async Task PlaySong(GuildState guildState, IVoiceChannel channel)
        {
            // This semaphore ensures that we play only one song at a time.
            await guildState.PlaySemaphore.WaitAsync();
            try
            {
                IAudioClient audioClient;

                if (guildState.AudioState is not null
                    && guildState.AudioState.ChannelId == channel.Id
                    && guildState.AudioState.Client.ConnectionState == ConnectionState.Connected)
                {
                    audioClient = guildState.AudioState.Client;
                }
                else
                {
                    audioClient = await channel.ConnectAsync();
                    guildState.AudioState = new LocalAudioState(audioClient, channel.Id);
                }

                using (var pcmStream = audioClient.CreatePCMStream(AudioApplication.Music))
                {
                    var songMetadata = guildState.SongQueue.Dequeue();

                    try
                    {
                        var cancellationTokenSource = new CancellationTokenSource();
                        guildState.AudioState.StreamCancellationTokenSource = cancellationTokenSource;

						using (var stream = await songMetadata.GetStream())
                        {
                            if (stream is not null)
							    await StreamHelpers.StreamAudioToDiscordAsync(pcmStream, stream, cancellationTokenSource.Token);
						}
					}
                    catch (OperationCanceledException)
                    {
                        await pcmStream.FlushAsync();
                    }
                    finally
                    {
                        guildState.AudioState.StreamCancellationTokenSource = null;
                    }

                    await pcmStream.FlushAsync();
                }
            }
            finally
            {
                guildState.PlaySemaphore.Release();
            }
        }

        public async Task<SongMetadata> AddSongToQueue(GuildState guildState, string songInput)
        {
			Console.WriteLine($"Added song to queue. SongInput: {songInput}");

			var url = new Url(songInput);
            SongMetadata songMetadata;

            if (!string.IsNullOrEmpty(url.Host) || url.IsInvalid)
            {
                songMetadata = await GetSongMetadata(url);

				guildState.SongQueue.Enqueue(songMetadata);
                return songMetadata;
			}

            var searchResults = _youtubeClient.Search.GetResultsAsync(songInput);
            var firstResult =  await searchResults.FirstAsync();
            url = new Url(firstResult.Url);
			songMetadata = await GetSongMetadata(url);

			guildState.SongQueue.Enqueue(songMetadata);
			
            return songMetadata;
		}

		public async Task<SongMetadata> GetSongMetadata(Url url)
		{
			var metadata = new SongMetadata();
			switch (url.Host)
			{
				case "www.youtube.com":
				case "youtube.com":
                case "youtu.be":
                case "m.youtube.com":
					var videoInfo = await _youtubeClient.Videos.GetAsync(url.ToString());
					metadata.Author = videoInfo?.Author?.ChannelTitle;
					metadata.Title = videoInfo?.Title;
					metadata.GetStream = async () => await _youtubeClient.GetYoutubeStream(url.ToString());
					break;
				case "www.soundcloud.com":
				case "soundcloud.com":
					var audioInfo = await _soundCloudClient.Tracks.GetAsync(url.ToString());
					metadata.Author = audioInfo?.LabelName?.ToString();
					metadata.Title = audioInfo?.Title;
					metadata.GetStream = async () => await _soundCloudClient.GetSoundCloudStream(url.ToString());
					break;
			}

			return metadata;
		}

		private string BuildPlayResponseMessage(SongMetadata queuedUrl, int currentQueueCount)
		{
			StringBuilder messageBuilder = new();

			if (queuedUrl.Title is not null)
			{
				if (currentQueueCount > 1)
					messageBuilder.Append($"Added {queuedUrl.Title}");
                else
					messageBuilder.Append($"Playing {queuedUrl.Title} by");

				if (queuedUrl.Author is not null)
				{
					messageBuilder.Append($" by {queuedUrl.Author}");
				}

				if (currentQueueCount > 1)
					messageBuilder.Append($" to Queue ({currentQueueCount - 1})");
			}
			else
			{
				messageBuilder.Append($"Added song to queue ({currentQueueCount - 1})");
			}

			return messageBuilder.ToString();
		}
	}
}