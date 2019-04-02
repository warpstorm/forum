using System.ComponentModel.DataAnnotations;

namespace Forum.Models.ControllerModels.Smileys {
	public class EditSmileysInput {
		[Required]
		public EditSmileyInput[] Smileys { get; set; }
	}
}