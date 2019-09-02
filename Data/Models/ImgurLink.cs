using System;
using System.ComponentModel.DataAnnotations;

namespace Forum.Data.Models {
	public class ImgurLink {
		public int Id { get; set; }
		public int ImgurUserId { get; set; }
		public string LocalUserId { get; set; }

		[MaxLength(128)]
		public string ImgurUserName { get; set; }

		[MaxLength(128)]
		public string RefreshToken { get; set; }

		[MaxLength(128)]
		public string AccessToken { get; set; }
		public DateTime AccessTokenExpiration { get; set; }
	}
}
