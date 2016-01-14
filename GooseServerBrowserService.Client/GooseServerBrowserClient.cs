using GooseServerBrowserService.Contract;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GooseServerBrowserService.Client
{
    public class GooseServerBrowserClient
    {
        string baseUrl;

        public GooseServerBrowserClient(string baseUrl)
        {
            this.baseUrl = baseUrl.TrimEnd('/') + '/';
        }

        public void Register(RegisterRequest request)
        {
            request.Checksum = request.ComputeChecksum();

            var httpClient = new HttpClient();
            var result = httpClient.PostAsync(baseUrl + "api/ServerBrowser/Register", new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")).Result;
        }
    }
}
