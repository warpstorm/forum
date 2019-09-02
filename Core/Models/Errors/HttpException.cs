using Newtonsoft.Json.Linq;
using System;

namespace Forum.Core.Models.Errors {
	public partial class HttpException : ApplicationException {
		public virtual int StatusCode { get; }
		public string ContentType { get; } = @"text/plain";

		public HttpException(string message) : base(message) { }
		public HttpException(string message, Exception inner) : base(message, inner) { }
		public HttpException(JObject errorObject) : this(errorObject.ToString()) => ContentType = @"application/json";
	}
}