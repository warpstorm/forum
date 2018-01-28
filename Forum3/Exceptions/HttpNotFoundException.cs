namespace Forum3.Exceptions {
	public class HttpNotFoundException : HttpStatusCodeException {
		public override int StatusCode => 404;

		public HttpNotFoundException() : base("No result found.") { }
		public HttpNotFoundException(string message) : base(message) { }
	}
}