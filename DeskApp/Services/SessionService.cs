using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DeskApp.Models;

namespace DeskApp.Services
{
    public class SessionService
    {
        private static SessionService? _instance;
        private static readonly object _lock = new object();
        private const string SessionFileName = "session.dat";
        private static readonly string SessionFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeskApp",
            SessionFileName
        );

        public bool IsAuthenticated { get; private set; }
        public string? Token { get; private set; }
        public UserData? CurrentUser { get; private set; }

        private SessionService()
        {
            IsAuthenticated = false;
            LoadSessionFromFile();
        }

        public static SessionService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SessionService();
                        }
                    }
                }
                return _instance;
            }
        }

        public void SetSession(string token, UserData user)
        {
            Token = token;
            CurrentUser = user;
            IsAuthenticated = true;
            SaveSessionToFile();
        }

        public void ClearSession()
        {
            Token = null;
            CurrentUser = null;
            IsAuthenticated = false;
            DeleteSessionFile();
        }

        public string GetAuthorizationHeader()
        {
            return IsAuthenticated && !string.IsNullOrEmpty(Token) 
                ? $"Bearer {Token}" 
                : string.Empty;
        }

        private void SaveSessionToFile()
        {
            try
            {
                var sessionData = new SessionData
                {
                    Token = Token,
                    User = CurrentUser
                };

                var directory = Path.GetDirectoryName(SessionFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(sessionData);
                var encrypted = EncryptString(json);
                File.WriteAllText(SessionFilePath, encrypted);
            }
            catch (Exception ex)
            {
                // En caso de error al guardar, continuar sin persistencia
                System.Diagnostics.Debug.WriteLine($"Error guardando sesión: {ex.Message}");
            }
        }

        private void LoadSessionFromFile()
        {
            try
            {
                if (File.Exists(SessionFilePath))
                {
                    var encrypted = File.ReadAllText(SessionFilePath);
                    var json = DecryptString(encrypted);
                    var sessionData = JsonSerializer.Deserialize<SessionData>(json);

                    if (sessionData != null && !string.IsNullOrEmpty(sessionData.Token))
                    {
                        Token = sessionData.Token;
                        CurrentUser = sessionData.User;
                        IsAuthenticated = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Si hay error al cargar, continuar sin sesión
                System.Diagnostics.Debug.WriteLine($"Error cargando sesión: {ex.Message}");
                DeleteSessionFile();
            }
        }

        private void DeleteSessionFile()
        {
            try
            {
                if (File.Exists(SessionFilePath))
                {
                    File.Delete(SessionFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error eliminando sesión: {ex.Message}");
            }
        }

        private string EncryptString(string plainText)
        {
            // Usar ProtectedData para encriptar (solo Windows)
            try
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var encryptedBytes = ProtectedData.Protect(
                    plainBytes,
                    null,
                    DataProtectionScope.CurrentUser
                );
                return Convert.ToBase64String(encryptedBytes);
            }
            catch
            {
                // Si falla, usar Base64 simple (menos seguro pero funcional)
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
            }
        }

        private string DecryptString(string encryptedText)
        {
            try
            {
                var encryptedBytes = Convert.FromBase64String(encryptedText);
                var plainBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    null,
                    DataProtectionScope.CurrentUser
                );
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                // Si falla, intentar decodificar Base64 simple
                return Encoding.UTF8.GetString(Convert.FromBase64String(encryptedText));
            }
        }

        private class SessionData
        {
            public string? Token { get; set; }
            public UserData? User { get; set; }
        }
    }
}
