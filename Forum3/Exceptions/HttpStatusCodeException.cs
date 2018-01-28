using Newtonsoft.Json.Linq;
using System;

namespace Forum3.Exceptions {
	public class HttpStatusCodeException : ApplicationException {
		public virtual int StatusCode => 500;
		public string ContentType { get; } = @"text/plain";

		public HttpStatusCodeException(string message) : base(message) { }
		public HttpStatusCodeException(Exception inner) : this(inner.ToString()) { }
		public HttpStatusCodeException(JObject errorObject) : this(errorObject.ToString()) => ContentType = @"application/json";
	}
}