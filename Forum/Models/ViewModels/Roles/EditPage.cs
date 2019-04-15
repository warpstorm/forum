using System;
using System.ComponentModel.DataAnnotations;

namespace Forum.Models.ViewModels.Roles {
	public class EditPage {
		public string Id { get; set; }

		[Required]
		[MaxLength(64)]
		public string Name { get; set; }

		[Required]
		[MaxLength(512)]
		public string Description { get; set; }

		public string CreatedBy { get; set; }
		public DateTime Created { get; set; }

		public string ModifiedBy { get; set; }
		public DateTime Modified { get; set; }

		public int NumberOfUsers { get; set; }
	}
}