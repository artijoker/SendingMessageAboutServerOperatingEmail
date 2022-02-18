using HttpModels;
using Serilog;
using System.Diagnostics;
using Polly;
using Polly.Retry;

namespace HttpServer
{
    public class ServiceEmailSenderAboutServerOperation : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SmtpCredentials> _logger;

        public ServiceEmailSenderAboutServerOperation(IServiceProvider serviceProvider, ILogger<SmtpCredentials> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Сервер успешно запущен.");
            using (PeriodicTimer timer = new(TimeSpan.FromHours(1)))
            {
                do
                {
                    await TrySendMessage(5, stoppingToken);
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
        }

        private async Task TrySendMessage(int retryCount, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Отправка сообщения 'Отсчет о состояния сервера'.");

            var process = Process.GetCurrentProcess();
            process.Refresh();
            var bytes = process.WorkingSet64;

            using (var scope = _serviceProvider.CreateScope())
            {
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                var email = "artijoker@gmail.com";

                AsyncRetryPolicy? policy = Policy
                              .Handle<Exception>()
                              .WaitAndRetryAsync(
                                   retryCount,
                                   retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                                   onRetry: (exception, time, retryCount, context) =>
                                   {
                                       _logger.LogWarning(
                                           exception,
                                           "Не удалось отправить сообщение по адресу {email}. Попытка: {retryCount}",
                                           email,
                                           retryCount
                                           );
                                   });
                PolicyResult? result = await policy.ExecuteAndCaptureAsync(
                    token => emailSender.SendMessage(
                        email,
                        "Отсчет о состояния сервера",
                        $"Сервер работает. Размер используемой памяти {bytes / 1024} Кб.",
                        token),
                    stoppingToken
                );

                if (result.Outcome == OutcomeType.Failure)
                    _logger.LogError(result.FinalException, "При отправке сообщение по адресу {email} произошла ошибка!", email);
                else
                    _logger.LogInformation("Cообщение по адресу {email} Успешно отправлено.", email);
            }
        }
    }
}
