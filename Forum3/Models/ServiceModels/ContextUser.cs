using System.Security.Claims;

namespace Forum3.ServiceModels {
	public class ContextUser {
		public string Id { get; set; }
		public bool IsAuthenticated { get; set; }
		public bool IsAdmin { get; set; }
		public bool IsVetted { get; set; }

		ClaimsPrincipal CurrentPrinicipal { get; set; }
	}
}