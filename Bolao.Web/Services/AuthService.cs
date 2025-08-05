
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using Bolao.Web.Models;

namespace Bolao.Web.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private UserInfo? _currentUser;
    private bool _initialized = false;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public UserInfo? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            var name = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userName");
            var email = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userEmail");

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(email))
            {
                _currentUser = new UserInfo
                {
                    Name = name,
                    Email = email,
                    Token = token
                };
            }
        }
        catch
        {
            // Falha ao acessar localStorage, usuário não autenticado
        }

        _initialized = true;
    }

    public async Task<(bool Success, string Message)> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("login", request);
            var content = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<AuthResponse>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                _currentUser = new UserInfo
                {
                    Name = apiResponse.Data.Name,
                    Email = apiResponse.Data.Email,
                    Token = apiResponse.Data.Token
                };

                // Persiste os dados no localStorage
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", _currentUser.Token);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userName", _currentUser.Name);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userEmail", _currentUser.Email);

                return (true, "Login realizado com sucesso!");
            }
            else
            {
                var errorMessage = apiResponse?.Message ?? "Erro desconhecido";
                if (apiResponse?.Errors?.Any() == true)
                {
                    errorMessage = string.Join(", ", apiResponse.Errors);
                }
                return (false, errorMessage);
            }
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        _currentUser = null;

        // Remove dados do localStorage
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userName");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userEmail");
    }
}
