using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace WallpaperVisualizer
{
    class Spotify
    {
        private string oAuthToken;
        private string csrfToken;
        private const string hostname = "http://49664118.spotilocal.com:4380";

        private WebClient client;
        public Spotify()
        {
            client = new WebClient();
            client.Headers.Add("Origin", "https://open.spotify.com");
            oAuthToken = GetOAuthToken();
        }
        private string getRequest(string url)
        {
            return Encoding.UTF8.GetString(Encoding.Default.GetBytes(client.DownloadString(url)));
        }
        private string GetOAuthToken()
        {
            return JsonConvert.DeserializeObject<OAuthTokenJson>(getRequest("https://open.spotify.com/token")).t;
        }
        private string GetCSRFToken()
        {
            return JsonConvert.DeserializeObject<CSRFTokenJson>(getRequest(hostname+"/simplecsrf/token.json")).token;
        }

        class OAuthTokenJson
        {
            public string t;
        }
        class CSRFTokenJson
        {
            public string token;
        }
    }
}
