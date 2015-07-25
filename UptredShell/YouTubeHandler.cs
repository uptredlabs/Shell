using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Uptred.YouTube;

namespace Uptred.Shell
{
    public static partial class YouTubeHandler
    {
        static YouTubeHook yc = null;
        static YouTubeUploadInformation info = new YouTubeUploadInformation();

        const string QUEUE_FILENAME = "youtube.resume";

        static bool login = false;

        static Dictionary<string, Action<Queue<string>>> modules = new Dictionary<string, Action<Queue<string>>>();
        static Dictionary<string, string> helps = new Dictionary<string, string>();

        static void AddCommand(string name, string help, Action<Queue<string>> module)
        {
            modules[name.ToLower()] = module;
            helps[name.ToLower()] = help;
        }

        public static void YouTubeMain(Queue<string> parameters)
        {
            try
            {
                Login();
            }
            catch
            {
                App.Info("Error logging in. Type 'auth' to (re)authorize.");
            }
            AddCommand("verbose", "Verbose: Shows additional debug information.", App.FlipVerbose);
            AddCommand("clientid", "ClientId <clientid>: Sets the API Key.", SetClientId);
            AddCommand("clientsecret", "ClientSecret <clientsecret>: Sets the API Secret.", SetClientSecret);
            AddCommand("redirecturl", "RedirectUrl <redirecturl>: Sets the API Redirect URL.", SetRedirectUrl);
            AddCommand("reset", "Reset: resets all stored configuration.", ResetKeys);
            AddCommand("refreshtoken", "RefreshToken <refreshtoken>: Uses a previously obtained refresh token to login.", SetRefreshToken);
            AddCommand("auth", "Auth [ie]: Goes through the authorization process. Use the ie parameter to automatically open an Internet Explorer window for the login (Windows only).", Auth);
            AddCommand("login", "Login: Uses the current refreshtoken to login. You can also pass an auth_code as an argument for a fresh authorization: Login <auth_code> (Step 2 of fresh authorization.)", Login);
            AddCommand("listcategories", "ListCategories: Lists all YouTube categories.", ListCategories);
            AddCommand("resume", "Resume: Tries to resume a previous upload.", Resume);
            AddCommand("title", "Title <\"Video Title\">: Sets the video title.", (p) => info.Meta.Title = p.Dequeue());
            AddCommand("description", "Description <\"Video Description\">: Sets the video description.", (p) => info.Meta.Description = p.Dequeue());
            AddCommand("tags", "Tags <\"tag1, tag2, ...\">: Sets the video tags.", (p) => info.Meta.Tags = p.Dequeue());
            AddCommand("category", "Category <categoryid>: Sets the video category. See ListCategories for more information.", (p) => info.Meta.CategoryId = int.Parse(p.Dequeue()));
            AddCommand("privacy", "Privacy <privacystatus>: Sets the video privacy state.", (p) => info.Meta.PrivacyStatus = p.Dequeue());
            AddCommand("embeddable", "Embeddable <true/false>: Sets the video embeddable tag.", (p) => info.Meta.Embeddable = bool.Parse(p.Dequeue()));
            AddCommand("license", "License <license>: Sets the video license.", (p) => info.Meta.License = p.Dequeue());
            AddCommand("chunksize", "ChunkSize <integer larger than 1048576>: Sets the upload chunk size.", (p) => info.ChunkSize = int.Parse(p.Dequeue()));
            AddCommand("buffersize", "BufferSize <positive integer smaller than ChunkSize>: Sets the intermediate buffer size. Low values make progressbar look smoother, but increase overhead.", (p) => Core.UpstreamCallbackRate = uint.Parse(p.Dequeue()));
            AddCommand("maxattempts", "MaxAttempts <integer>: Sets the maximum attempts for retrying upload.", (p) => info.MaxAttempts = int.Parse(p.Dequeue()));
            AddCommand("url", "Url <\"url\">: Manually sets the upload endpoint.", (p) => info.Url = p.Dequeue());
            AddCommand("upload", "Upload [\"path\"]: Begins uploading a file", Upload);
            AddCommand("help", "Help: Shows the available commands", Help);
            AddCommand("display", "Display <User/Video/Keys>: Displays information regarding the logged in user, stored video details or API keys.", Display);
            AddCommand("request", "Request <...>: Sends a custom API request. Try Request Help for more info.", Request);
            AddCommand("getloginurl", "GetLoginUrl: Generates and prints an Auth URL. (Step 1 of fresh authorization.)", (p) =>
                App.Out(YouTubeHook.GetLoginURL(
                clientId: Constants.GetYouTubeAPIKey(),
                redirect: Constants.GetYouTubeRedirectURL())));

            if (parameters.Count == 0)
            {
                App.Info("Uptred YouTube module interactive mode. Type 'help' for Help, 'pop' to Exit.");
                while (true)
                {
                    Console.Write("Uptred\\YouTube> ");
                    string command = Console.ReadLine().Trim();

                    var p = App.Str2Args(command);
                    if (p.Count > 0 && new string[]{"pop","exit","quit"}.Contains(p.First().ToLower())) break;
                    Interpret(p);
                }
            }
            else Interpret(parameters);
        }

        static void Interpret(Queue<string> parameters)
        {
            try
            {
                while (parameters.Count > 0)
                {
                    var module = parameters.Dequeue().ToLower();
                    if (modules.ContainsKey(module)) modules[module](parameters);
                    else App.Error(String.Format("Incorrect parameter: {0}", module));
                }
            }
            catch (Exception e)
            {
                App.PrintException(e);
            }
        }

        public static void Help(Queue<string> parameters)
        {
            foreach (var key in helps)
            {
                App.Out(key.Value);
            }
        }
        
        public static void SetClientId(Queue<string> parameters)
        {
            Constants.YouTubeAPIKey = parameters.Dequeue();
            Constants.SaveSettings();
        }

        public static void SetClientSecret(Queue<string> parameters)
        {
            Constants.YouTubeAPISecret = parameters.Dequeue();
            Constants.SaveSettings();
        }

        public static void SetRedirectUrl(Queue<string> parameters)
        {
            Constants.YouTubeRedirectURL = parameters.Dequeue();
            Constants.SaveSettings();
        }

        public static void ResetKeys(Queue<string> parameters)
        {
            Settings.Default.Reset();
            Settings.Default.Save();
            Constants.LoadSettings();
        }

        public static void SetRefreshToken(Queue<string> parameters)
        {
            Settings.Default.YouTubeRefresh = parameters.Dequeue();
            Settings.Default.Save();
        }

        public static void Login(Queue<string> parameters)
        {
            string authcode = "";
            if (parameters.Count > 0) authcode = parameters.Dequeue();

            if (string.IsNullOrWhiteSpace(authcode))
            {
                login = false;
                Login();
                return;
            }

            try
            {
                yc = YouTubeHook.Authorize(
                    authCode: authcode,
                    clientId: Constants.GetYouTubeAPIKey(),
                    secret: Constants.GetYouTubeAPISecret(),
                    redirect: Constants.GetYouTubeRedirectURL());

                Settings.Default.YouTubeRefresh = yc.RefreshToken;
                Settings.Default.Save();
                App.Info("Logged in as " + yc.DisplayName);
                login = true;
            }
            catch
            {
                App.Error("Error logging in.");
                return;
            }
        }

        public static void Auth(Queue<string> parameters)
        {
            var url = YouTubeHook.GetLoginURL(
                clientId: Constants.GetYouTubeAPIKey(),
                redirect: Constants.GetYouTubeRedirectURL());
            App.Info("Open this URL and follow the instructions:");
            App.Info(url);
            if (parameters.Count > 0 && parameters.First().ToLower() == "ie")
            {
                parameters.Dequeue();
                var psi = new ProcessStartInfo("iexplore.exe");
                psi.Arguments = url;
                Process.Start(psi);
            }
            Console.Write("Enter authCode> ");
            var authcode = Console.ReadLine();

            try
            {
                yc = YouTubeHook.Authorize(
                    authCode: authcode,
                    clientId: Constants.GetYouTubeAPIKey(),
                    secret: Constants.GetYouTubeAPISecret(),
                    redirect: Constants.GetYouTubeRedirectURL());

                Settings.Default.YouTubeRefresh = yc.RefreshToken;
                Settings.Default.Save();
                App.Info("Logged in as " + yc.DisplayName);
                login = true;
            }
            catch
            {
                App.Error("Error logging in.");
                return;
            }
        }

        public static bool Login()
        {
            try
            {
                if (!login)
                {
                    yc = YouTubeHook.ReAuthorize(
                        refreshToken: Settings.Default.YouTubeRefresh,
                        clientId: Constants.GetYouTubeAPIKey(),
                        secret: Constants.GetYouTubeAPISecret(),
                        redirect: Constants.GetYouTubeRedirectURL());
                    App.Info("Logged in as " + yc.DisplayName);
                    login = true;
                }
            }
            catch
            {
                App.Error("Error logging in.");
                return false;
            }
            return login;
        }

        public static void ListCategories(Queue<string> parameters)
        {
            Login();
            yc.FillCategories();
            Console.ResetColor();
            foreach (var cat in yc.Categories)
                App.Out(string.Format("ID:{0}\tName:{1}", cat.Key, cat.Value));
        }

        static void Save(YouTubeUploadInformation info)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(QUEUE_FILENAME, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, info);
            stream.Close();
        }
        
        public static void Resume(Queue<string> parameters)
        {
            Login();
            if (!File.Exists(QUEUE_FILENAME))
            {
                App.Error("Resume file not found.");
                return;
            }
            App.Verbose("Reading resume information file...");
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(QUEUE_FILENAME, FileMode.Open, FileAccess.Read, FileShare.Read);
            info = (YouTubeUploadInformation)formatter.Deserialize(stream);
            stream.Close();
        }

        public static void Upload(Queue<string> parameters)
        {
            Login();
            //If no parameters, it's ...resume upload
            //If a parameter, it's the new file name: ...upload "path"
            if (parameters.Count > 0) info.Path = parameters.Dequeue();
            FileInfo file = new FileInfo(info.Path);
            if (!file.Exists)
            {
                App.Error(string.Format("Path does not exist: {0}", info.Path));
                return;
            }
            
            yc.UploadCallback = (feedback) =>
            {
                info.LastByte = feedback.LastByte;
                if (App.UseProgressBar)
                {
                    try
                    {
                        App.DrawProgressBar(
                            feedback.LastByte,
                            feedback.ContentSize,
                            (int)(Console.BufferWidth * 0.6),
                            '♦');
                    }
                    catch
                    {
                        App.Info(string.Format("{0}/{1} bytes uploaded.", feedback.LastByte, feedback.ContentSize));
                    }
                }
                else
                {
                    App.Info(string.Format("{0}/{1} bytes uploaded.", feedback.LastByte, feedback.ContentSize));
                }
                
                Save(info);
            };
            
            if (info.Url == null)
            {
                App.Info("Getting Upload URL...");
                info.Url = yc.GetUploadSessionUrl(info.Path);
            }
            App.Info("Uploading Video File...");
            Core.UpstreamCallback = (last, total) =>
            {
                if (App.UseProgressBar)
                {
                    try
                    {
                        App.DrawProgressBar(
                            info.LastByte + last,
                            file.Length,
                            (int)(Console.BufferWidth * 0.6),
                            '♦');
                    }
                    catch
                    {

                    }
                }
            };
            var video_id = yc.Upload(info.Path, info.Url, info.ChunkSize,
                info.MaxAttempts, info.LastByte);
            Core.UpstreamCallback = null;
            Save(info);
            
            if (string.IsNullOrWhiteSpace(video_id))
            {
                App.Error("Upload failed. Retry with <resume upload>");
                return;
            }

            App.DrawProgressBar(100, 100, (int)(Console.BufferWidth * 0.6), '♦');
            App.Info(" ");
            App.Info("Applying Metadata...");
            yc.ApplyVideoMetadata(video_id, info.Meta);
            App.Success("All Done!");
            File.Delete(QUEUE_FILENAME);
            App.Out(video_id);
        }

        public static void Display(Queue<string> parameters)
        {
            if (parameters.Count == 0)
            {
                App.Info(
                    "Type <Display User> to print user information.\n" +
                    "<Display Video> to print video information.\n" +
                    "<Display Keys> to print API keys.");
                return;
            }
            var mode = parameters.Dequeue().ToLower();
            if (mode == "user")
            {
                App.Info(yc.Me == null ? "User data is null." : yc.Me);
            }
            else if (mode == "keys")
            {
                App.Info("Client ID: " + Constants.YouTubeAPIKey);
                App.Info("Client Secret: " + Constants.YouTubeAPISecret);
                App.Info("Redirect URL: " + Constants.YouTubeRedirectURL);
                if (yc != null)
                {
                    App.Info("Refresh Token: " + (yc.RefreshToken == null ? "null" : yc.RefreshToken));
                }
                else
                {
                    App.Info("No refresh token stored.");
                }
            }
            else if (mode == "video")
            {
                App.Info(info.ToString());
            }
        }
    }
}
