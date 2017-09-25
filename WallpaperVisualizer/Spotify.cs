using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace WallpaperVisualizer
{
    class Spotify
    {
        public Result result;
        public bool on;
        public bool newSong;
        private string oAuthToken;
        private string csrfToken;
        private const string hostname = "http://49664118.spotilocal.com:4380";
        private const string ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36";
        private Timer timer;
        public Bitmap artwork { get; private set; }
        public Spotify()
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            oAuthToken = GetOAuthToken();
            csrfToken = GetCSRFToken();
            timer = new Timer(GetStatus, "", 100, 500);
        }
        private string getRequest(string url)
        {
            WebClient client = new WebClient();
            client.Headers.Add("Origin", "https://open.spotify.com");
            client.Headers.Add("User-Agent", ua);
            return Encoding.UTF8.GetString(Encoding.Default.GetBytes(client.DownloadString(url)));
        }
        private string GetOAuthToken()
        {
            return JsonConvert.DeserializeObject<OAuthTokenJson>(getRequest("https://open.spotify.com/token")).t;
        }
        private string GetCSRFToken()
        {
            return JsonConvert.DeserializeObject<CSRFTokenJson>(getRequest(hostname + "/simplecsrf/token.json")).token;
        }

        private void GetStatus(object state) { GetStatus(); }
        public void GetStatus()
        {
            string output = getRequest(hostname + "/remote/status.json" + "?oauth=" + oAuthToken + "&csrf=" + csrfToken);
            if (output.Contains("Invalid Csrf token"))
            {
                csrfToken = GetCSRFToken();
            }
            else if (output.Contains("Invalid OAuth token"))
            {
                oAuthToken = GetOAuthToken();
            }
            else if (output.Contains("Expired OAuth token"))
            {
                oAuthToken = GetOAuthToken();
            }
            else if (output.Contains("\"error\""))
            {
                on = false;
            }
            else
            {
                Result result = JsonConvert.DeserializeObject<Result>(output);
                if (this.result == null || result.track.track_resource.uri != this.result.track.track_resource.uri)
                {
                    artwork = GetArtwork(result.track.track_resource.uri);
                    newSong = true;
                }
                this.result = result;
                on = true;
            }

        }

        public Bitmap GetArtwork(string uri)
        {
            WebClient client = new WebClient();
            client.Headers.Add("Origin", "https://open.spotify.com");
            client.Headers.Add("User-Agent", ua);
            ArtworkResponse response;
            try
            {
                response = JsonConvert.DeserializeObject<ArtworkResponse>(Encoding.UTF8.GetString(Encoding.Default.GetBytes(client.DownloadString("https://open.spotify.com/oembed?url=" + uri))));
            }
            catch (WebException)
            {
                // just in case
                response = JsonConvert.DeserializeObject<ArtworkResponse>(Encoding.UTF8.GetString(Encoding.Default.GetBytes(client.DownloadString("https://open.spotify.com/oembed?url=" + uri))));
            }
            return new Bitmap(new MemoryStream(client.DownloadData(response.thumbnail_url)));
        }

        #pragma warning disable 649
        class OAuthTokenJson
        {
            public string t;
        }
        class CSRFTokenJson
        {
            public string token;
        }
        class ArtworkResponse
        {
            public int height;
            public int width;
            public string html;
            public string type;
            public string version;
            public int thumbnail_height;
            public int thumbnail_width;
            public string thumbnail_url;
            public string title;
            public string provider_name;
            public string provider_url;
        }
        public class Result
        {
            public bool repeat;
            public bool play_enabled;
            public Track track;
            public int version;
            public bool shuffle;
            public bool prev_enabled;
            public double volume;
            public Context context;
            public OpenGraphState open_graph_state;
            public bool next_enabled;
            public int server_time;
            public bool running;
            public string client_version;
            public bool online;
            double playing_position;
            public bool playing;
            public Error error;
            public class Track
            {
                public int length;
                public string track_type;
                public Resource artist_resource;
                public Resource track_resource;
                public Resource album_resource;
                public class Resource
                {
                    public string uri;
                    public string name;
                    public Location location;
                    public class Location
                    {
                        public string og;
                    }
                }
            }
            public class Context { }
            public class OpenGraphState
            {
                public bool private_session;
                public bool posting_disabled;
            }
            public class Error
            {
                public string type;
                public string message;
            }
        }
        #pragma warning restore 649
    }
}
