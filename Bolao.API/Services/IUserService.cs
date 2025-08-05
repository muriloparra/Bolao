
using Bolao.API.Models;

namespace Bolao.API.Services;

public interface IUserService
{
    Task<ApiResponse<UserResponse>> CreateUserAsync(CreateUserRequest request);
    Task<ApiResponse<PaginatedResponse<UserResponse>>> GetUsersAsync(string? nome, int page, int pageSize);
    Task<ApiResponse<UserResponse>> GetUserByIdAsync(string id);
    Task<ApiResponse<string>> DeleteUserAsync(string id);
}
