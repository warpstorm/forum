using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Forum.Models.ViewModels.Messages {
	public class EventDetails {
		public List<SelectListItem> Years { get; set; }
		public int StartYear { get; set; }
		public int EndYear { get; set; }

		public List<SelectListItem> Months { get; set; }
		public int StartMonth { get; set; }
		public int EndMonth { get; set; }

		public List<SelectListItem> Days { get; set; }
		public int StartDay { get; set; }
		public int EndDay { get; set; }

		public List<SelectListItem> Hours { get; set; }
		public int StartHour { get; set; }
		public int EndHour { get; set; }

		public List<SelectListItem> Minutes { get; set; }
		public int StartMinute { get; set; }
		public int EndMinute { get; set; }

		public List<SelectListItem> AmPm { get; set; }
		public bool StartAfternoon { get; set; }
		public bool EndAfternoon { get; set; }

		public bool AllDay { get; set; }
	}
}
