using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Bolao.Web;
using Bolao.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiSettings:ApiBaseUrl"];
if (string.IsNullOrEmpty(apiBaseUrl))
{
    Console.WriteLine("Erro: ApiBaseUrl não configurada em appsettings!");
}

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl ?? "https://localhost:5001") });
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

await app.RunAsync();