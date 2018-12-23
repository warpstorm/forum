using Forum.Contexts;
using Forum.Interfaces.Services;
using Forum.Repositories;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Services {
	public class SetupService {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		RoleRepository RoleRepository { get; }
		SettingsRepository SettingsRepository { get; }
		IForumViewResult ForumViewResult { get; }

		public SetupService(
			ApplicationDbContext dbContext,
			UserContext userContext,
			RoleRepository roleRepository,
			SettingsRepository settingsRepository,
			IForumViewResult forumViewResult
		) {
			DbContext = dbContext;
			UserContext = userContext;
			RoleRepository = roleRepository;
			SettingsRepository = settingsRepository;
			ForumViewResult = forumViewResult;
		}

		public async Task SetupRoles() {
			if (!RoleRepository.SiteRoles.Any()) {
				await RoleRepository.Create(new Models.InputModels.CreateRoleInput {
					Name = Constants.InternalKeys.Admin,
					Description = "Forum administrators"
				});
			}
		}

		public async Task SetupAdmins() {
			if (DbContext.Users.Count() > 1) {
				return;
			}

			if (UserContext.IsAdmin) {
				return;
			}

			var adminRole = RoleRepository.SiteRoles.First(r => r.Name == Constants.InternalKeys.Admin);
			await RoleRepository.AddUser(adminRole.Id, UserContext.ApplicationUser.Id);
		}

		public void SetupCategories() {
			if (DbContext.Categories.Any()) {
				return;
			}

			DbContext.Categories.Add(new Models.DataModels.Category {
				DisplayOrder = 1,
				Name = "On Topic"
			});

			DbContext.SaveChanges();
		}

		public void SetupBoards() {
			if (DbContext.Boards.Any()) {
				return;
			}

			var category = DbContext.Categories.First();

			DbContext.Boards.Add(new Models.DataModels.Board {
				CategoryId = category.Id,
				DisplayOrder = 1,
				Name = "General Discussion",
				Description = "Various talk about things that interest you."
			});

			DbContext.SaveChanges();
		}
	}
}
