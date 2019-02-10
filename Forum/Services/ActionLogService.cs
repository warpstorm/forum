using Forum.Services.Contexts;
using Forum.Models.DataModels;
using Forum.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Services {
	public class ActionLogService {
		ApplicationDbContext DbContext { get; set; }
		UserContext UserContext { get; set; }
		AccountRepository AccountRepository { get; set; }

		public ActionLogService(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
		}

		public async Task Add(ActionLogItem logItem) {
			logItem.Timestamp = DateTime.Now;
			logItem.UserId = UserContext.ApplicationUser.Id;

			// Check if user is logged in or not

			DbContext.ActionLog.Add(logItem);

			// If performance becomes a concern, we could execute both of these simultaneously.
			// The downside is that the current actionLogItem would not be available on the current request.
			// Perhaps this could be sidestepped by saving back to the context.

			await DbContext.SaveChangesAsync();

			UserContext.ApplicationUser.LastActionLogItemId = logItem.Id;
			(await AccountRepository.Records()).First(r => r.Id == UserContext.ApplicationUser.Id).LastActionLogItemId = logItem.Id;

			await DbContext.SaveChangesAsync();
		}
	}
}
