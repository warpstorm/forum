using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Forum3.Models.ViewModels.Profile {
	public class ConfigureTwoFactorViewModel {
		public string SelectedProvider { get; set; }

		public ICollection<SelectListItem> Providers { get; set; }
	}
}