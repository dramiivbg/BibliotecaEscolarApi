using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;


namespace MiApi.Services.EmailService
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }


public async Task SendAsync(string to, string subject, string body)
{
    var message = new MimeMessage();
    message.From.Add(MailboxAddress.Parse(_configuration["Smtp:From"]!));
    message.To.Add(MailboxAddress.Parse(to));
    message.Subject = subject;
    message.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

    var host = _configuration["Smtp:Host"]!;
    var port = int.Parse(_configuration["Smtp:Port"]!);

    using var client = new SmtpClient();

    // 465 = SSL implícito; 587 = STARTTLS; 1025 = sin cifrado (Mailpit)
    var options = port switch
    {
        465 => SecureSocketOptions.SslOnConnect,
        587 => SecureSocketOptions.StartTls,
        _   => SecureSocketOptions.None
    };

    client.CheckCertificateRevocation = false;   // <-- esto

    await client.ConnectAsync(host, port, options);

    if (!string.IsNullOrEmpty(_configuration["Smtp:User"]))
        await client.AuthenticateAsync(_configuration["Smtp:User"], _configuration["Smtp:Password"]);

    await client.SendAsync(message);
    await client.DisconnectAsync(true);
}
    }
}