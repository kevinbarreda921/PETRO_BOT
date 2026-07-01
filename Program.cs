using PETRO_BOT.Components;
using PETRO_BOT.Services.Toast;
using PETRO_BOT.Services.Shared;
using PETRO_BOT.Services.Services;
using System.IO;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = ConfiguracionService.ObtenerRutaBase(),
    WebRootPath = Path.Combine(ConfiguracionService.ObtenerRutaBase(), "wwwroot")
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddSingleton<HistorialService>();
builder.Services.AddSingleton<PETRO_BOT.Services.Services.ValidacionPrecioService>();
builder.Services.AddSingleton<PETRO_BOT.Services.Services.ParteDiarioTotalService>();
builder.Services.AddSingleton<PETRO_BOT.Services.WebActiva.WebActivaScraperService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.UseStaticFiles(); // Necesario para archivos dinámicos generados en runtime (uploads)
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
