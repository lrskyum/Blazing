using System.Text.RegularExpressions;
using Blazing.Client.Modules.Import;
using Blazing.Components;
using Blazing.Modules.Database;
using Blazing.Modules.Import;
using Blazing.Modules.JobQueue;
using Havit.Blazor.Components.Web;
using _Imports = Blazing.Client._Imports;

bool.TryParse(Environment.GetEnvironmentVariable("TEMP_DB"), out var tempDb);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddHxServices();

// DB
var connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres;CommandTimeout=3600";

builder.Services.AddMigratedPostgresDatabase(o =>
{
    o.IsTemporaryDb = tempDb;
    o.TemporaryPort = 5442;
    o.ConnectionString = connectionString;
    o.SeedDataFilter = (string s) => SeedDataRegex().IsMatch(s);
});

builder.Services.AddScoped<IJobQueue, JobQueue>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IImportRestApi, ImportServiceApi>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(_Imports).Assembly);

app.Run();

partial class Program
{
    [GeneratedRegex(@".*v[0-9]+-seed-data.*")]
    private static partial Regex SeedDataRegex();
}