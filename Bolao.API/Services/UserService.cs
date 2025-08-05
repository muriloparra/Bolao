
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Bolao.API.Data;
using Bolao.API.Models;

namespace Bolao.API.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public UserService(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<ApiResponse<UserResponse>> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new ApiResponse<UserResponse>(false, "Email já está em uso", null, new List<string> { "Email já cadastrado" });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                Name = request.Name,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return new ApiResponse<UserResponse>(false, "Erro ao criar usuário", null, result.Errors.Select(e => e.Description).ToList());
            }

            var userResponse = new UserResponse(user.Id, user.Name, user.Email!, user.CreatedAt);
            return new ApiResponse<UserResponse>(true, "Usuário criado com sucesso", userResponse);
        }
        catch (Exception ex)
        {
            return new ApiResponse<UserResponse>(false, "Erro interno do servidor", null, new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<PaginatedResponse<UserResponse>>> GetUsersAsync(string? nome, int page, int pageSize)
    {
        try
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(nome))
            {
                query = query.Where(u => u.Name.Contains(nome));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserResponse(u.Id, u.Name, u.Email!, u.CreatedAt))
                .ToListAsync();

            var paginatedResponse = new PaginatedResponse<UserResponse>(users, totalCount, page, pageSize, totalPages);
            return new ApiResponse<PaginatedResponse<UserResponse>>(true, "Usuários recuperados com sucesso", paginatedResponse);
        }
        catch (Exception ex)
        {
            return new ApiResponse<PaginatedResponse<UserResponse>>(false, "Erro interno do servidor", null, new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<UserResponse>> GetUserByIdAsync(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return new ApiResponse<UserResponse>(false, "Usuário não encontrado", null, new List<string> { "Usuário não existe" });
            }

            var userResponse = new UserResponse(user.Id, user.Name, user.Email!, user.CreatedAt);
            return new ApiResponse<UserResponse>(true, "Usuário encontrado", userResponse);
        }
        catch (Exception ex)
        {
            return new ApiResponse<UserResponse>(false, "Erro interno do servidor", null, new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<string>> DeleteUserAsync(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return new ApiResponse<string>(false, "Usuário não encontrado", null, new List<string> { "Usuário não existe" });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return new ApiResponse<string>(false, "Erro ao deletar usuário", null, result.Errors.Select(e => e.Description).ToList());
            }

            return new ApiResponse<string>(true, "Usuário deletado com sucesso", "Usuário removido");
        }
        catch (Exception ex)
        {
            return new ApiResponse<string>(false, "Erro interno do servidor", null, new List<string> { ex.Message });
        }
    }
}
