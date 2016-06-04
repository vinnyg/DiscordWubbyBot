using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using Newtonsoft.Json;

namespace DiscordSharpTest
{
    [Obsolete]
    class TwitterClient
    {
        public string OAuthConsumerKey { get; set; }
        public string OAuthConsumerSecret { get; set; }

        public async Task<string> GetAccessToken()
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.twitter.com/oauth2/token");

            var info = Convert.ToBase64String(new UTF8Encoding().GetBytes(OAuthConsumerKey + ":" + OAuthConsumerSecret));
            request.Headers.Add("Authorization", "Basic " + info);
            request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

            //Get response contents and deserialize
            HttpResponseMessage response = await client.SendAsync(request);
            string jsonResponse = await response.Content.ReadAsStringAsync();

            //Return deserialized results
            return JsonConvert.DeserializeObject<string>(jsonResponse);
        }
    }
}
