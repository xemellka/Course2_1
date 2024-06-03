using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

public class SpotifyBot
{
    private static readonly string TelegramBotToken = "7166757164:AAFHVOd-0kxhzKle2eOgljwdaYUXP_Hkf1Y";
    private static readonly string Endpoint = "https://spokiapi-a6c792bd77b1.herokuapp.com/SpotifyAuth";
    private static readonly HttpClient HttpClient = new HttpClient();
    private static readonly TelegramBotClient Bot = new TelegramBotClient(TelegramBotToken);
    private static readonly Dictionary<long, string> UserTokens = new Dictionary<long, string>();
    private static readonly Dictionary<long, bool> UserAuthorizationState = new Dictionary<long, bool>();
    private static readonly Dictionary<long, string> UserPlaylists = new Dictionary<long, string>();
    private static readonly Dictionary<long, bool> UserTrackSearchState = new Dictionary<long, bool>();
    private static readonly Dictionary<long, bool> UserTrackAddState = new Dictionary<long, bool>();
    private static readonly Dictionary<long, bool> UserTrackRemoveState = new Dictionary<long, bool>();

    public static async Task Main()
    {
        var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        Bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token
        );

        Console.WriteLine("Bot is working (maybe)");
        Console.ReadKey();
        cts.Cancel();
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text)
            return;

        var chatId = update.Message.Chat.Id;
        var messageText = update.Message.Text;

        if (messageText == "/start")
        {
            var authorizationUrl = $"https://accounts.spotify.com/authorize?client_id=e3d3dc11e6de46e3bf8e69503bb38f66&response_type=code&redirect_uri=https://spokiapi-a6c792bd77b1.herokuapp.com/Auth/callback&scope=user-read-private%20user-read-email%20playlist-modify-public%20playlist-modify-private%20user-modify-playback-state";
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Please authorize Spotify: [Authorize]({authorizationUrl})",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken,
                replyMarkup: GetMainMenu()
            );

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Please send me the authorization code after you have authorized.",
                cancellationToken: cancellationToken,
                replyMarkup: GetMainMenu()
            );
            UserAuthorizationState[chatId] = true;
        }
        else if (messageText == "Username")
        {
            if (UserTokens.TryGetValue(chatId, out var accessToken))
            {
                var response = await HttpClient.GetAsync($"https://spokiapi-a6c792bd77b1.herokuapp.com/SpotifyAuth/me?accessToken={accessToken}");

                if (response.IsSuccessStatusCode)
                {
                    var userProfile = await response.Content.ReadAsStringAsync();
                    dynamic userProfileObject = JObject.Parse(userProfile);
                    string displayName = userProfileObject.display_name;
                    string id = userProfileObject.id;
                    string country = userProfileObject.country;
                    string email = userProfileObject.email;

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"User Profile:\n\nDisplay Name: {displayName}\nID: {id}\nCountry: {country}\nEmail: {email}",
                        cancellationToken: cancellationToken,
                        replyMarkup: GetMainMenu()
                    );
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Access token not found. Please authorize first.",
                    cancellationToken: cancellationToken,
                    replyMarkup: GetMainMenu()
                );
            }
        }
        else if (messageText == "Createplaylist")
        {
            if (UserTokens.TryGetValue(chatId, out var accessToken))
            {
                UserAuthorizationState[chatId] = true;

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Please send the playlist details in the format: Name,Description,IsPublic(yes/no)",
                    cancellationToken: cancellationToken,
                    replyMarkup: GetMainMenu()
                );
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Access token not found. Please authorize first.",
                    cancellationToken: cancellationToken,
                    replyMarkup: GetMainMenu()
                );
            }
        }
        else if (messageText == "Searchtrack")
        {
            if (UserTokens.TryGetValue(chatId, out var accessToken))
            {
                UserTrackSearchState[chatId] = true;

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Please send the track name",
                    cancellationToken: cancellationToken,
                    replyMarkup: GetMainMenu()
                );
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Access token not found. Please authorize first.",
                    cancellationToken: cancellationToken,
                    replyMarkup: GetMainMenu()
                );
            }
        }
        else if (messageText == "Nexttrack")
        {
            if (UserTokens.TryGetValue(chatId, out var accessToken))
            {
                var response = await HttpClient.PostAsync($"https://spokiapi-a6c792bd77b1.herokuapp.com/SpotifyAuth/nextTrack?accessToken={accessToken}", null);

                if (response.IsSuccessStatusCode)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Skipped to the next track.",
                        cancellationToken: cancellationToken,
                        replyMarkup: GetMainMenu()
                    );
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Access token not found. Please authorize first.",
                    cancellationToken: cancellationToken,
                    replyMarkup: GetMainMenu()
                );
            }
        }
        else if (messageText == "Previoustrack")
        {
            if (UserTokens.TryGetValue(chatId, out var accessToken))
            {
                var response = await HttpClient.PostAsync($"https://spokiapi-a6c792bd77b1.herokuapp.com/SpotifyAuth/previousTrack?accessToken={accessToken}", null);

                if (response.IsSuccessStatusCode)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Skipped to the next track.",
                        cancellationToken: cancellationToken,
                        replyMarkup: GetMainMenu()
                    );
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Access token not found. Please authorize first.",
                    cancellationToken: cancellationToken,
                    replyMarkup: GetMainMenu()
                );
            }
        }
        else if (messageText == "Pause")
        {
            if (UserTokens.TryGetValue(chatId, out var accessToken))
            {
                var response = await HttpClient.PutAsync($"https://spokiapi-a6c792bd77b1.herokuapp.com/SpotifyAuth/pause?accessToken={accessToken}", null);

                if (response.IsSuccessStatusCode)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Pause track.",
                        cancellationToken: cancellationToken,
                        replyMarkup: GetMainMenu()
                    );
                }
            }

            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Access token not found. Please authorize first.",
                    cancellationToken: cancellationToken,
                    replyMarkup: GetMainMenu()
                );
            }
        }
        else if (messageText == "Play")
        {
            if (UserTokens.TryGetValue(chatId, out var accessToken))
            {
                var response = await HttpClient.PutAsync($"https://spokiapi-a6c792bd77b1.herokuapp.com/SpotifyAuth/play?accessToken={accessToken}", null);

                if (response.IsSuccessStatusCode)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Resume track.",
                        cancellationToken: cancellationToken,
                        replyMarkup: GetMainMenu()
                    );
                }
            }

            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Access token not found. Please authorize first.",
                    cancellationToken: cancellationToken,
                    replyMarkup: GetMainMenu()
                );
            }
        }
        else if (messageText == "Addtrack")
        {
            if (UserTokens.TryGetValue(chatId, out var accessToken) && UserPlaylists.TryGetValue(chatId, out var playlistId))
            {
                UserTrackAddState[chatId] = true;

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Please send the track ID to add to the playlist.",
                    cancellationToken: cancellationToken,
                    replyMarkup: GetMainMenu()
                );
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Access token or playlist ID not found. Please authorize and create a playlist first.",
                    cancellationToken: cancellationToken,
                    replyMarkup: GetMainMenu()
                );
            }
        }
        else if (messageText == "Removetrack")
        {
            if (UserTokens.TryGetValue(chatId, out var accessToken) && UserPlaylists.TryGetValue(chatId, out var playlistId))
            {
                UserTrackRemoveState[chatId] = true;

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Please send the track ID to remove from the playlist.",
                    cancellationToken: cancellationToken,
                    replyMarkup: GetMainMenu()
                );
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Access token or playlist ID not found. Please authorize and create a playlist first.",
                    cancellationToken: cancellationToken,
                    replyMarkup: GetMainMenu()
                );
            }
        }
        else
        {
            if (UserAuthorizationState.TryGetValue(chatId, out var isAwaitingCode) && isAwaitingCode)
            {
                if (messageText.Contains(","))
                {
                    var parts = messageText.Split(',');
                    if (parts.Length == 3)
                    {
                        var name = parts[0].Trim();
                        var description = parts[1].Trim();
                        var isPublicInput = parts[2].Trim().ToLower();

                        bool? isPublic = isPublicInput switch
                        {
                            "yes" => true,
                            "no" => false,
                            _ => (bool?)null
                        };

                        if (isPublic.HasValue)
                        {
                            var playlistInfo = new PlaylistInfo
                            {
                                Name = name,
                                Description = description,
                                IsPublic = isPublic.Value
                            };

                            if (UserTokens.TryGetValue(chatId, out var accessToken))
                            {
                                var userProfileResponse = await HttpClient.GetAsync($"https://spokiapi-a6c792bd77b1.herokuapp.com/SpotifyAuth/me?accessToken={accessToken}");
                                if (userProfileResponse.IsSuccessStatusCode)
                                {
                                    var userProfile = await userProfileResponse.Content.ReadAsStringAsync();
                                    dynamic userProfileObject = JObject.Parse(userProfile);
                                    var userId = userProfileObject.id;

                                    var createPlaylistResponse = await CreatePlaylist(userId.ToString(), accessToken, playlistInfo);
                                    if (createPlaylistResponse.IsSuccessStatusCode)
                                    {
                                        var playlistResponseString = await createPlaylistResponse.Content.ReadAsStringAsync();
                                        dynamic playlistResponseObject = JObject.Parse(playlistResponseString);
                                        var playlistId = playlistResponseObject.id;

                                        UserPlaylists[chatId] = playlistId;

                                        await botClient.SendTextMessageAsync(
                                            chatId: chatId,
                                            text: "Playlist created successfully.",
                                            cancellationToken: cancellationToken,
                                            replyMarkup: GetMainMenu()
                                        );
                                    }
                                }
                                else
                                {
                                    var errorString = await userProfileResponse.Content.ReadAsStringAsync();
                                    await botClient.SendTextMessageAsync(
                                        chatId: chatId,
                                        text: $"Failed to get user profile. Error: {errorString}",
                                        cancellationToken: cancellationToken,
                                        replyMarkup: GetMainMenu()
                                    );
                                }
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "Access token not found. Please authorize first.",
                                    cancellationToken: cancellationToken,
                                    replyMarkup: GetMainMenu()
                                );
                            }
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Invalid playlist details format. Please use the format: Name,Description,IsPublic (yes/no)",
                            cancellationToken: cancellationToken,
                            replyMarkup: GetMainMenu()
                        );
                    }
                    UserAuthorizationState[chatId] = false;
                }
                else
                {
                    var code = messageText;

                    var response = await HttpClient.GetAsync($"{Endpoint}/callback?code={code}&state=optional_state_parameter");

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        dynamic responseObject = JObject.Parse(responseString);
                        var accessToken = responseObject.access_token;

                        UserTokens[chatId] = accessToken;
                        UserAuthorizationState[chatId] = false;

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Access received",
                            cancellationToken: cancellationToken,
                            replyMarkup: GetMainMenu()
                        );
                    }
                    else
                    {
                        var errorString = await response.Content.ReadAsStringAsync();
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Failed to get access token.",
                            cancellationToken: cancellationToken,
                            replyMarkup: GetMainMenu()
                        );
                    }
                }
            }
            else if (UserTrackSearchState.TryGetValue(chatId, out var isAwaitingTrack) && isAwaitingTrack)
            {
                var trackName = messageText.Trim();
                if (UserTokens.TryGetValue(chatId, out var accessToken))
                {
                    var response = await HttpClient.GetAsync($"https://spokiapi-a6c792bd77b1.herokuapp.com/SpotifyAuth/searchTrack?query={trackName}&accessToken={accessToken}");
                    if (response.IsSuccessStatusCode)
                    {
                        var searchResults = await response.Content.ReadAsStringAsync();
                        dynamic searchResultsObject = JObject.Parse(searchResults);
                        var tracks = searchResultsObject.tracks.items;

                        if (tracks.Count > 0)
                        {
                            var message = $"Top tracks for '{trackName}':\n\n";

                            for (int i = 0; i < Math.Min(tracks.Count, 4); i++)
                            {
                                var track = tracks[i];
                                var artists = string.Join(", ", ((JArray)track.artists).Select(a => (string)a["name"]));
                                message += $"{i + 1}. {track.name} by {artists}\n";
                                message += $"   ID: {track.id}\n";
                            }

                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: message,
                                cancellationToken: cancellationToken,
                                replyMarkup: GetMainMenu()
                            );
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"No tracks found for '{trackName}'.",
                                cancellationToken: cancellationToken,
                                replyMarkup: GetMainMenu()
                            );
                        }
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Access token not found. Please authorize first.",
                        cancellationToken: cancellationToken,
                        replyMarkup: GetMainMenu()
                    );
                }
                UserTrackSearchState[chatId] = false;
            }
            else if (UserTrackAddState.TryGetValue(chatId, out var isAwaitingTrackId) && isAwaitingTrackId)
            {
                var trackId = messageText.Trim();
                if (UserTokens.TryGetValue(chatId, out var accessToken) && UserPlaylists.TryGetValue(chatId, out var playlistId))
                {
                    var response = await AddTrackToPlaylist(playlistId, trackId, accessToken);
                    if (response.IsSuccessStatusCode)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Track added to playlist successfully.",
                            cancellationToken: cancellationToken,
                            replyMarkup: GetMainMenu()
                        );
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Access token or playlist ID not found. Please authorize and create a playlist first.",
                        cancellationToken: cancellationToken,
                        replyMarkup: GetMainMenu()
                    );
                }
                UserTrackAddState[chatId] = false;
            }
            else if (UserTrackRemoveState.TryGetValue(chatId, out var isAwaitingTrackRemoveId) && isAwaitingTrackRemoveId)
            {
                var trackId = messageText.Trim();
                if (UserTokens.TryGetValue(chatId, out var accessToken) && UserPlaylists.TryGetValue(chatId, out var playlistId))
                {
                    var response = await RemoveTrackFromPlaylist(playlistId, trackId, accessToken);
                    if (response.IsSuccessStatusCode)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Track removed from playlist successfully.",
                            cancellationToken: cancellationToken,
                            replyMarkup: GetMainMenu()
                        );
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Access token or playlist ID not found. Please authorize and create a playlist first.",
                        cancellationToken: cancellationToken,
                        replyMarkup: GetMainMenu()
                    );
                }
                UserTrackRemoveState[chatId] = false;
            }
        }
    }
    public static IReplyMarkup GetMainMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
        new[]
        {
            new KeyboardButton("/start"),
            new KeyboardButton("Username"),
            new KeyboardButton("Searchtrack"),
        },
        new[]
        {
            new KeyboardButton("Createplaylist"),
            new KeyboardButton("Addtrack"),
            new KeyboardButton("Removetrack"),
        },
        new[]
        {
            new KeyboardButton("Play"),
            new KeyboardButton("Pause"),
            new KeyboardButton("Nexttrack"),
            new KeyboardButton("Previoustrack"),
        }
    })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }
    private static async Task<HttpResponseMessage> CreatePlaylist(string userId, string accessToken, PlaylistInfo playlistInfo)
    {
        string url = $"https://spokiapi-a6c792bd77b1.herokuapp.com/SpotifyAuth/createPlaylist?userId={userId}&accessToken={accessToken}";
        var playlistData = new
        {
            name = playlistInfo.Name,
            description = playlistInfo.Description,
            @public = playlistInfo.IsPublic
        };
        var content = new StringContent(JsonConvert.SerializeObject(playlistData), Encoding.UTF8, "application/json");
        return await HttpClient.PostAsync(url, content);
    }
    private static async Task<HttpResponseMessage> AddTrackToPlaylist(string playlistId, string trackId, string accessToken)
    {
        string url = $"https://spokiapi-a6c792bd77b1.herokuapp.com/SpotifyAuth/addTrackToPlaylist?playlistId={playlistId}&accessToken={accessToken}";
        var trackUris = new List<string> { $"spotify:track:{trackId}" };
        var content = new StringContent(JsonConvert.SerializeObject(trackUris), Encoding.UTF8, "application/json");
        return await HttpClient.PostAsync(url, content);
    }
    private static async Task<HttpResponseMessage> RemoveTrackFromPlaylist(string playlistId, string trackId, string accessToken)
    {
        string url = $"https://spokiapi-a6c792bd77b1.herokuapp.com/SpotifyAuth/removeTracksFromPlaylist?playlistId={playlistId}&accessToken={accessToken}";
        var trackUris = new { Tracks = new List<string> { $"spotify:track:{trackId}" }, SnapshotId = "" };
        var content = new StringContent(JsonConvert.SerializeObject(trackUris), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Delete, url)
        {
            Content = content
        };
        return await HttpClient.SendAsync(request);
    }
    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
    public class PlaylistInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsPublic { get; set; }
    }
}