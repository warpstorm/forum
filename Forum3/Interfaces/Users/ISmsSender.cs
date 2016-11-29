using System.Threading.Tasks;

namespace Forum3.Interfaces.Users {
	public interface ISmsSender
    {
        Task SendSms(string number, string message);
    }
}
