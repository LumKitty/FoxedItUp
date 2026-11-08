using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static FoxedItUp.Functions;
using static FoxedItUp._Settings;

namespace FoxedItUp {
    internal static class HTTPServer {
        private static HttpListener Listener;
        private static Task ListenerTask;
        private static bool StopServer = false;

        internal static void Init(int Port) {
            string Binding = "http://localhost:" + Port.ToString() + "/";
            Listener = new HttpListener();
            Log("Attempting to bind to URL: " + Binding);
            Listener.Prefixes.Add(Binding);
            Log("Starting HTTP server");
            Start();
        }

        internal static void Start() {
            Listener.Start();
            StopServer = false;
            ListenerTask = Task.Run(async () => { await HandleHttpRequests(); });
        }

        internal static void Stop() {
            StopServer = true;
            ListenerTask.Dispose();
            Listener.Stop();
        }

        private static async Task HandleHttpRequests() {
            while (!StopServer) {
                string Body = "";
                HttpListenerContext Context = await Listener.GetContextAsync();
                var Request = Context.Request;
                int Result = 404;

                Log($"HTTP Response: [{Request.HttpMethod}] {Request.Url.AbsolutePath}");
                if (Request.HasEntityBody) {
                    System.IO.StreamReader Reader = new System.IO.StreamReader(Request.InputStream, Request.ContentEncoding);
                    Body = await Reader.ReadToEndAsync();
                    Request.InputStream.Close();
                    Reader.Close();
                    Log(Body);
                }

                if (Request.HttpMethod.ToLower() == "post") {
                    switch (Request.Url.AbsolutePath.ToLower()) {
                        case "/alt/chat":
                            AltStream.SendChat(Body);
                            Result = 200;
                            break;
                        case "/alt/shoutout":
                            AltStream.ShoutOut(Body);
                            Result = 200;
                            break;
                    }
                }

                Context.Response.StatusCode = Result;
                Context.Response.Close();

                if (Settings.ShowRandomJSON) {
                    //Log(Newtonsoft.Json.JsonConvert.SerializeObject(request));
                }

            }
        }
    }
}
