using System.Threading.Tasks;

namespace Forum.ExternalClients.Recaptcha {
	public interface IRecaptcha3Validator {
		Task Validate(string recaptchaResponse, string ipAddress);
	}
}