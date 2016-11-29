using System.Threading.Tasks;

namespace Forum3.Interfaces.Users {
	public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
