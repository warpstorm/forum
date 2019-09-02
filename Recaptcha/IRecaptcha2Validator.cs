using System.Threading.Tasks;

namespace Forum.ExternalClients.Recaptcha {
	public interface IRecaptcha2Validator {
		string Response { get; }
		Task Validate(string recaptchaResponse, string ipAddress);
	}
}