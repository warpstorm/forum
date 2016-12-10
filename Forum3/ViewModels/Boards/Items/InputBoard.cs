namespace Forum3.ViewModels.Boards.Items {
	public class InputBoard {
		public int Id { get; set; }
		public string Name { get; set; }
		public string Parent { get; set; }
		public bool VettedOnly { get; set; }
		public bool InviteOnly { get; set; }
	}
}
