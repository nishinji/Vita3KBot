using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Vita3KBot.Services;

namespace Vita3KBot {
    public class Bot {
        private readonly string _token;

        // Initializes Discord.Net
        private async Task Start() {
            using (var services = ConfigureServices()) {

                var client = services.GetRequiredService<DiscordSocketClient>();
                client.Log += LogAsync;
                services.GetRequiredService<CommandService>().Log += LogAsync;

                await client.LoginAsync(TokenType.Bot, _token);
                await client.StartAsync();

                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
                services.GetRequiredService<MessageHandlingService>().Initialize();

                await Task.Delay(Timeout.Infinite);
            }
        }

        private Bot(string token)
        {
          _token = token;
        }

        public static void Main(string[] args)
        {
            // Init command with token.
            if (args.Length >= 2 && args[0] == "init")
            {
              File.WriteAllText("token.txt", args[1]);
              Console.WriteLine("Token saved to token.txt");
              return;
            }

            try
            {
              string? token = Environment.GetEnvironmentVariable("TOKEN");

              // If env not set → fallback to file
              if (string.IsNullOrWhiteSpace(token))
              {
                token = File.ReadAllText("token.txt").Trim();
              }

              var bot = new Bot(token);
              bot.Start().GetAwaiter().GetResult();
            }
            catch (IOException)
            {
              Console.WriteLine("Could not read token.txt and TOKEN env not set.");
            }
            catch (Exception ex)
            {
              Console.WriteLine($"Error: {ex.Message}");
            }
        }

    // Called by Discord.Net when it wants to log something.
    private Task LogAsync(LogMessage log) {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices() {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds
                    | GatewayIntents.DirectMessages
                    | GatewayIntents.GuildMessages
                    | GatewayIntents.GuildMembers,
            };

            var client = new DiscordSocketClient(config);

            return new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<MessageHandlingService>()
                .BuildServiceProvider();
        }
    }
}
