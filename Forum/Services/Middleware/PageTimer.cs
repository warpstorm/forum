using Forum.Core.Extensions;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Forum.Services.Middleware {
	public class PageTimer {
		RequestDelegate Next { get; }

		public PageTimer(
			RequestDelegate next
		) {
			next.ThrowIfNull(nameof(next));
			Next = next;
		}

		public async Task Invoke(HttpContext context) {
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			context.Items["PageTimer"] = stopwatch;
			await Next(context);
		}
	}
}