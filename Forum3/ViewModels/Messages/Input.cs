using System.ComponentModel.DataAnnotations;

namespace Forum3.ViewModels.Messages {
	public class Input
    {
		[DataType(DataType.MultilineText)]
		public string Body { get; set; }
	}
}
