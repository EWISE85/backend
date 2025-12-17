using DocumentFormat.OpenXml.Vml;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MimeKit;


namespace ElecWasteCollection.Infrastructure.ExternalService.Email
{
	public class EmailService : IEmailService
	{
		private readonly EmailSettings _settings;

		public EmailService(IOptions<EmailSettings> settings)
		{
			_settings = settings.Value;
		}
		public async Task SendEmailAsync(string to, string subject, string body)
		{
			var message = new MimeMessage();
			message.From.Add(new MailboxAddress(_settings.DisplayName, _settings.From));
			message.To.Add(new MailboxAddress("", to));
			message.Subject = subject;

			message.Body = new TextPart("plain")
			{
				Text = body
			};
			using var smtp = new SmtpClient();
			await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls);
			await smtp.AuthenticateAsync(_settings.From, _settings.Password);
			await smtp.SendAsync(message);
			await smtp.DisconnectAsync(true);
		}
	}
}
