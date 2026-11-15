using AltStreamLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static FoxedItUp._Settings;
using static FoxedItUp.Functions;

namespace FoxedItUp {
    internal static class httpRequest {
        private static HttpClient client = new HttpClient();
        internal static async Task<string> HttpRequest(string Method, string URL, string Content, string Authorization=null) {
            try {
                var jsonData = new StringContent(Content, Encoding.UTF8);
                jsonData.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                if (!String.IsNullOrEmpty(Authorization)) {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Authorization);
                }
                string Response = "";
                int httpStatus = 0;

                switch (Method) {
                    case "POST":
                        Log("POST: " + URL + " : " + Content, 4); 
                        var PostResult = await client.PostAsync(URL, jsonData);
                        Response = PostResult.Content.ReadAsStringAsync().Result;
                        httpStatus = ((int)PostResult.StatusCode);
                        //Log(PostResult.ToString());
                        PostResult.Dispose();
                        break;
                    case "PUT":
                        Log("PUT: " + URL + " : " + Content, 4); 
                        var PutResult = await client.PutAsync(URL, jsonData);
                        Response = PutResult.Content.ReadAsStringAsync().Result;
                        httpStatus = ((int)PutResult.StatusCode);
                        PutResult.Dispose();
                        break;
                    case "GET":
                        Log("GET: " + URL + " : " + Content, 4); 
                        var GetResult = await client.GetAsync(URL);
                        Response = GetResult.Content.ReadAsStringAsync().Result;
                        httpStatus = ((int)GetResult.StatusCode);
                        GetResult.Dispose();
                        break;
                    case "PATCH":
                        Log("PATCH: " + URL + " : " + Content, 4);
                        var request = new HttpRequestMessage(new HttpMethod("PATCH"), URL);
                        request.Content = jsonData;
                        var PatchResult = await client.SendAsync(request);
                        Response = PatchResult.Content.ReadAsStringAsync().Result;
                        httpStatus = ((int)PatchResult.StatusCode);
                        PatchResult.Dispose();
                        break;
                }
                if (Settings.LogLevel >= 5) { 
                    Log("HTTP status: " + httpStatus + " - Response: " + Response, 5); 
                } else {
                    Log("HTTP status: " + httpStatus, 4);
                }


                    return Response;
            } catch (Exception e) {
                Log(e.ToString(), 1);
                return "";
            }
        }
    }
}
