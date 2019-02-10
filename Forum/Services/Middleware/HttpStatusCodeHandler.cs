using Forum.Models.Errors;
using Forum.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Forum.Services.Middleware {
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
			catch (Exception exception) {
				if (context.Response.HasStarted) {
					throw new Exception($"An exception was caught after the response started.", exception);
				}

				HttpException error;

				if (exception is HttpException) {
					error = exception as HttpException;
				}
				else {
					error = new HttpInternalServerError(exception);
				}

				context.Response.Clear();

				context.Response.StatusCode = error.StatusCode;
				context.Response.Headers.Clear();

				context.Response.ContentType = error.ContentType;

				await context.Response.WriteAsync(exception.ToString());

				return;
			}
		}
	}
}