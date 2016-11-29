using System.Threading.Tasks;
using Forum3.Interfaces.Users;

namespace Forum3.Services {
	// This class is used by the application to send Email and SMS when you turn on two-factor authentication in ASP.NET Identity.
	// For more details see this link http://go.microsoft.com/fwlink/?LinkID=532713
	public class AuthMessageSender : IEmailSender, ISmsSender {
		public Task SendEmail(string email, string subject, string message) {
			// Plug in your email service here to send an email.
			return Task.FromResult(0);
		}

		public Task SendSms(string number, string message) {
			// Plug in your SMS service here to send a text message.
			return Task.FromResult(0);
		}
	}
}