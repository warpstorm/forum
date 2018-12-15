using System.Threading.Tasks;

namespace Forum.Plugins.Recaptcha {
	public interface IRecaptchaValidator {
		string Response { get; }
		Task Validate(string recaptchaResponse, string ipAddress);
	}
}