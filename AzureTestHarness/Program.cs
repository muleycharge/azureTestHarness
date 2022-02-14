
using BLL;
using BO.Options;
using NLog.Web;

NLog.Logger _logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();



WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false)
    .AddJsonFile("appsettings.json.user", true)
    .AddEnvironmentVariables();
builder.Services.AddLogging();
builder.Services.AddSingleton<Sender>();
builder.Services.Configure<AzureTestHarnessOptions>(builder.Configuration.GetSection(AzureTestHarnessOptions.AzureTestHarness));
builder.Services.AddControllers();
var app = builder.Build();

app.MapControllers();

app.Run();
