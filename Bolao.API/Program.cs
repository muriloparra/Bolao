
using Bolao.API.Data;
using Bolao.API.Models;
using Bolao.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp",
        builder =>
        {
            builder.WithOrigins("https://localhost:59027", "http://localhost:59028")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

// Add services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

var jwtKey = "your-super-secret-jwt-key-that-should-be-at-least-32-characters-long";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "your-api",
        ValidAudience = "your-api-users",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Identity API",
        Version = "v1",
        Description = "Web API com autenticação Identity e JWT"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando Bearer. Exemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
   {
           {
               new OpenApiSecurityScheme
               {
                   Reference = new OpenApiReference // Fix: Use OpenApiReference from Microsoft.OpenApi.Models
                   {
                       Type = ReferenceType.SecurityScheme,
                       Id = "Bearer"
                   }
               },
               Array.Empty<string>()
           }
   });
});

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowBlazorApp");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity API V1");
        c.RoutePrefix = string.Empty;
    });
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await context.Database.EnsureCreatedAsync();
    await SeedAdminUser(userManager);
}

// Auth Endpoints
app.MapPost("/register", async (RegisterRequest request, IAuthService authService) =>
{
    var result = await authService.RegisterAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("Register")
.WithTags("Authentication")
.WithSummary("Registrar novo usuário")
.WithDescription("Cria uma nova conta de usuário no sistema");

app.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
{
    var result = await authService.LoginAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("Login")
.WithTags("Authentication")
.WithSummary("Autenticar usuário")
.WithDescription("Autentica o usuário e retorna um token JWT");

app.MapPost("/logout", [Authorize] async (IAuthService authService, HttpContext context) =>
{
    var result = await authService.LogoutAsync();
    return Results.Ok(result);
})
.WithName("Logout")
.WithTags("Authentication")
.WithSummary("Encerrar sessão")
.WithDescription("Encerra a sessão do usuário atual")
.RequireAuthorization();

app.MapPost("/forget-password", async (ForgetPasswordRequest request, IAuthService authService) =>
{
    var result = await authService.ForgetPasswordAsync(request);
    return Results.Ok(result);
})
.WithName("ForgetPassword")
.WithTags("Authentication")
.WithSummary("Iniciar recuperação de senha")
.WithDescription("Inicia o processo de recuperação de senha por email");

app.MapPost("/reset-password", async (ResetPasswordRequest request, IAuthService authService) =>
{
    var result = await authService.ResetPasswordAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("ResetPassword")
.WithTags("Authentication")
.WithSummary("Resetar senha")
.WithDescription("Redefine a senha do usuário usando o token de recuperação");

// User CRUD Endpoints
app.MapPost("/user", [Authorize] async (CreateUserRequest request, IUserService userService) =>
{
    var result = await userService.CreateUserAsync(request);
    return result.Success ? Results.Created($"/user/{result.Data?.Id}", result) : Results.BadRequest(result);
})
.WithName("CreateUser")
.WithTags("User Management")
.WithSummary("Criar novo usuário")
.WithDescription("Cria um novo usuário no sistema")
.RequireAuthorization();

app.MapGet("/users", [Authorize] async (IUserService userService, string nome, int page, int pageSize) =>
{
    nome = nome ?? string.Empty;
    page = page <= 0 ? 1 : page;
    pageSize = pageSize <= 0 ? 10 : pageSize;

    var result = await userService.GetUsersAsync(nome, page, pageSize);
    return Results.Ok(result);
})
.WithName("GetUsers")
.WithTags("User Management")
.WithSummary("Listar usuários")
.WithDescription("Lista todos os usuários com paginação e filtro por nome")
.RequireAuthorization();

app.MapGet("/user/{id}", [Authorize] async (string id, IUserService userService) =>
{
    var result = await userService.GetUserByIdAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.WithName("GetUserById")
.WithTags("User Management")
.WithSummary("Obter usuário por ID")
.WithDescription("Retorna os dados de um usuário específico")
.RequireAuthorization();

app.MapDelete("/user/{id}", [Authorize] async (string id, IUserService userService) =>
{
    var result = await userService.DeleteUserAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.WithName("DeleteUser")
.WithTags("User Management")
.WithSummary("Remover usuário")
.WithDescription("Remove um usuário do sistema")
.RequireAuthorization();

app.Run();

static async Task SeedAdminUser(UserManager<ApplicationUser> userManager)
{
    const string adminEmail = "murilo.parra@gmail.com";
    const string adminPassword = "Admin@123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            Name = "Administrador"
        };

        await userManager.CreateAsync(adminUser, adminPassword);
    }
}
