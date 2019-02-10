using Forum.Models.Errors;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Forum.Services.Plugins.Recaptcha {
	public class Recaptcha2Validator : IRecaptcha2Validator {
		public string Response { get; }

		HttpClient HttpClient { get; }
		Recaptcha2Options Options { get; }

		public Recaptcha2Validator(
			IOptions<Recaptcha2Options> optionsAccessor
		) {
			HttpClient = new HttpClient(new HttpClientHandler()) {
				Timeout = TimeSpan.FromSeconds(3)
			};

			Options = optionsAccessor.Value;
		}

		public async Task Validate(string recaptchaResponse, string ipAddress) {
			var siteAddress = "https://www.google.com/recaptcha/api/siteverify";

			var request = new HttpRequestMessage(HttpMethod.Post, siteAddress) {
				Content = new FormUrlEncodedContent(new Dictionary<string, string> {
					["secret"] = Options.RecaptchaSecretKey,
					["response"] = recaptchaResponse,
					["remoteip"] = ipAddress
				})
			};

			var requestResponse = await HttpClient.SendAsync(request);
			requestResponse.EnsureSuccessStatusCode();

			var responseText = await requestResponse.Content.ReadAsStringAsync();

			var validatorResponse = JsonConvert.DeserializeObject<Recaptcha2ValidatorResponse>(responseText);

			if (!validatorResponse.Success) {
				var exceptionMessage = $"There was a problem validating the recaptcha. Error code(s) were:\n{string.Join("\n", validatorResponse.ErrorCodes)}\n";
				throw new HttpBadRequestError(validatorResponse.ErrorCodes.FirstOrDefault());
			}
		}
	}
}