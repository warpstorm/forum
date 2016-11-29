using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Forum3.Interfaces.Messages {
	public interface IMessageInput
    {
		[HiddenInput]
		int Id { get; set; }

		[Required]
		[DataType(DataType.MultilineText)]
		string Body { get; set; }

		string FormAction { get; set; }
	}
}
