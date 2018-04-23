namespace Forum3.Errors {
	public class HttpBadRequestError : HttpException {
		public override int StatusCode => 400;

		public HttpBadRequestError() : base("Bad request.") { }
		public HttpBadRequestError(string message) : base(message) { }
	}
}