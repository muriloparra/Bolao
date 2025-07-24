
using Bolao.API.Models;

namespace Bolao.API.Services;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<string>> LogoutAsync();
    Task<ApiResponse<string>> ForgetPasswordAsync(ForgetPasswordRequest request);
    Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequest request);
}
