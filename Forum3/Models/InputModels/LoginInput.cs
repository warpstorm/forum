using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.InputModels {
	public class LoginInput {
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		[DataType(DataType.Password)]
		public string Password { get; set; }

		public bool RememberMe { get; set; }
	}
}