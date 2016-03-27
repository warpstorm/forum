using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Mvc;

namespace Forum3.ViewModels.Messages {
	public class Input
    {
		[HiddenInput]
		public int Id { get; set; }

		[HiddenInput]
		public int ParentId { get; set; }

		[HiddenInput]
		public int ReplyId { get; set; }

		[DataType(DataType.MultilineText)]
		public string Body { get; set; }
	}
}