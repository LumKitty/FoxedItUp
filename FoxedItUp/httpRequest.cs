using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FoxedItUp.Functions;
using AltStreamLib;
using System.Net.Http;

namespace FoxedItUp {
    internal static class httpRequest {
        private static HttpClient client = new HttpClient();
        internal static async Task<string> HttpRequest(string Method, string URL, string Content) {
            try {
                var jsonData = new StringContent(Content, Encoding.ASCII);
                jsonData.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                string Response = "";
                int httpStatus = 0;

                switch (Method) {
                    case "POST":
                        //Log("POST: " + URL + " : " + Content);
                        var PostResult = await client.PostAsync(URL, jsonData);
                        Response = PostResult.Content.ReadAsStringAsync().Result;
                        httpStatus = ((int)PostResult.StatusCode);
                        //Log(PostResult.ToString());
                        PostResult.Dispose();
                        break;
                    case "PUT":
                        //Log("PUT: " + URL + " : " + Content);
                        var PutResult = await client.PutAsync(URL, jsonData);
                        Response = PutResult.Content.ReadAsStringAsync().Result;
                        httpStatus = ((int)PutResult.StatusCode);
                        PutResult.Dispose();
                        break;
                    case "GET":
                        //Log("GET: " + URL + " : " + Content);
                        var GetResult = await client.GetAsync(URL);
                        Response = GetResult.Content.ReadAsStringAsync().Result;
                        httpStatus = ((int)GetResult.StatusCode);
                        GetResult.Dispose();
                        break;
                    case "PATCH":
                        Log("PATCH: " + URL + " : " + Content);
                        var request = new HttpRequestMessage(new HttpMethod("PATCH"), URL);
                        request.Content = jsonData;
                        var PatchResult = await client.SendAsync(request);
                        Response = PatchResult.Content.ReadAsStringAsync().Result;
                        httpStatus = ((int)PatchResult.StatusCode);
                        PatchResult.Dispose();
                        break;
                }
                //Log("HTTP status: " + httpStatus + " - Response: " + Response);


                return Response;
            } catch (Exception e) {
                Log(e.ToString());
                return "";
            }
        }
    }
}
