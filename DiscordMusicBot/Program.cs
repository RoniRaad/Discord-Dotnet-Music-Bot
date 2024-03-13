using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordMusicBot.Handlers;
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

Task _client_UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
{
	var userLeftChannel = arg3.VoiceChannel is null;
	var channelIsNowEmpty = arg2.VoiceChannel?.ConnectedUsers.Count(x => !x.IsBot && x.Id != arg1.Id) == 0;

	if (!userLeftChannel
		|| !channelIsNowEmpty
		|| !MusicCommandModule.GuildStates.TryGetValue(arg2.VoiceChannel.Guild.Id, out var guildState)
		|| guildState?.AudioState?.ChannelId is null
	)
		return Task.CompletedTask;

	guildState?.AudioState?.Client?.Dispose();
	MusicCommandModule.GuildStates.Remove(arg2.VoiceChannel.Guild.Id);

	return Task.CompletedTask;
}

var commandService = new CommandService();
var commandHandler = new CommandHandler(_client, commandService);
await commandHandler.InstallCommandsAsync();

await Task.Delay(-1);