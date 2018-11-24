using System.ComponentModel.DataAnnotations;

namespace Forum.Interfaces.Models.ViewModels {
	public interface IMessageViewModel {
		int Id { get; }

		int? BoardId { get; }

		[Required]
		[DataType(DataType.MultilineText)]
		string Body { get; set; }

		string FormAction { get; }
		string FormController { get; }
	}
}