using Forum3.Interfaces.Models.InputModels;

namespace Forum3.InputModels {
	public class MessageInput : IMessageInputModel {
		public int Id { get; set; }
		public string Body { get; set; }
	}
}