using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordMusicBot.Handlers;
using DiscordMusicBot.Models;
using DiscordMusicBot.Modules;

DiscordSocketClient _client;
Task Log(LogMessage msg)
{
	Console.WriteLine(msg.ToString());
	return Task.CompletedTask;
}

_client = new DiscordSocketClient(new DiscordSocketConfig()
{
  GatewayIntents = GatewayIntents.GuildVoiceStates | GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.GuildMessageTyping | GatewayIntents.MessageContent
});

_client.Log += Log;

var token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");

if (token is null)
{
	Console.Error.WriteLine("DISCORD_BOT_TOKEN environment variable is not set. Aborting");
	Environment.Exit(1);
}

await _client.LoginAsync(TokenType.Bot, token);
await _client.StartAsync();
_client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;

async Task _client_UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
{
	if (arg2.VoiceChannel is null || arg3.VoiceChannel is not null)
		return;

	var channel = (IVoiceChannel)arg2.VoiceChannel;
	var guild = channel?.Guild;
	if (guild is null)
		return;

	var guildId = guild.Id;

	if (MusicCommandModule.GuildStates.TryGetValue(guildId, out var guildState)
		&& guildState?.AudioState?.ChannelId is not null)
	{
		var userLists = await channel.GetUsersAsync().ToListAsync();
		var users = userLists?.FirstOrDefault()?.ToList();
		users?.RemoveAll(x => x.Id == arg1.Id);

		if (users?.Count != 1)
			return;

		guildState?.AudioState?.Client?.Dispose();
		MusicCommandModule.GuildStates.Remove(guildId);
	}
}

var commandService = new CommandService();
var commandHandler = new CommandHandler(_client, commandService);
await commandHandler.InstallCommandsAsync();

await Task.Delay(-1);