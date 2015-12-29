using System.ComponentModel.DataAnnotations;

namespace Forum3.ViewModels.Messages {
	public class Input
    {
		public int? Id { get; set; }

		[DataType(DataType.MultilineText)]
		public string Body { get; set; }
	}
}
