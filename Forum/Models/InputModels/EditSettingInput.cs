using System.ComponentModel.DataAnnotations;

namespace Forum.Models.InputModels {
	public class EditSettingInput {
		[Required]
		[MaxLength(200)]
		public string Key { get; set; }

		[MaxLength(200)]
		public string Value { get; set; }

		public bool AdminOnly { get; set; }
	}
}