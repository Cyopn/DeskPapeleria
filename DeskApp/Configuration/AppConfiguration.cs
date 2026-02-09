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
            // Valores por defecto desde appsettings.json
            ApiBaseUrl = "https://noninitial-chirurgical-judah.ngrok-free.dev/api";
            UsersEndpoint = "/users/";
            LoginEndpoint = "/users/login";

            // También puedes leer desde variables de entorno si están configuradas
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
