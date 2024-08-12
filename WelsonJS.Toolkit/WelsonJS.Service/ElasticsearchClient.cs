// ElasticsearchClient.cs
// https://github.com/gnh1201/welsonjs
using RestSharp.Authenticators;
using RestSharp;

namespace WelsonJS.Service
{
    public class ElasticsearchClient
    {
        private readonly RestClient client;

        public ElasticsearchClient(string baseUrl, string username, string password)
        {
            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new HttpBasicAuthenticator(username, password)
            };
            client = new RestClient(options);
        }

        public RestResponse IndexDocument(string indexName, object document)
        {
            var request = new RestRequest($"/{indexName}/_doc", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(document);

            return client.Execute(request);
        }
    }
}
