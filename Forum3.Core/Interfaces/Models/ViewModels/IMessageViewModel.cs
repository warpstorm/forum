using System.ComponentModel.DataAnnotations;

namespace Forum3.Interfaces.Models.ViewModels {
	public interface IMessageViewModel {
		int Id { get; }

		[Required]
		[DataType(DataType.MultilineText)]
		string Body { get; set; }

		string FormAction { get; }
		string FormController { get; }

		string CancelPath { get; set; }
	}
}