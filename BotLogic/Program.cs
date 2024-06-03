//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Text.Json;
//using System.Threading;
//using System.Threading.Tasks;
//using Telegram.Bot;
//using Telegram.Bot.Exceptions;
//using Telegram.Bot.Polling;
//using Telegram.Bot.Types;
//using Telegram.Bot.Types.Enums;

//public class SpotifyBot
//{
//    private static readonly string TelegramBotToken = "7166757164:AAFHVOd-0kxhzKle2eOgljwdaYUXP_Hkf1Y";
//    private static readonly string YourApiEndpoint = "https://localhost:7099/SpotifyAuth/callback"; // Замените на ваш API endpoint
//    private static readonly HttpClient HttpClient = new HttpClient();
//    private static readonly TelegramBotClient Bot = new TelegramBotClient(TelegramBotToken);

//    // Словарь для хранения access tokens (в примере используется для хранения в памяти)
//    private static readonly Dictionary<long, string> AccessTokens = new Dictionary<long, string>();

//    public static async Task Main()
//    {
//        var cts = new CancellationTokenSource();
//        var receiverOptions = new ReceiverOptions
//        {
//            AllowedUpdates = Array.Empty<UpdateType>()
//        };

//        Bot.StartReceiving(
//            HandleUpdateAsync,
//            HandleErrorAsync,
//            receiverOptions,
//            cancellationToken: cts.Token
//        );

//        Console.WriteLine("Press any key to exit");
//        Console.ReadKey();
//        cts.Cancel();
//    }

//    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
//    {
//        if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text)
//            return;

//        var chatId = update.Message.Chat.Id;
//        var messageText = update.Message.Text;

//        if (messageText == "/start")
//        {
//            var authorizationUrl = $"https://accounts.spotify.com/authorize?client_id=e3d3dc11e6de46e3bf8e69503bb38f66&response_type=code&redirect_uri=https://localhost:7099/Auth/callback&scope=user-read-private%20user-read-email%20playlist-modify-public%20playlist-modify-private";
//            await botClient.SendTextMessageAsync(
//                chatId: chatId,
//                text: $"Please authorize Spotify: [Authorize]({authorizationUrl})",
//                parseMode: ParseMode.Markdown,
//                cancellationToken: cancellationToken
//            );

//            await botClient.SendTextMessageAsync(
//                chatId: chatId,
//                text: "Please send me the authorization code after you have authorized.",
//                cancellationToken: cancellationToken
//            );
//        }
//        else if (messageText == "/username")
//        {
//            if (AccessTokens.TryGetValue(chatId, out var accessToken))
//            {
//                var response = await HttpClient.GetAsync($"https://localhost:7099/SpotifyAuth/me?accessToken={accessToken}");

//                if (response.IsSuccessStatusCode)
//                {
//                    var userInfo = await response.Content.ReadAsStringAsync();
//                    await botClient.SendTextMessageAsync(
//                        chatId: chatId,
//                        text: $"User Info: {userInfo}",
//                        cancellationToken: cancellationToken
//                    );
//                }
//                else
//                {
//                    await botClient.SendTextMessageAsync(
//                        chatId: chatId,
//                        text: "Failed to retrieve user information. Please try again.",
//                        cancellationToken: cancellationToken
//                    );
//                }
//            }
//            else
//            {
//                await botClient.SendTextMessageAsync(
//                    chatId: chatId,
//                    text: "No access token found. Please authorize first using /start.",
//                    cancellationToken: cancellationToken
//                );
//            }
//        }
//        else
//        {
//            var code = messageText;

//            var response = await HttpClient.GetAsync($"{YourApiEndpoint}?code={code}&state=optional_state_parameter");

//            if (response.IsSuccessStatusCode)
//            {
//                var responseString = await response.Content.ReadAsStringAsync();
//                var tokenResponse = JsonSerializer.Deserialize<SpotifyTokenResponse>(responseString);

//                if (tokenResponse != null && tokenResponse.AccessToken != null)
//                {
//                    AccessTokens[chatId] = tokenResponse.AccessToken;

//                    await botClient.SendTextMessageAsync(
//                        chatId: chatId,
//                        text: "Доступ получен",
//                        cancellationToken: cancellationToken
//                    );
//                }
//                else
//                {
//                    await botClient.SendTextMessageAsync(
//                        chatId: chatId,
//                        text: "Failed to parse access token. Please try again.",
//                        cancellationToken: cancellationToken
//                    );
//                }
//            }
//            else
//            {
//                await botClient.SendTextMessageAsync(
//                    chatId: chatId,
//                    text: "Failed to get access token. Please try again.",
//                    cancellationToken: cancellationToken
//                );
//            }
//        }
//    }

//    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
//    {
//        var errorMessage = exception switch
//        {
//            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
//            _ => exception.ToString()
//        };

//        Console.WriteLine(errorMessage);
//        return Task.CompletedTask;
//    }

//    // Класс для десериализации ответа с токеном
//    private class SpotifyTokenResponse
//    {
//        public string AccessToken { get; set; }
//        public string TokenType { get; set; }
//        public int ExpiresIn { get; set; }
//        public string RefreshToken { get; set; }
//        public string Scope { get; set; }
//    }
//}

