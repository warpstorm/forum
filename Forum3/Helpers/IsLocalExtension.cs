using System.Net;
using Microsoft.AspNetCore.Http;

namespace Forum3.Helpers {
	/// <summary>
	/// Source: http://www.strathweb.com/2016/04/request-islocal-in-asp-net-core/
	/// </summary>
	public static class IsLocalExtension {
		public static bool IsLocal(this HttpRequest request) {
			var connection = request.HttpContext.Connection;

			if (connection.RemoteIpAddress != null) {
				if (connection.LocalIpAddress != null)
					return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
				else
					return IPAddress.IsLoopback(connection.RemoteIpAddress);
			}

			// for in memory TestServer or when dealing with default connection info
			if (connection.RemoteIpAddress == null && connection.LocalIpAddress == null)
				return true;

			return false;
		}
	}
}