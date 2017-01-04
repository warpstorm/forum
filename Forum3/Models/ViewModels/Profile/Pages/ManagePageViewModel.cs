using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Forum3.Models.ViewModels.Profile.Pages {
	public class ManagePageViewModel
    {
		public string DisplayName { get; set; }

        public bool HasPassword { get; set; }

        public IList<UserLoginInfo> Logins { get; set; }

        public bool BrowserRemembered { get; set; }
    }
}
