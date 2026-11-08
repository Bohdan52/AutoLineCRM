using AutoLineCRM.BlazorClient;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using D20Tek.Blazor.BrowserStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddBrowserStorage();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var host = builder.Build();

// Якщо користувач не обрав «Запам'ятати цей пристрій», починаємо як новий користувач
try
{
    var storage = host.Services.GetRequiredService<D20Tek.Blazor.BrowserStorage.ILocalStorageService>();
    var remember = await storage.GetAsync<string>("rememberDevice");

    if (!remember.IsSuccess || remember.Value != "true")
        await storage.RemoveAsync("authToken");
}
catch { }

await host.RunAsync();