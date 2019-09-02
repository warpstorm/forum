using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Forum.Data.Models {
	public class ImgurDetails {
		[Required]
		public int Id { get; set; }

		[Required]
		public int ImgurUserId { get; set; }

		[Required]
		public string LocalUserId { get; set; }

		[Required]
		[MaxLength(128)]
		public string ImgurUserName { get; set; }

		[Required]
		[MaxLength(128)]
		public string RefreshToken { get; set; }

		[Required]
		[MaxLength(128)]
		public string AccessToken { get; set; }

		[Required]
		public DateTime AccessTokenExpiration { get; set; }

		public List<string> Favorites { get; set; }
		public DateTime FavoritesUpdate { get; set; }
	}
}
