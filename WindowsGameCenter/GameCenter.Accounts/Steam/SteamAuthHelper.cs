using GameCenter.Commun;
using GameCenter.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Windows.Security.Authentication.Web;

namespace GameCenter.Accounts.Steam
{
    public class SteamAuthHelper
    {
        private const string SteamApiKey = "1EE96110F83C48398E1A20B8B2BC8437";
        private const string RedirectUri = "https://firebrowserofficial.vercel.app/auth/steam/callback";
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<GameCenter.Commun.UserData> AuthenticateAsync(XamlRoot xamlRoot)
        {
            var credentials = await ShowSteamLoginDialog(xamlRoot);
            if (credentials == null)
            {
                throw new Exception("Authentication cancelled by user");
            }

            // Here you would typically use the credentials to authenticate with Steam
            // For this example, we'll continue with the OpenID flow as before
            var authorizationUrl = $"https://steamcommunity.com/openid/login?openid.ns=http://specs.openid.net/auth/2.0&openid.mode=checkid_setup&openid.return_to={HttpUtility.UrlEncode(RedirectUri)}&openid.realm={HttpUtility.UrlEncode(RedirectUri)}&openid.identity=http://specs.openid.net/auth/2.0/identifier_select&openid.claimed_id=http://specs.openid.net/auth/2.0/identifier_select";

            var result = await WebAuthenticationBroker.AuthenticateAsync(
                WebAuthenticationOptions.None,
                new Uri(authorizationUrl),
                new Uri(RedirectUri));

            if (result.ResponseStatus == WebAuthenticationStatus.Success)
            {
                var responseUri = new Uri(result.ResponseData);
                var queryParams = HttpUtility.ParseQueryString(responseUri.Query);
                var steamId = queryParams["openid.claimed_id"]?.Split('/').LastOrDefault();

                if (!string.IsNullOrEmpty(steamId))
                {
                    return await GetUserDataAsync(steamId);
                }
            }

            throw new Exception("Authentication failed");
        }

        private static async Task<GameCenter.Commun.UserData> GetUserDataAsync(string steamId)
        {
            var url = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={SteamApiKey}&steamids={steamId}";
            var response = await _httpClient.GetStringAsync(url);
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);

            var playerData = jsonResponse.GetProperty("response").GetProperty("players")[0];

            var userData = new GameCenter.Commun.UserData
            {
                SteamId = steamId,
                PersonaName = playerData.GetProperty("personaname").GetString(),
                ProfileUrl = playerData.GetProperty("profileurl").GetString(),
                AvatarUrl = playerData.GetProperty("avatarfull").GetString(),
                AccessToken = Guid.NewGuid().ToString(), // Generate a mock access token
                RefreshToken = Guid.NewGuid().ToString(), // Generate a mock refresh token
                TokenExpirationTime = DateTime.UtcNow.AddDays(1) // Set expiration to 1 day from now
            };

            await DatabaseHelper.SaveUserDataAsync(userData);

            return userData;
        }

        private static async Task<SteamCredentials> ShowSteamLoginDialog(XamlRoot xamlRoot)
        {
            var dialog = new ContentDialog
            {
                Title = "Steam Login",
                PrimaryButtonText = "Login",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = new SteamLoginControl(),
                XamlRoot = xamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var loginControl = (SteamLoginControl)dialog.Content;
                return new SteamCredentials
                {
                    Username = loginControl.Username,
                    Password = loginControl.Password,
                    SteamGuardCode = loginControl.SteamGuardCode
                };
            }

            return null;
        }
    }

    public class SteamCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string SteamGuardCode { get; set; }
    }

    public class SteamLoginControl : StackPanel
    {
        private TextBox _usernameBox;
        private PasswordBox _passwordBox;
        private TextBox _steamGuardBox;

        public string Username => _usernameBox.Text;
        public string Password => _passwordBox.Password;
        public string SteamGuardCode => _steamGuardBox.Text;

        public SteamLoginControl()
        {
            Spacing = 10;

            _usernameBox = new TextBox { Header = "Username", PlaceholderText = "Enter your Steam username" };
            _passwordBox = new PasswordBox { Header = "Password", PlaceholderText = "Enter your Steam password" };
            _steamGuardBox = new TextBox { Header = "Steam Guard Code (if required)", PlaceholderText = "Enter Steam Guard code" };

            Children.Add(_usernameBox);
            Children.Add(_passwordBox);
            Children.Add(_steamGuardBox);
        }
    }
}