using System;
using System.Collections.Generic;
using System.Linq;
using Uptred.YouTube;
using Uptred.Vimeo;
using System.Net;

namespace Uptred.Shell
{
    public static class App
    {
        static Stack<ConsoleColor> colors = new Stack<ConsoleColor>();
        public static bool UseProgressBar = true;
        public static bool CausedError = false;
        public static bool IsSilent = false;
        public static bool IsVerbose
        {
            get
            {
                return YouTubeHook.VerboseCallback != null && VimeoHook.VerboseCallback != null;
            }
            set
            {
                if (value)
                {
                    YouTubeHook.VerboseCallback = App.Verbose;
                    VimeoHook.VerboseCallback = App.Verbose;
                }
                else
                {
                    YouTubeHook.VerboseCallback = null;
                    VimeoHook.VerboseCallback = null;
                }
            }
        }

        public static void PopColor()
        {
#if WINDOWS || WIN32
            if (colors.Count == 0) Console.ResetColor();
            else Console.ForegroundColor = colors.Pop();
#endif
        }

        public static void PushColor(ConsoleColor color)
        {
#if WINDOWS || WIN32
            colors.Push(Console.ForegroundColor);
            Console.ForegroundColor = color;
#endif
        }

        public static void FlipVerbose(Queue<string> parameters)
        {
            App.IsVerbose = !App.IsVerbose;
            App.Info("Verbose " + (App.IsVerbose ? "On." : "Off."));
        }
        
        public static void DrawProgressBar(long complete, long maxVal, int barSize, char progressCharacter)
        {
            if (IsSilent) return;
            if (CausedError) return;
            try
            {
                Console.CursorVisible = false;
                int left = Console.CursorLeft;
                decimal perc = (decimal)complete / (decimal)maxVal;
                int chars = (int)Math.Floor(perc / ((decimal)1 / (decimal)barSize));
                string p1 = String.Empty, p2 = String.Empty;

                for (int i = 0; i < chars; i++) p1 += progressCharacter;
                for (int i = 0; i < barSize - chars; i++) p2 += progressCharacter;

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(p1);
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write(p2);

                Console.ResetColor();
                Console.Write(" {0}%      ", (perc * 100).ToString("N2"));
                Console.CursorLeft = left;
            }
            catch
            {
                CausedError = true;
                Console.WriteLine("Cannot draw a progressbar. Will not draw a progressbar.");
                UseProgressBar = false;
            }
        }

        public static void Error(string message)
        {
            throw new Exception(message);
        }

        public static void Success(string message)
        {
            if (IsSilent) return;
            Console.WriteLine(message);
        }

        public static void Info(string message)
        {
            if (IsSilent) return;
            Console.WriteLine(message);
        }

        public static void Out(string message)
        {
            Console.WriteLine(message);
        }

        public static void Verbose(string message)
        {
            Console.WriteLine(message);
        }
        
        public static void Help(Queue<string> parameters)
        {
            Console.WriteLine("youtube: Starts the YouTube module.");
            Console.WriteLine("vimeo: Starts the Vimeo module.");
            Console.WriteLine("verbose: Enables Verbose mode (Shows communications with the server.)");
            Console.WriteLine("silent: Only prints command outputs. Use for scripting.");
            Console.WriteLine("request <...>: Sends a custom API request. Try Request Help for more info.");
        }

        static Dictionary<string, Action<Queue<string>>> actions =
            new Dictionary<string, Action<Queue<string>>>()
        {
            { "youtube", YouTubeHandler.YouTubeMain },
            { "vimeo", VimeoHandler.VimeoMain },
            { "request", Request },
            { "verbose", FlipVerbose },
            { "silent", (p) => IsSilent = !IsSilent },
            { "help", Help }
        };

        public static void PrintException(Exception e)
        {
            PushColor(ConsoleColor.Red);
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            PushColor(ConsoleColor.DarkRed);
            Console.WriteLine(e.InnerException);
            PopColor();
            PopColor();
        }

        public static void Main(string[] args)
        {
            IsVerbose = false;
            Shell.Constants.LoadSettings();

            RequestConfig.Init();
            Shell.YouTubeHandler.RequestConfig.Init();
            Shell.VimeoHandler.RequestConfig.Init();
            //File name is not a parameter. First parameter has index 0.
            //If quotes are used, they are automatically packed in one arg.
            Queue<string> parameters = new Queue<string>(args.ToList());
            if (parameters.Count == 0)
            {
                Info("This is the Uptred Interactive Mode. Type 'help' for Help, 'pop' for Exit.");
                while (true)
                {
                    Console.Write("Uptred> ");
                    string command = Console.ReadLine().Trim();
                    var p = App.Str2Args(command);
                    if (p.Count > 0 && new string[] { "pop", "exit", "quit" }.Contains(p.First().ToLower())) break;
                    Interpret(p);
                }
            }
            else Interpret(parameters);
        }

        public static void Interpret(Queue<string> parameters)
        {
            try
            {
                while (parameters.Count > 0)
                {
                    var module = parameters.Dequeue().ToLower();
                    if (actions.ContainsKey(module)) actions[module](parameters);
                    else App.Error(String.Format("Incorrect parameter: {0}", module));
                }
            }
            catch (Exception e)
            {
                App.PrintException(e);
            }
        }

        public static void Request(Queue<string> parameters)
        {
            //Request Entry Point
            if (parameters.Count == 0)
            {
                App.Info("Uptred generic request module interactive mode. " +
                    "Type 'help' for Help, 'pop' to Exit.");
                while (true)
                {
                    Console.Write("Uptred\\Request> ");
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

        class RequestConfig
        {
            public static string url = "";
            public static string method = "GET";
            public static bool jsonBody = false;
            public static string body = "";
            public static bool showHeaders = false;
            public static WebHeaderCollection headers = new WebHeaderCollection();
            public static Dictionary<string, Action<Queue<string>>> modules;
            public static Dictionary<string, string> helps;
            public static Dictionary<string, string> form = new Dictionary<string, string>();
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
                AddCommand("method", "Method <GET/PUT/POST/...>: Sets the HTTP method.", (p) => method = p.Dequeue().ToUpper());
                AddCommand("jsonbody", "JsonBody <true/false>: Sets the request content type to json.",
                        (p) => jsonBody = bool.Parse(p.Dequeue()));
                AddCommand("line", "Line <data>: Adds a line of content to the body.",
                    (p) =>
                    {
                        form.Clear();
                        if (string.IsNullOrWhiteSpace(body)) body = p.Dequeue();
                        else body += "\r\n" + p.Dequeue();
                    });
                AddCommand("form", "Form <key> <value>: Sets a value to a key in a multipart/form-data request.",
                    (p) =>
                    {
                        body = "";
                        form.Add(p.Dequeue(), p.Dequeue());
                    });
                AddCommand("header", "Header <data>: Adds a request header.",
                    (p) =>
                    {
                        headers.Add(p.Dequeue());
                    });
                AddCommand("clear", "Clear: Clears the request data.",
                    (p) =>
                    {
                        url = "";
                        method = "GET";
                        jsonBody = false;
                        body = "";
                        form.Clear();
                        headers.Clear();
                    });
                AddCommand("display", "Print: Prints the request data.",
                    (p) =>
                    {
                        App.Out("Url: " + url);
                        App.Out("Method: " + method);
                        App.Out("JsonBody: " + jsonBody);
                        App.Out("Body: " + body);
                        App.Out("Form: ");
                        foreach (var kv in form) App.Out(string.Format("{0}={1}", kv.Key, kv.Value));
                        App.Out("Headers:"); 
                        foreach (string header in headers) App.Out(header);
                        App.Out("ShowHeaders: " + showHeaders);
                    });
                AddCommand("showheaders", "ShowHeaders: Enables showing response headers in the output.",
                    (p) =>
                    {
                        showHeaders = !showHeaders;
                    });
                AddCommand("go", "Go: Sends the request. (Only use this in interactive mode).", Go);
            }

            public static void Go(Queue<string> parameters)
            {
                string[] responseHeaders;
                string fetch = "";
                if (form.Count > 0) fetch = Core.RequestRaw(url, method, jsonBody, form, headers, out responseHeaders);
                else fetch = Core.RequestRaw(url, method, jsonBody, body, headers, out responseHeaders);
                if (showHeaders)
                {
                    App.Out("Headers:");
                    foreach (string header in responseHeaders) App.Out(header);
                    App.Out("Body:");
                }
                App.Out(fetch);
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

        public static Queue<string> Str2Args(string command)
        {
            Queue<string> p = new Queue<string>();
            string arg = "";
            bool quote = false;
            for (int i = 0; i < command.Length; i++)
            {
                if (command[i] == '"')
                {
                    quote = !quote;
                    if (!string.IsNullOrWhiteSpace(arg)) p.Enqueue(arg);
                    arg = "";
                }
                else if (command[i] == ' ' && !quote)
                {
                    if (!string.IsNullOrWhiteSpace(arg)) p.Enqueue(arg);
                    arg = "";
                }
                else arg += command[i];
            }
            if (!string.IsNullOrWhiteSpace(arg)) p.Enqueue(arg);
            return p;
        }
    }
}
