using System.Threading.Tasks;

namespace Forum.Plugins.Recaptcha {
	public interface IRecaptcha3Validator {
		Task Validate(string recaptchaResponse, string ipAddress);
	}
}