using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace VehiclePartsBackend.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new InvalidOperationException("Recipient email is required.");
        }

        if (_settings.UseDevelopmentMode || string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            _logger.LogInformation(
                "DEV EMAIL to {To} | Subject: {Subject} | Body length: {Length} chars",
                toEmail,
                subject,
                htmlBody.Length);
            _logger.LogDebug("DEV EMAIL body: {Body}", htmlBody);
            await Task.CompletedTask;
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = true,
            Credentials = string.IsNullOrWhiteSpace(_settings.SmtpUser)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_settings.SmtpUser, _settings.SmtpPassword)
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}