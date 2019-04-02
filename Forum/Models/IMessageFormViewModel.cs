using System.ComponentModel.DataAnnotations;

namespace Forum.Models {
	public interface IMessageFormViewModel {
		string Id { get; }

		string TopicId { get; }
		string BoardId { get; }

		[Required]
		[DataType(DataType.MultilineText)]
		string Body { get; set; }

		string FormAction { get; }
		string FormController { get; }
		string ElementId { get; }
	}
}