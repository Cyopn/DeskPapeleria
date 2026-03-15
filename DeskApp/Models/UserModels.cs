using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeskApp.Models
{
    public class UserRegistrationRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("names")]
        public string Names { get; set; } = string.Empty;

        [JsonPropertyName("lastnames")]
        public string Lastnames { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("passwordConfirm")]
        public string PasswordConfirm { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = "employee";

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;
    }

    public class UserRegistrationResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public UserData? User { get; set; }
    }

    public class LoginRequest
    {
        [JsonPropertyName("identifier")]
        public string? Identifier { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public UserData? User { get; set; }
    }

    public class UserData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("id_user")]
        public int IdUser { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("names")]
        public string Names { get; set; } = string.Empty;

        [JsonPropertyName("lastnames")]
        public string Lastnames { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("file")]
        public System.Collections.Generic.List<FileData>? Files { get; set; }

        [JsonPropertyName("transactions")]
        public System.Collections.Generic.List<TransactionData>? Transactions { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class ApiErrorResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }
    }

    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string>? ValidationErrors { get; set; }
    }

    public class UserUpdateRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("names")]
        public string Names { get; set; } = string.Empty;

        [JsonPropertyName("lastnames")]
        public string Lastnames { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;
    }
}
