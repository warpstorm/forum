using System.ComponentModel.DataAnnotations;

namespace Forum.Models.InputModels {
	public class EditSmileysInput {
		[Required]
		public EditSmileyInput[] Smileys { get; set; }
	}
}