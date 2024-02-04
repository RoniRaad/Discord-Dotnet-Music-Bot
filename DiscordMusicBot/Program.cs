using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordMusicBot.Handlers;

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
var commandService = new CommandService();
var commandHandler = new CommandHandler(_client, commandService);
await commandHandler.InstallCommandsAsync();

await Task.Delay(-1);