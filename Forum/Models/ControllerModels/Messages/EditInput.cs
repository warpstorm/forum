using System.ComponentModel.DataAnnotations;

namespace Forum.Models.ControllerModels.Messages {
	public class EditInput {
		public int Id { get; set; }

		[Required]
		public string Body { get; set; }

		public bool SideLoad { get; set; }
	}
}