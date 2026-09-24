#region Usings
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
#endregion

namespace NovetusLauncher
{
    #region RobloxServer Definitions
    public class RobloxServerGame
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CreatorName { get; set; }
        public string Client { get; set; }
        public long Visits { get; set; }
        public int Playing { get; set; }
        public int Servers { get; set; }
        public int MaxPlayers { get; set; }
        public bool CanHost { get; set; }
        public bool FilteringEnabled { get; set; }
        public string Md5 { get; set; }
        public string Updated { get; set; }
    }

    public class RobloxServerJoin
    {
        public string JobId { get; set; }
        public long PlaceId { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public string Client { get; set; }
        public string Ticket { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
    }

    public class RobloxServerJob
    {
        public string JobId { get; set; }
        public string ServerKey { get; set; }
        public long PlaceId { get; set; }
        public bool RequireAuthTickets { get; set; }
        public bool FilteringEnabled { get; set; }
        public int HeartbeatSeconds { get; set; }
    }

    public class RobloxServerException : Exception
    {
        public RobloxServerException(string message) : base(message) { }
    }
    #endregion

    #region RobloxServer API
    /// <summary>
    /// Talks to a RobloxServer (ASP.NET) website: logs in with the .ROBLOSECURITY cookie, lists the
    /// published games, downloads places, asks PlaceLauncher/Join for a server and registers hosted jobs.
    /// </summary>
    public class RobloxServerApi
    {
        CookieContainer cookies = new CookieContainer();
        string csrfToken = "";

        public string BaseUrl { get; private set; }
        public bool LoggedIn { get; private set; }
        public long UserId { get; private set; }
        public string UserName { get; private set; }

        public RobloxServerApi(string address)
        {
            BaseUrl = NormalizeAddress(address);
        }

        /// <summary>"192.168.1.2", "meuroblox.duckdns.org:8080" or "http://host/" -> "http://host[:port]/".</summary>
        public static string NormalizeAddress(string address)
        {
            address = (address ?? "").Trim();
            if (string.IsNullOrEmpty(address))
            {
                throw new RobloxServerException("Enter the address of the RobloxServer website.");
            }

            if (!address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                address = "http://" + address;
            }

            Uri uri;
            if (!Uri.TryCreate(address, UriKind.Absolute, out uri))
            {
                throw new RobloxServerException("'" + address + "' is not a valid address.");
            }

            return uri.GetLeftPart(UriPartial.Authority) + "/";
        }

        #region HTTP
        HttpWebRequest CreateRequest(string method, string path)
        {
            var request = (HttpWebRequest)WebRequest.Create(BaseUrl + path.TrimStart('/'));
            request.Method = method;
            request.CookieContainer = cookies;
            request.UserAgent = "Roblox/WinInet";
            request.Timeout = 30000;
            request.AllowAutoRedirect = false;
            // The Novetus web proxy may be the system proxy; talk to the server directly.
            request.Proxy = null;
            if (!string.IsNullOrEmpty(csrfToken))
            {
                request.Headers["X-CSRF-TOKEN"] = csrfToken;
            }
            return request;
        }

        static HttpWebResponse GetResponse(HttpWebRequest request)
        {
            try
            {
                return (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                if (response == null)
                {
                    throw new RobloxServerException("Could not reach the RobloxServer (" + ex.Message + ").");
                }
                return response;
            }
        }

        /// <summary>Sends a request, retrying once when the server answers 403 with a new X-CSRF-TOKEN (2015 behaviour).</summary>
        string Send(string method, string path, IDictionary<string, string> form = null)
        {
            for (int attempt = 0; ; attempt++)
            {
                HttpWebRequest request = CreateRequest(method, path);
                if (form != null)
                {
                    string body = string.Join("&", form.Select(p => HttpUtility.UrlEncode(p.Key) + "=" + HttpUtility.UrlEncode(p.Value ?? "")));
                    byte[] bytes = Encoding.UTF8.GetBytes(body);
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = bytes.Length;
                    using (Stream stream = request.GetRequestStream())
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }

                using (HttpWebResponse response = GetResponse(request))
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    string text = reader.ReadToEnd();
                    string token = response.Headers["X-CSRF-TOKEN"];
                    if (response.StatusCode == HttpStatusCode.Forbidden && !string.IsNullOrEmpty(token) && attempt == 0)
                    {
                        csrfToken = token;
                        continue;
                    }

                    if ((int)response.StatusCode >= 400)
                    {
                        string message = text.Length > 200 ? text.Substring(0, 200) : text;
                        throw new RobloxServerException("RobloxServer answered " + (int)response.StatusCode + ": " + message);
                    }
                    return text;
                }
            }
        }

        static JObject ParseObject(string json)
        {
            try
            {
                return JObject.Parse(json);
            }
            catch (Exception)
            {
                throw new RobloxServerException("Unexpected answer from the RobloxServer. Is the address correct?");
            }
        }

        static string Query(params string[] pairs)
        {
            var parts = new List<string>();
            for (int i = 0; i + 1 < pairs.Length; i += 2)
            {
                parts.Add(HttpUtility.UrlEncode(pairs[i]) + "=" + HttpUtility.UrlEncode(pairs[i + 1] ?? ""));
            }
            return string.Join("&", parts);
        }
        #endregion

        #region Account
        public string SiteName()
        {
            JObject status = ParseObject(Send("GET", "Api/Status.ashx"));
            if ((string)status["status"] != "online")
            {
                throw new RobloxServerException("The RobloxServer is offline.");
            }
            return (string)status["name"] ?? "RobloxServer";
        }

        public void Login(string userName, string password)
        {
            JObject result = ParseObject(Send("POST", "Api/Login.ashx", new Dictionary<string, string>
            {
                { "username", userName },
                { "password", password }
            }));

            if ((bool?)result["success"] != true)
            {
                throw new RobloxServerException((string)result["message"] ?? "Login failed.");
            }

            LoggedIn = true;
            UserId = (long)result["userId"];
            UserName = (string)result["userName"];
        }

        public void Logout()
        {
            try
            {
                if (LoggedIn)
                {
                    Send("POST", "Api/Logout.ashx", new Dictionary<string, string>());
                }
            }
            finally
            {
                cookies = new CookieContainer();
                csrfToken = "";
                LoggedIn = false;
                UserId = 0;
                UserName = null;
            }
        }
        #endregion

        #region Games
        public List<RobloxServerGame> GetGames()
        {
            JObject result = ParseObject(Send("GET", "Api/Games.ashx"));
            return ((JArray)result["data"]).Select(g => new RobloxServerGame
            {
                Id = (long)g["id"],
                Name = (string)g["name"],
                Description = (string)g["description"],
                CreatorName = (string)g["creatorName"],
                Client = (string)g["client"],
                Visits = (long)g["visits"],
                Playing = (int)g["playing"],
                Servers = (int)g["servers"],
                MaxPlayers = (int)g["maxPlayers"],
                CanHost = (bool)g["canHost"],
                FilteringEnabled = (bool)g["filteringEnabled"],
                Md5 = (string)g["md5"],
                Updated = (string)g["updated"]
            }).ToList();
        }

        /// <summary>Downloads the place file and checks it against the MD5 the website reported.</summary>
        public void DownloadPlace(RobloxServerGame game, string path)
        {
            HttpWebRequest request = CreateRequest("GET", "asset/?id=" + game.Id);
            byte[] data;
            using (HttpWebResponse response = GetResponse(request))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new RobloxServerException("Could not download '" + game.Name + "' (" + (int)response.StatusCode + ").");
                }
                using (var memory = new MemoryStream())
                {
                    response.GetResponseStream().CopyTo(memory);
                    data = memory.ToArray();
                }
            }

            if (!string.IsNullOrEmpty(game.Md5))
            {
                using (var md5 = MD5.Create())
                {
                    string hash = BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "");
                    if (!string.Equals(hash, game.Md5, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new RobloxServerException("The downloaded place is corrupted (MD5 mismatch).");
                    }
                }
            }

            string temp = path + ".download";
            File.WriteAllBytes(temp, data);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.Move(temp, path);
        }

        /// <summary>Counts a Play Solo visit (the website increments visits when Visit.ashx loads the place).</summary>
        public void RecordSoloVisit(long placeId)
        {
            Send("GET", "Game/Visit.ashx?" + Query("IsPlaySolo", "1", "PlaceID", placeId.ToString()));
        }
        #endregion

        #region Joining
        /// <summary>PlaceLauncher.ashx + Join.ashx, like the 2015 website did before starting RobloxPlayer.</summary>
        public RobloxServerJoin RequestJoin(long placeId)
        {
            JObject launch = ParseObject(Send("GET", "Game/PlaceLauncher.ashx?" + Query("request", "RequestGame", "placeId", placeId.ToString())));
            int status = (int)launch["status"];
            if (status != 2)
            {
                throw new RobloxServerException((string)launch["message"] ?? ("Cannot join this game (status " + status + ")."));
            }

            string jobId = (string)launch["jobId"];
            string script = Send("GET", "Game/Join.ashx?" + Query("jobId", jobId));

            // Strip the --rbxsig%SIGNATURE% header, the rest is the JSON join script.
            if (script.StartsWith("--rbxsig%"))
            {
                int end = script.IndexOf('%', 9);
                script = end >= 0 ? script.Substring(end + 1) : script;
            }

            JObject join = ParseObject(script.Trim());
            return new RobloxServerJoin
            {
                JobId = jobId,
                PlaceId = (long)join["PlaceId"],
                Address = (string)join["MachineAddress"],
                Port = (int)join["ServerPort"],
                Client = (string)join["NovetusClient"],
                Ticket = (string)join["ClientTicket"],
                UserId = (long)join["UserId"],
                UserName = (string)join["UserName"]
            };
        }
        #endregion

        #region Hosting
        public RobloxServerJob RegisterServer(long placeId, int port, string client, string name, int maxPlayers, string address, string version)
        {
            JObject result = ParseObject(Send("POST", "Game/Servers.ashx", new Dictionary<string, string>
            {
                { "action", "register" },
                { "placeId", placeId.ToString() },
                { "port", port.ToString() },
                { "client", client },
                { "name", name },
                { "maxPlayers", maxPlayers.ToString() },
                { "address", address },
                { "version", version }
            }));

            if ((bool?)result["success"] != true)
            {
                throw new RobloxServerException((string)result["message"] ?? "Could not register the server.");
            }

            return new RobloxServerJob
            {
                JobId = (string)result["jobId"],
                ServerKey = (string)result["serverKey"],
                PlaceId = (long)result["placeId"],
                RequireAuthTickets = (bool)result["requireAuthTickets"],
                FilteringEnabled = (bool)result["filteringEnabled"],
                HeartbeatSeconds = (int?)result["heartbeatSeconds"] ?? 60
            };
        }

        public void Heartbeat(RobloxServerJob job)
        {
            Send("GET", "Game/Servers.ashx?" + Query("action", "heartbeat", "jobId", job.JobId, "serverKey", job.ServerKey));
        }

        public void Unregister(RobloxServerJob job)
        {
            Send("GET", "Game/Servers.ashx?" + Query("action", "unregister", "jobId", job.JobId, "serverKey", job.ServerKey));
        }
        #endregion
    }
    #endregion
}
