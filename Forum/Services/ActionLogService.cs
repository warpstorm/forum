using Forum.Contexts;
using Forum.Models.DataModels;
using System.Threading.Tasks;

namespace Forum.Services {
	public class ActionLogService {
		public ApplicationDbContext DbContext { get; set; }
		public UserContext UserContext { get; set; }

		public ActionLogService(
			ApplicationDbContext dbContext,
			UserContext userContext
		) {
			DbContext = dbContext;
			UserContext = userContext;
		}

		public async Task Add(ActionLogItem logItem) {
			logItem.UserId = UserContext.ApplicationUser.Id;

			// Check if user is logged in or not

			DbContext.ActionLog.Add(logItem);

			// If performance becomes a concern, we could execute both of these simultaneously.
			// The downside is that the current actionLogItem would not be available on the current request.
			// Perhaps this could be sidestepped by saving back to the context.

			await DbContext.SaveChangesAsync();

			UserContext.ApplicationUser.LastActionLogItemId = logItem.Id;

			await DbContext.SaveChangesAsync();
		}
	}
}
