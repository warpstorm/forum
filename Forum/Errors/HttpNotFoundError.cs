namespace Forum.Errors {
	public class HttpNotFoundError : HttpException {
		public override int StatusCode => 404;

		public HttpNotFoundError() : base("No result found. You hackin' bro?") { }
	}
}