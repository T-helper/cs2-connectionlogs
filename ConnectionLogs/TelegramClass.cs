using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Text.Json;

namespace ConnectionLogs
{
    internal class TelegramClass
    {
        private readonly string _botToken;
        private readonly string _chatId;
        private readonly HttpClient _client;

        public TelegramClass(string botToken, string chatId)
        {
            _botToken = botToken;
            _chatId = chatId;
            _client = new HttpClient();
        }

        private string GenerateMessageContent(bool connectType, CCSPlayerController player, string ipAddress)
        {
            string connectTypeString = connectType ? "connected" : "disconnected";
            string timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            string message = $"{timestamp}: {player.PlayerName} (https://steamcommunity.com/profiles/{player.SteamID}) {player.SteamID} {connectTypeString}";

            if (ipAddress != null)
            {
                message += $" with ip {ipAddress}";
            }

            return message;
        }

        public async Task SendMessage(bool connectType, CCSPlayerController player, string ipAddress = null)
        {
            try
            {
                string messageContent = GenerateMessageContent(connectType, player, ipAddress);
                string encodedMessage = HttpUtility.UrlEncode(messageContent);
                string url = $"https://api.telegram.org/bot{_botToken}/sendMessage?chat_id={_chatId}&text={encodedMessage}";

                HttpResponseMessage response = await _client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JsonDocument responseObject = JsonDocument.Parse(responseBody);
                    string errorDescription = responseObject.RootElement.GetProperty("description").GetString();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed to send message to Telegram. Error: {errorDescription}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception when trying to send message to Telegram.\n{ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
