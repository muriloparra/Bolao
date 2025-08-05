
namespace Bolao.API.Models;

public record ApiResponse<T>(bool Success, string Message, T? Data = default, List<string>? Errors = null);

public record AuthResponse(string Token, string Email, string Name);

public record UserResponse(string Id, string Name, string Email, DateTime CreatedAt);

public record PaginatedResponse<T>(List<T> Items, int TotalCount, int Page, int PageSize, int TotalPages);
