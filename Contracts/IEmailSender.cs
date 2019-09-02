using System.Threading.Tasks;

namespace Forum.Contracts {
	public interface IEmailSender {
		bool Ready { get; }

		Task SendEmailAsync(string email, string subject, string message);
		Task SendEmailConfirmationAsync(string email, string link);
	}
}