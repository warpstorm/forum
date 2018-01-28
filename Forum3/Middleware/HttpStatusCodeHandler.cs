using Forum3.Exceptions;
using Forum3.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Forum3.Middleware {
	public class HttpStatusCodeHandler {
		RequestDelegate _next { get; }

		public HttpStatusCodeHandler(
			RequestDelegate next
		) {
			next.ThrowIfNull(nameof(next));
			_next = next;
		}

		public async Task Invoke(HttpContext context) {
			try {
				await _next(context);
			}
			catch (HttpStatusCodeException e) {
				if (context.Response.HasStarted)
					throw new Exception("HttpStatusCodeException caught after response has already started.", e);

				context.Response.Clear();

				context.Response.StatusCode = e.StatusCode;
				context.Response.Headers.Clear();

				context.Response.ContentType = e.ContentType;

				await context.Response.WriteAsync(e.Message);

				return;
			}
		}
	}
}