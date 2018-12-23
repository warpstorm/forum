using Forum.Models.ServiceModels;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Forum {
	public class BaseSettings : IEnumerable<BaseSetting> {
		static List<BaseSetting> Values { get; } = new List<BaseSetting> {
			new BaseSetting {
				Key = Constants.Settings.AvatarSize,
				Display = "Avatar size",
				Description = "Alters the size of the avatars"
			},
			new BaseSetting {
				Key = Constants.Settings.FrontPage,
				Display = "Front page",
				Description = "Choose which page is your front page.",
				Options = new List<string> {
					"Board List",
					"All Topics",
					"Unread Topics"
				}
			},
			new BaseSetting {
				Key = Constants.Settings.HistoryTimeLimit,
				Display = "History time limit",
				Description = "How many days back to limit viewed topic checks. Should be negative!"
			},
			new BaseSetting {
				Key = Constants.Settings.Installed,
				Display = "Installed",
				Description = "Is the forum installed?"
			},
			new BaseSetting {
				Key = Constants.Settings.MessagesPerPage,
				Display = "Messages per page",
				Description = "How many days back to limit viewed topic checks. Should be negative!"
			},
			new BaseSetting {
				Key = Constants.Settings.OnlineTimeLimit,
				Display = "Online time limit",
				Description = "How long a person can be offline before they're no longer marked as online."
			},
			new BaseSetting {
				Key = Constants.Settings.PopularityLimit,
				Display = "Popularity limit",
				Description = "Changes how many posts a topic must have to be considered popular."
			},
			new BaseSetting {
				Key = Constants.Settings.PoseyUsers,
				Display = "Posey'd users",
				Description = "Posey's the users. Separate IDs with a pipe."
			},
			new BaseSetting {
				Key = Constants.Settings.ShowFavicons,
				Display = "Show favicons",
				Description = "Show icons by links in post bodies.",
				Options = new List<string> {
					"False",
					"True"
				}
			},
			new BaseSetting {
				Key = Constants.Settings.SideLoading,
				Display = "Enable sideloading",
				Description = "Enables content management with the server in the background",
				Options = new List<string> {
					"False",
					"True"
				}
			},
			new BaseSetting {
				Key = Constants.Settings.StrippedUrls,
				Display = "Site-Stripped URLs",
				Description = "Strips the site name from these second level domains. Separate domains with a pipe. Do not include the TLD like .com or .org"
			},
			new BaseSetting {
				Key = Constants.Settings.TopicsPerPage,
				Display = "Topics per page",
				Description = "Limits how many topics per page are displayed in a board."
			},
		};

		public IEnumerator<BaseSetting> GetEnumerator() => Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public static BaseSetting Get(string name) => Values.First(v => v.Key == name);
	}
}