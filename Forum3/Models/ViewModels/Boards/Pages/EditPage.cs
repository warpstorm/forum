using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.ViewModels.Boards.Pages {
	public class EditPage {
		[Required]
		public int Id { get; set; }

		[Required]
		[StringLength(64)]
		public string Name { get; set; }

		[StringLength(512)]
		public string Description { get; set; }

		[StringLength(64)]
		public string Category { get; set; }

		[StringLength(64)]
		public string NewCategory { get; set; }

		public List<SelectListItem> Categories { get; set; }
	}
}