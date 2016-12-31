using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Forum3.Interfaces.Models.ViewModels {
	public interface IMessageViewModel {
		int Id { get; }

		[Required]
		[DataType(DataType.MultilineText)]
		string Body { get; set; }

		string FormAction { get; }

		bool AllowCancel { get; }
	}
}