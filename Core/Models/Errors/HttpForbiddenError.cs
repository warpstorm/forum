namespace Forum.Core.Models.Errors {
	public class HttpForbiddenError : HttpException {
		public override int StatusCode => 403;

		public HttpForbiddenError() : base("You are not authorized to view this page.") { }
	}
}