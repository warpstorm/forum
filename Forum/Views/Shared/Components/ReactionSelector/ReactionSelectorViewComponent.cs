using Forum.Data.Contexts;
using Forum.ExternalClients.Imgur;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Views.Shared.Components.ReactionSelector {
	public class ReactionSelectorViewComponent : ViewComponent {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		ImgurClient ImgurClient { get; }

		public ReactionSelectorViewComponent(
			ApplicationDbContext dbContext,
			UserContext userContext,
			ImgurClient imgurClient
		) {
			DbContext = dbContext;
			UserContext = userContext;
			ImgurClient = imgurClient;
		}

		public async Task<IViewComponentResult> InvokeAsync() {
			var viewModel = new ReactionSelectorViewModel();

			if (!(UserContext.Imgur is null)) {
				if (UserContext.Imgur.AccessTokenExpiration < DateTime.Now) {
					await ImgurClient.RefreshToken();
				}

				if (!UserContext.Imgur.Favorites?.Any() ?? false || UserContext.Imgur.FavoritesUpdate < DateTime.Now.AddHours(-1)) {
					var favorites = await ImgurClient.GetFavorites();

					if (favorites.Any()) {
						UserContext.Imgur.Favorites = favorites;
						UserContext.Imgur.FavoritesUpdate = DateTime.Now;
						DbContext.Update(UserContext.Imgur);
						await DbContext.SaveChangesAsync();
					}
				}

				var images = new List<ReactionSelectorItem>();

				if (!(UserContext.Imgur.Favorites is null)) {
					foreach (var item in UserContext.Imgur.Favorites) {
						images.Add(new ReactionSelectorItem {
							Id = item,
							Path = $"https://i.imgur.com/{item}.mp4"
						});
					}
				}

				viewModel.Images = images;
			}

			return View("Default", viewModel);
		}

		public class ReactionSelectorViewModel {
			public List<ReactionSelectorItem> Images { get; set; }
		}

		public class ReactionSelectorItem {
			public string Id { get; set; }
			public string Path { get; set; }
		}
	}
}
