using System.Threading.Tasks;

namespace Forum3.Interfaces.Accounts {
	public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }
}
