using Newtonsoft.Json;
using RestSharp;
using System;
using System.Net;

namespace SitefinityWebApp.Custom.AlbumOptimization
{
    public class Kraken
    {
        private const string KrakenAPIUrl = "https://api.kraken.io/v1/";

        JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
        {
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
        };

        private RestClient _client;
        private KrakenAuth _auth;

        public Kraken(string key = "", string secret = "")
        {
            _auth = new KrakenAuth()
            {
                ApiKey = key,
                ApiSecret = secret
            };

            _client = new RestClient(KrakenAPIUrl);
        }

        public KrakenResponse Url(KrakenRequest kr)
        {
            var request = new RestRequest("url", Method.POST);

            kr.Auth = _auth;

            string json = JsonConvert.SerializeObject(kr, serializerSettings);

            request.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = Request(request);

            return response;
        }

        public KrakenResponse Upload(KrakenRequest kr, string fileName, string fileExtension)
        {
            var request = new RestRequest("upload", Method.POST);

            if (kr.File.Length == 0)
            {
                return new KrakenResponse()
                {
                    Success = false,
                    Error = "File parameter was not provided"
                };
            }

            kr.Auth = _auth;

            request.AddFile("image_bytes", kr.File, fileName + fileExtension);

            kr.File = null;

            string json = JsonConvert.SerializeObject(kr, serializerSettings);

            request.AddParameter("json", json);

            var result = Request(request);

            return result;
        }

        private KrakenResponse Request(RestRequest request)
        {
            var response = _client.Execute<KrakenResponse>(request);

            if (response.StatusCode != HttpStatusCode.OK) {
                throw new Exception();
            }

            return response.Data;
        }
    }

    public class KrakenAuth
    {
        [JsonProperty("api_key")]
        public string ApiKey { get; set; }

        [JsonProperty("api_secret")]
        public string ApiSecret { get; set; }
    }

    public class KrakenRequest
    {
        [JsonProperty("auth")]
        public KrakenAuth Auth { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("file")]
        public byte[] File { get; set; }

        [JsonProperty("wait")]
        public bool Wait { get; set; }

        [JsonProperty("callback_url")]
        public string CallbackUrl { get; set; }

        [JsonProperty("lossy")]
        public bool Lossy { get; set; }
    }

    public class KrakenResponse
    {
        public bool Success { get; set; }

        public string FileName { get; set; }

        public int OriginalSize { get; set; }

        public int KrakedSize { get; set; }

        public int SavedBytes { get; set; }

        public string KrakedUrl { get; set; }

        public string Error { get; set; }
    }
}