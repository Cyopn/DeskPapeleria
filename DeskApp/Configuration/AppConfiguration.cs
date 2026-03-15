using System;

namespace DeskApp.Configuration
{
    public class AppConfiguration
    {
        private static AppConfiguration? _instance;
        private static readonly object _lock = new object();

        public string ApiBaseUrl { get; private set; }
        public string UsersEndpoint { get; private set; }
        public string LoginEndpoint { get; private set; }

        private AppConfiguration()
        {
            ApiBaseUrl = "https://1e8d-2806-265-b4a2-1935-9a28-a6ff-fe02-9d72.ngrok-free.app/api";
            UsersEndpoint = "/users/";
            LoginEndpoint = "/users/login";

            var envApiUrl = Environment.GetEnvironmentVariable("DESK_API_URL");
            if (!string.IsNullOrEmpty(envApiUrl))
            {
                ApiBaseUrl = envApiUrl;
            }
        }

        public static AppConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new AppConfiguration();
                        }
                    }
                }
                return _instance;
            }
        }

        public string GetFullUrl(string endpoint)
        {
            return $"{ApiBaseUrl}{endpoint}";
        }
    }
}
