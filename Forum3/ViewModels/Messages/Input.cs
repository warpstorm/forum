using System.ComponentModel.DataAnnotations;
using Forum3.Helpers;
using Microsoft.AspNet.Mvc;

namespace Forum3.ViewModels.Messages {
	public class Input
    {
		[HiddenInput]
		public int Id { get; set; }

		[Required]
		[DataType(DataType.MultilineText)]
		public string Body { get; set; }

		public string FormAction { get; set; }
	}
}