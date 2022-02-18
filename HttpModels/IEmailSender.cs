namespace HttpModels
{
    public interface IEmailSender
    {
        Task SendMessage(string toEmail, string? subject = null, string? body = null, CancellationToken token = default);
    }
}