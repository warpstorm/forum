using System.Threading.Tasks;

namespace Forum3.Interfaces.Users {
	public interface IEmailSender
    {
        Task SendEmail(string email, string subject, string message);
    }
}
