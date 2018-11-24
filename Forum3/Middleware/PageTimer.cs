using Forum.Extensions;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Forum.Middleware {
	public class PageTimer {
		RequestDelegate _next { get; }

		public PageTimer(
			RequestDelegate next
		) {
			next.ThrowIfNull(nameof(next));
			_next = next;
		}

		public async Task Invoke(HttpContext context) {
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			context.Items["PageTimer"] = stopwatch;
			await _next(context);
		}
	}
}