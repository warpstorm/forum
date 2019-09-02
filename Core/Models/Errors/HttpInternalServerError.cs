using System;

namespace Forum.Core.Models.Errors {
	public class HttpInternalServerError : HttpException {
		public override int StatusCode => 500;

		public HttpInternalServerError() : base("An error occurred.") { }
		public HttpInternalServerError(string message) : base(message) { }
		public HttpInternalServerError(Exception e) : base("An error occurred.", e) { }
	}
}