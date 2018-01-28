namespace Forum3.Exceptions {
	public class HttpForbiddenException : HttpStatusCodeException {
		public override int StatusCode => 403;

		public HttpForbiddenException() : base("You are not authorized to view this page.") { }
		public HttpForbiddenException(string message) : base(message) { }
	}
}