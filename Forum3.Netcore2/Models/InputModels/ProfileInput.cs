using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.InputModels {
	public class ProfileInput {
		[Required]
		public int Id { get; set; }

		[Required]
		[MaxLength(64)]
		public string DisplayName { get; set; }
	}
}