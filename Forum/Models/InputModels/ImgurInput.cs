using System.ComponentModel.DataAnnotations;

namespace Forum.Models.InputModels {
	public class ImgurInput {
		public int Id { get; set; }

		[MaxLength(128)]
		public string Username { get; set; }

		[MaxLength(128)]
		public string AccessToken { get; set; }
		public int ExpiresIn { get; set; }

		[MaxLength(128)]
		public string RefreshToken { get; set; }
	}
}
