using System.ComponentModel.DataAnnotations;

namespace Forum3.ViewModels.Message {
	public class Input
    {
		[DataType(DataType.MultilineText)]
		public string Body { get; set; }
	}
}
