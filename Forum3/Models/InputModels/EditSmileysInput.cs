using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.InputModels {
	public class EditSmileysInput {
		[Required]
		public EditSmileyInput[] Smileys { get; set; }
	}
}