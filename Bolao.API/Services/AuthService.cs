
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bolao.API.Models;

namespace Bolao.API.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new ApiResponse<AuthResponse>(false, "Email já está em uso", null, new List<string> { "Email já cadastrado" });
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
                return new ApiResponse<AuthResponse>(false, "Erro ao criar usuário", null, result.Errors.Select(e => e.Description).ToList());
            }

            var token = GenerateJwtToken(user);
            var authResponse = new AuthResponse(token, user.Email!, user.Name);

            return new ApiResponse<AuthResponse>(true, "Usuário registrado com sucesso", authResponse);
        }
        catch (Exception ex)
        {
            return new ApiResponse<AuthResponse>(false, "Erro interno do servidor", null, new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new ApiResponse<AuthResponse>(false, "Credenciais inválidas", null, new List<string> { "Email ou senha incorretos" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                return new ApiResponse<AuthResponse>(false, "Credenciais inválidas", null, new List<string> { "Email ou senha incorretos" });
            }

            var token = GenerateJwtToken(user);
            var authResponse = new AuthResponse(token, user.Email!, user.Name);

            return new ApiResponse<AuthResponse>(true, "Login realizado com sucesso", authResponse);
        }
        catch (Exception ex)
        {
            return new ApiResponse<AuthResponse>(false, "Erro interno do servidor", null, new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<string>> LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return new ApiResponse<string>(true, "Logout realizado com sucesso", "Sessão encerrada");
    }

    public async Task<ApiResponse<string>> ForgetPasswordAsync(ForgetPasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new ApiResponse<string>(true, "Se o email existir, você receberá um link de recuperação", "Email enviado");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            Console.WriteLine($"Token de recuperação para {user.Email}: {token}");

            return new ApiResponse<string>(true, "Link de recuperação enviado para seu email", "Email enviado");
        }
        catch (Exception ex)
        {
            return new ApiResponse<string>(false, "Erro interno do servidor", null, new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new ApiResponse<string>(false, "Usuário não encontrado", null, new List<string> { "Email não encontrado" });
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                return new ApiResponse<string>(false, "Erro ao redefinir senha", null, result.Errors.Select(e => e.Description).ToList());
            }

            return new ApiResponse<string>(true, "Senha redefinida com sucesso", "Senha alterada");
        }
        catch (Exception ex)
        {
            return new ApiResponse<string>(false, "Erro interno do servidor", null, new List<string> { ex.Message });
        }
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var jwtKey = "your-super-secret-jwt-key-that-should-be-at-least-32-characters-long";
        var key = Encoding.UTF8.GetBytes(jwtKey);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "your-api",
            audience: "your-api-users",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
