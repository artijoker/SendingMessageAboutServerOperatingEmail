using HttpModels;
using HttpServer;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Начало работы сервера");

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
    builder.Services.AddHostedService<ServiceEmailSenderAboutServerOperation>();

    builder.Services.Configure<SmtpCredentials>(builder.Configuration.GetSection("SmtpCredentials"));

    builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));

    var app = builder.Build();

    app.MapGet("/", () => "Сервер работает!");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Фатальная ошибка!");
}
finally
{
    Log.Information("Серевер завершил работу");
    Log.CloseAndFlush();
}

