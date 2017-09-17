using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using Forum3.Interfaces.Users;
using Forum3.Models.ServiceModels;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using System.Net;
using System;

namespace Forum3.Services {
	public class EmailSender : IEmailSender {
		public bool Ready { get; }

		EmailSenderOptions Options { get; }
		ILogger Logger { get; }

		public EmailSender(
			IOptions<EmailSenderOptions> optionsAccessor,
			ILogger<EmailSender> logger
		) {
			Options = optionsAccessor.Value;
			Logger = logger;

			if (Options.SendGridKey != null 
				&& Options.SendGridUser != null
				&& Options.FromName != null
				&& Options.FromAddress != null)
				Ready = true;
		}

		public Task SendEmailAsync(string email, string subject, string message) {
			Execute(Options.SendGridKey, subject, message, email).Wait();
			return Task.CompletedTask;
		}

		public Task SendEmailConfirmationAsync(string email, string link) {
			return SendEmailAsync(
				email,
				"Confirm your email",
				$"Please confirm your account by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>");
		}

		public async Task Execute(string apiKey, string subject, string message, string email) {
			if (!Ready)
				throw new ApplicationException("EmailSender is not ready.");

			var client = new SendGridClient(apiKey);

			var msg = new SendGridMessage() {
				From = new EmailAddress(Options.FromAddress, Options.FromName),
				Subject = subject,
				PlainTextContent = message,
				HtmlContent = message
			};

			msg.AddTo(new EmailAddress(email));

			var response = await client.SendEmailAsync(msg);

			if (response.StatusCode != HttpStatusCode.Accepted)
				Logger.LogCritical($"Error sending email. Response body: {response.Body}");
		}
	}
}