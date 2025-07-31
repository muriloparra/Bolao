using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Bolao.Web;
using Bolao.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Fix: Use builder.Configuration to access configuration settings
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? throw new KeyNotFoundException()) });
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

await app.RunAsync();
