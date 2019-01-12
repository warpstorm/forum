using System.ComponentModel.DataAnnotations;

namespace Forum.Interfaces.Models.ViewModels {
	public interface IMessageViewModel {
		string Id { get; }

		string BoardId { get; }

		[Required]
		[DataType(DataType.MultilineText)]
		string Body { get; set; }

		string FormAction { get; }
		string FormController { get; }
		string ElementId { get; set; }
	}
}