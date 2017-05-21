using System.Threading.Tasks;
using Forum3.Interfaces.Users;
using Forum3.Models.ServiceModels;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Forum3.Services {
	public class AuthMessageSender : IEmailSender, ISmsSender {
		public AuthMessageSenderOptions Options { get; }

		public AuthMessageSender(IOptions<AuthMessageSenderOptions> optionsAccessor) {
			Options = optionsAccessor.Value;
		}

		public Task SendEmailAsync(string email, string subject, string message) {
			Execute(Options.SendGridKey, subject, message, email).Wait();
			return Task.FromResult(0);
		}

		public async Task Execute(string apiKey, string subject, string message, string email) {
			var client = new SendGridClient(apiKey);

			var msg = new SendGridMessage() {
				From = new EmailAddress(Options.FromAddress, Options.FromName),
				Subject = subject,
				PlainTextContent = message,
				HtmlContent = message
			};

			msg.AddTo(new EmailAddress(email));

			var response = await client.SendEmailAsync(msg);
		}

		public Task SendSmsAsync(string number, string message) {
			// Plug in your SMS service here to send a text message.
			return Task.FromResult(0);
		}
	}
}