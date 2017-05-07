using System.Threading.Tasks;

namespace Forum3.Interfaces.Users {
	public interface ISmsSender {
		Task SendSmsAsync(string number, string message);
	}
}
