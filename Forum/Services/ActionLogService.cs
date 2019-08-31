using Forum.Models.DataModels;
using Forum.Services.Contexts;
using Forum.Services.Repositories;
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
			logItem.UserId = UserContext.ApplicationUser?.Id ?? string.Empty;

			// Check if user is logged in or not

			DbContext.ActionLog.Add(logItem);

			// If performance becomes a concern, we could execute both of these simultaneously.
			// The downside is that the current actionLogItem would not be available on the current request.
			// Perhaps this could be sidestepped by saving back to the context.

			await DbContext.SaveChangesAsync();

			if (!(UserContext.ApplicationUser is null)) {
				UserContext.ApplicationUser.LastActionLogItemId = logItem.Id;

				var records = await AccountRepository.Records();

				var record = records.First(r => r.Id == UserContext.ApplicationUser.Id);
				record.LastActionLogItemId = logItem.Id;

				await DbContext.SaveChangesAsync();
			}
		}
	}
}
