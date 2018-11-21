using Forum3.Models.ServiceModels;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Forum3 {
	public class BaseSettings : IEnumerable<BaseSetting> {
		static List<BaseSetting> Values { get; } = new List<BaseSetting> {
			new BaseSetting {
				Key = "Installed",
				Display = "Installed",
				Description = "Is the forum installed?"
			},
			new BaseSetting {
				Key = "AvatarSize",
				Display = "Avatar Size",
				Description = "Alters the size of the avatars"
			},
			new BaseSetting {
				Key = "HistoryTimeLimit",
				Display = "History Time Limit",
				Description = "How many days back to limit viewed topic checks. Should be negative!"
			},
			new BaseSetting {
				Key = "MessagesPerPage",
				Display = "MessagesPerPage",
				Description = "How many days back to limit viewed topic checks. Should be negative!"
			},
			new BaseSetting {
				Key = "OnlineTimeLimit",
				Display = "OnlineTimeLimit",
				Description = "How long a person can be offline before they're no longer marked as online."
			},
			new BaseSetting {
				Key = "PopularityLimit",
				Display = "PopularityLimit",
				Description = "Changes how many posts a topic must have to be considered popular."
			},
			new BaseSetting {
				Key = "TopicsPerPage",
				Display = "TopicsPerPage",
				Description = "Limits how many topics per page are displayed in a board."
			},
			new BaseSetting {
				Key = "ShowFavicons",
				Display = "ShowFavicons",
				Description = "Show icons by links in post bodies.",
				Options = new List<string> {
					"True",
					"False"
				}
			},
			new BaseSetting {
				Key = "FrontPage",
				Display = "Front Page",
				Description = "Choose which page is your front page.",
				Options = new List<string> {
					"Board List",
					"All Topics",
					"Unread Topics"
				}
			},
			new BaseSetting {
				Key = "PoseyUsers",
				Display = "Posey'd Users",
				Description = "Posey's the users. Separate IDs with a pipe."
			},
			new BaseSetting {
				Key = "StrippedUrls",
				Display = "Site-Stripped URLs",
				Description = "Strips the site name from these second level domains. Separate domains with a pipe. Do not include the TLD like .com or .org"
			},
		};

		public IEnumerator<BaseSetting> GetEnumerator() => Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public static BaseSetting Get(string name) => Values.First(v => v.Key == name);
	}
}