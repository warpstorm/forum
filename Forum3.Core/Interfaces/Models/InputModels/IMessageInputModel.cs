using System.ComponentModel.DataAnnotations;

namespace Forum3.Interfaces.Models.InputModels {
	public interface IMessageInputModel {
		[Required]
		int Id { get; }

		[Required]
		[DataType(DataType.MultilineText)]
		string Body { get; set; }
	}
}