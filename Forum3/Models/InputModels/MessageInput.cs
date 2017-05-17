using Forum3.Interfaces.Models.InputModels;
using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.InputModels {
	public class MessageInput : IMessageInputModel {
		public int Id { get; set; }
		public int? BoardId { get; set; }

		[Required]
		public string Body { get; set; }
	}
}