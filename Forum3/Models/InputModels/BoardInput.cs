using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.InputModels {
	public class BoardInput {
		[Required]
		[StringLength(64)]
		public string Name { get; set; }

		[StringLength(64)]
		public string Category { get; set; }
		
		[StringLength(64)]
		public string NewCategory { get; set; }

		public bool VettedOnly { get; set; }
	}
}