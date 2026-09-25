using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RobloxPlayerLauncher
{
    public enum LaunchMode
    {
        Play,
        Host
    }

    /// <summary>
    /// What the website's Play / Host Server button asked for, in the style of the roblox-player: links:
    ///   robloxserver-player:1+launchmode:play+gameinfo:TICKET+placeid:ID+baseurl:URL[+placelauncherurl:URL][+launchtime:MS]
    /// Values are URL encoded. Everything is validated here because any web page can open this link.
    /// </summary>
    public class LaunchRequest
    {
        public const string Scheme = "robloxserver-player";

        public LaunchMode Mode { get; private set; }
        public string Ticket { get; private set; }
        public long PlaceId { get; private set; }
        public Uri BaseUrl { get; private set; }
        public Uri PlaceLauncherUrl { get; private set; }

        public static bool LooksLikeLaunchUri(string argument)
        {
            return argument != null && argument.StartsWith(Scheme + ":", StringComparison.OrdinalIgnoreCase);
        }

        public static LaunchRequest Parse(string uri)
        {
            if (!LooksLikeLaunchUri(uri))
            {
                throw new FormatException("This is not a " + Scheme + ": link.");
            }

            // Some browsers add a trailing slash, some encode the separators.
            string body = uri.Substring(Scheme.Length + 1).Trim().TrimEnd('/');
            if (body.IndexOf('+') < 0 && body.IndexOf("%2B", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                body = Regex.Replace(body, "%2B", "+", RegexOptions.IgnoreCase);
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] parts = body.Split('+');
            if (parts.Length == 0 || parts[0] != "1")
            {
                throw new FormatException("Unsupported launch link version. Update ROBLOX from the website.");
            }
            for (int i = 1; i < parts.Length; i++)
            {
                int colon = parts[i].IndexOf(':');
                if (colon > 0)
                {
                    values[parts[i].Substring(0, colon)] = Uri.UnescapeDataString(parts[i].Substring(colon + 1));
                }
            }

            var request = new LaunchRequest();

            string mode = Get(values, "launchmode").ToLowerInvariant();
            if (mode == "play")
            {
                request.Mode = LaunchMode.Play;
            }
            else if (mode == "host")
            {
                request.Mode = LaunchMode.Host;
            }
            else
            {
                throw new FormatException("Unknown launch mode '" + mode + "'.");
            }

            request.Ticket = Get(values, "gameinfo");
            if (!Regex.IsMatch(request.Ticket, "^[A-Za-z0-9]{16,256}$"))
            {
                throw new FormatException("The authentication ticket in the link is not valid.");
            }

            long placeId;
            if (!long.TryParse(Get(values, "placeid"), out placeId) || placeId <= 0)
            {
                throw new FormatException("The place id in the link is not valid.");
            }
            request.PlaceId = placeId;

            request.BaseUrl = ParseBaseUrl(Get(values, "baseurl"));

            string placeLauncher;
            if (values.TryGetValue("placelauncherurl", out placeLauncher) && placeLauncher.Length > 0)
            {
                Uri url;
                if (!Uri.TryCreate(placeLauncher, UriKind.Absolute, out url) || !SameSite(url, request.BaseUrl)
                    || !url.AbsolutePath.StartsWith(request.BaseUrl.AbsolutePath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException("The place launcher address does not belong to " + request.BaseUrl.Host + ".");
                }
                request.PlaceLauncherUrl = url;
            }
            else
            {
                request.PlaceLauncherUrl = new Uri(request.BaseUrl, "Game/PlaceLauncher.ashx?request=RequestGame&placeId=" + placeId);
            }

            return request;
        }

        static string Get(Dictionary<string, string> values, string key)
        {
            string value;
            if (!values.TryGetValue(key, out value) || string.IsNullOrWhiteSpace(value))
            {
                throw new FormatException("The launch link has no " + key + ".");
            }
            return value.Trim();
        }

        public static Uri ParseBaseUrl(string text)
        {
            Uri url;
            if (!Uri.TryCreate(text, UriKind.Absolute, out url) || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
                || !string.IsNullOrEmpty(url.UserInfo) || url.Query.Length > 0 || url.Fragment.Length > 0)
            {
                throw new FormatException("The website address in the link is not valid.");
            }
            string path = url.AbsolutePath.EndsWith("/") ? url.AbsolutePath : url.AbsolutePath + "/";
            return new Uri(url.Scheme + "://" + url.Authority + path);
        }

        public static bool SameSite(Uri a, Uri b)
        {
            return string.Equals(a.Scheme, b.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.Host, b.Host, StringComparison.OrdinalIgnoreCase)
                && a.Port == b.Port;
        }
    }
}
