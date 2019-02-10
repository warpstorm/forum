using System.ComponentModel.DataAnnotations;

namespace Forum.Interfaces.Models.ViewModels {
	public interface IMessageFormViewModel {
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