using System.Threading.Tasks;

namespace Forum3.Interfaces.Services {
	public interface IEmailSender {
		bool Ready { get; }

		Task SendEmailAsync(string email, string subject, string message);
		Task SendEmailConfirmationAsync(string email, string link);
	}
}