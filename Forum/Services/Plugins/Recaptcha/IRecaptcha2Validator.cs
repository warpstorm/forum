using System.Threading.Tasks;

namespace Forum.Services.Plugins.Recaptcha {
	public interface IRecaptcha2Validator {
		string Response { get; }
		Task Validate(string recaptchaResponse, string ipAddress);
	}
}