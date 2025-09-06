using MicroSASS.Web;
using MicroSASS.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://centralbolao-api-gqe8b2h7cdakh8de.brazilsouth-01.azurewebsites.net") });
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

await app.RunAsync();