using System;
using System.Collections.Generic;
using System.Linq;

namespace Uptred.Shell
{
    public static partial class VimeoHandler
    {
        public static class RequestConfig
        {
            public static string url = "";
            public static string method = "GET";
            public static bool jsonBody = false;
            public static string body = "";
            public static Dictionary<string, Action<Queue<string>>> modules;
            public static Dictionary<string, string> helps;

            public static void AddCommand(string name, string help, Action<Queue<string>> module)
            {
                modules[name.ToLower()] = module;
                helps[name.ToLower()] = help;
            }

            public static void Init()
            {
                modules = new Dictionary<string, Action<Queue<string>>>();
                helps = new Dictionary<string, string>();
                AddCommand("help", "Help: Displays this text.", Help);
                AddCommand("url", "Url <\"url\">: Sets the endpoint.", (p) => url = p.Dequeue());
                AddCommand("method", "Method <GET/PUT/POST/...>: Sets the HTTP method.",
                    (p) => method = p.Dequeue().ToUpper());
                AddCommand("jsonbody", "JsonBody <true/false>: Sets the request content type to json.",
                    (p) => jsonBody = bool.Parse(p.Dequeue()));
                AddCommand("line", "Line <data>: Adds a line of content to the body.",
                    (p) =>
                    {
                        if (string.IsNullOrWhiteSpace(body)) body = p.Dequeue();
                        else body += "\r\n" + p.Dequeue();
                    });
                AddCommand("clear", "Clear: Clears the request data.",
                    (p) =>
                    {
                        url = "";
                        method = "GET";
                        jsonBody = false;
                        body = "";
                    });
                AddCommand("display", "Print: Prints the request data.",
                    (p) =>
                    {
                        App.Info("Url: " + url);
                        App.Info("Method: " + method);
                        App.Info("JsonBody: " + jsonBody);
                        App.Info("Body: " + body);
                    });
                AddCommand("go", "Go: Sends the request. (Only use this in interactive mode).", Go);
            }

            public static void Go(Queue<string> parameters)
            {
                Login();
                string[] responseHeaders;
                var r = vc.RequestRaw(url, method, jsonBody, body, out responseHeaders);
                App.Out("HEADERS");
                foreach (var header in responseHeaders) App.Out(header);
                App.Out("BODY");
                App.Out(r);
            }

            public static void Help(Queue<string> parameters)
            {
                foreach (var key in helps)
                {
                    App.Out(key.Value);
                }
            }

            public static void Interpret(Queue<string> parameters)
            {
                try
                {
                    while (parameters.Count > 0)
                    {
                        var module = parameters.Dequeue().ToLower();
                        if (module == "go") Go(parameters);
                        else if (modules.ContainsKey(module)) modules[module](parameters);
                        else App.Error(String.Format("Incorrect parameter: {0}", module));
                    }
                }
                catch (Exception e)
                {
                    App.PrintException(e);
                }
            }
        }

        //Entry point.
        public static void Request(Queue<string> parameters)
        {
#if DISABLE_CUSTOM_REQUESTS
            if (Constants.VimeoAPIKey == Constants._DEFAULT)
            {
                App.Error("To be able to send custom requests, you need to assign your own API keys.");
                return;
            }
#endif

            if (parameters.Count == 0)
            {
                App.Info("Uptred Vimeo Request Module Interactive Mode. Type 'help' For Help, 'pop' For Exit, 'go' To Send Request.");
                while (true)
                {
                    Console.Write("Uptred\\Vimeo\\Request> ");
                    string command = Console.ReadLine().Trim();
                    var p = App.Str2Args(command);

                    if (p.Count > 0 && new string[] { "pop", "exit", "quit" }.Contains(p.First().ToLower())) break;
                    RequestConfig.Interpret(p);
                }
            }
            else
            {
                RequestConfig.Interpret(parameters);
                RequestConfig.Go(parameters);
                parameters.Clear();
            }
        }
    }
}