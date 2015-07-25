namespace Uptred.Shell
{
    public static class Constants
    {
        const string _VimeoAPIKey = "YOUR_CLIENT_ID";
        const string _VimeoAPISecret = "YOUR_CLIENT_SECRET";
        const string _VimeoRedirectURL = "http://callback.uptred.com";

        const string _YouTubeAPIKey = "YOUR_CLIENT_ID";
        const string _YouTubeAPISecret = "YOUR_CLIENT_SECRET";
        const string _YouTubeRedirectURL = "urn:ietf:wg:oauth:2.0:oob";
        
        public const string _DEFAULT = "DEFAULT";

        public static string VimeoAPIKey = _DEFAULT;
        public static string VimeoAPISecret = _DEFAULT;
        public static string VimeoRedirectURL = _DEFAULT;
        public static string YouTubeAPIKey = _DEFAULT;
        public static string YouTubeAPISecret = _DEFAULT;
        public static string YouTubeRedirectURL = _DEFAULT;

        public static string GetVimeoAPIKey()
        {
            if (VimeoAPIKey == _DEFAULT) return _VimeoAPIKey;
            return VimeoAPIKey;
        }

        public static string GetVimeoAPISecret()
        {
            if (VimeoAPISecret == _DEFAULT) return _VimeoAPISecret;
            return VimeoAPISecret;
        }

        public static string GetVimeoRedirectURL()
        {
            if (VimeoRedirectURL == _DEFAULT) return _VimeoRedirectURL;
            return VimeoRedirectURL;
        }

        public static string GetYouTubeAPIKey()
        {
            if (YouTubeAPIKey == _DEFAULT) return _YouTubeAPIKey;
            return YouTubeAPIKey;
        }

        public static string GetYouTubeAPISecret()
        {
            if (YouTubeAPISecret == _DEFAULT) return _YouTubeAPISecret;
            return YouTubeAPISecret;
        }

        public static string GetYouTubeRedirectURL()
        {
            if (YouTubeRedirectURL == _DEFAULT) return _YouTubeRedirectURL;
            return YouTubeRedirectURL;
        }
        
        public static void LoadSettings()
        {
            VimeoAPIKey = Settings.Default.VimeoAPIKey;
            VimeoAPISecret = Settings.Default.VimeoAPISecret;
            VimeoRedirectURL = Settings.Default.VimeoRedirectURL;
            YouTubeAPIKey = Settings.Default.YouTubeAPIKey;
            YouTubeAPISecret = Settings.Default.YouTubeAPISecret;
            YouTubeRedirectURL = Settings.Default.YouTubeRedirectURL;
        }

        public static void SaveSettings()
        {
            Settings.Default.VimeoAPIKey = VimeoAPIKey;
            Settings.Default.VimeoAPISecret = VimeoAPISecret;
            Settings.Default.VimeoRedirectURL = VimeoRedirectURL;
            Settings.Default.YouTubeAPIKey = YouTubeAPIKey;
            Settings.Default.YouTubeAPISecret = YouTubeAPISecret;
            Settings.Default.YouTubeRedirectURL = YouTubeRedirectURL;
            Settings.Default.Save();
        }
    }
}
