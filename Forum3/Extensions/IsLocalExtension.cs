using Microsoft.AspNetCore.Http;
using System.Net;

namespace Forum3.Extensions {
	/// <summary>
	/// Source: http://www.strathweb.com/2016/04/request-islocal-in-asp-net-core/
	/// </summary>
	public static class IsLocalExtension {
		public static bool IsLocal(this HttpRequest request) {
			var connection = request.HttpContext.Connection;

			if (connection.RemoteIpAddress != null) {
				if (IPAddress.IsLoopback(connection.RemoteIpAddress))
					return true;

				var remoteAddress = connection.RemoteIpAddress.MapToIPv4();

				if (connection.LocalIpAddress != null) {
					var localAddress = connection.LocalIpAddress.MapToIPv4();
					return remoteAddress.Equals(localAddress);
				}
			}

			// for in memory TestServer or when dealing with default connection info
			if (connection.RemoteIpAddress is null && connection.LocalIpAddress is null)
				return true;

			return false;
		}
	}
}