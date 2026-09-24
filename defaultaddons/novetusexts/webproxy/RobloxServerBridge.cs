using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;
using Novetus.Core;

// Some old clients only let game:HttpGet talk to www.roblox.com. When the game server (hosted from the
// RobloxServer game browser) asks www.roblox.com for these paths, forward them to the configured RobloxServer.
public class RobloxServerBridge : IWebProxyExtension
{
    static readonly string[] ForwardedPaths =
    {
        "/game/validateticket.ashx",
        "/game/servers.ashx"
    };

    public override string Name()
    {
        return "RobloxServer Bridge Extension";
    }

    public override string Version()
    {
        return "1.0.0";
    }

    public override string Author()
    {
        return "RobloxServer";
    }

    static string BaseUrl()
    {
        string address = (GlobalVars.UserConfiguration.ReadSetting("RobloxServerAddress") ?? "").Trim();
        if (address.Length == 0)
        {
            return null;
        }
        if (!address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            address = "http://" + address;
        }
        Uri uri;
        return Uri.TryCreate(address, UriKind.Absolute, out uri) ? uri.GetLeftPart(UriPartial.Authority) : null;
    }

    public override bool IsValidURL(string absolutePath, string host)
    {
        if (BaseUrl() == null)
        {
            return false;
        }

        foreach (string path in ForwardedPaths)
        {
            if (absolutePath.StartsWith(path))
            {
                return true;
            }
        }
        return false;
    }

    public override async Task OnRequest(object sender, SessionEventArgs e)
    {
        string target = BaseUrl() + e.HttpClient.Request.RequestUri.PathAndQuery;
        string result;

        try
        {
            var request = (HttpWebRequest)WebRequest.Create(target);
            request.Proxy = null;
            request.UserAgent = "Roblox/WinInet";
            request.Timeout = 15000;
            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                result = await reader.ReadToEndAsync();
            }
        }
        catch (WebException ex)
        {
            result = "ERROR|RobloxServer bridge: " + ex.Message;
        }

        e.Ok(result, NetFuncs.GenerateHeaders(result.Length.ToString(), "text/plain"));
    }
}
