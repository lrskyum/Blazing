using System.Text.RegularExpressions;
using Blazing.Client.Modules.Import;
using Blazing.Components;
using Blazing.Modules.Import;
using Blazing.Modules.JobQueue;
using Blazing.Modules.RdbmsDatabase;
using Havit.Blazor.Components.Web;
using Microsoft.AspNetCore.ResponseCompression;
using _Imports = Blazing.Client._Imports;

bool.TryParse(Environment.GetEnvironmentVariable("TEMP_DB"), out var tempDb);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

// Blazor
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts =>
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]));
builder.Services.AddHxServices();

// DB
// -- cassandra
// builder.Services.AddMigratedCassandraDatabase(o =>
// {
//     o.IsTemporaryDb = tempDb;
//     o.TemporaryPort = 10042;
// });
// -- postgres
var connectionString = "Host=localhost" +
                       ";Port=5432" +
                       ";Database=postgres" +
                       ";Username=postgres" +
                       ";Password=postgres" +
                       ";CommandTimeout=3600";
builder.Services.AddMigratedPostgresDatabase(o =>
{
    o.IsTemporaryDb = tempDb;
    o.TemporaryPort = 5442;
    o.ConnectionString = connectionString;
    o.SeedDataFilter = (string s) => SeedDataRegex().IsMatch(s);
});

// IoC
builder.Services.AddSingleton<CounterHub>();
builder.Services.AddSingleton<IRowCounter, RowCounter>();
builder.Services.AddScoped<IJobQueue, JobQueue>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IImportRestApi, ImportServiceApi>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(_Imports).Assembly);
app.MapHub<CounterHub>("/counterhub");

app.Run();

partial class Program
{
    [GeneratedRegex(@".*v[0-9]+-seed-data.*")]
    private static partial Regex SeedDataRegex();
}