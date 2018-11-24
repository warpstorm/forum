using System.Threading.Tasks;

namespace Forum.Interfaces.Filters {
	public interface IRecaptchaValidator {
		string Response { get; }
		Task Validate(string recaptchaResponse, string ipAddress);
	}
}