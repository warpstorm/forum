namespace Forum3.Models.ViewModels {
	public class Delay {
		public string ActionName { get; set; }
		public string NextAction { get; set; }
		public int CurrentPage { get; set; }
		public int TotalPages { get; set; }

		public double Percent {
			get {
				if (TotalPages > 0)
					return 100D * CurrentPage / TotalPages;
				else
					return 100;
			}
		}		
	}
}