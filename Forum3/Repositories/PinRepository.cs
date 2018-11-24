using Forum.Contexts;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Forum.Repositories {
	using DataModels = Models.DataModels;

	public class PinRepository : Repository<DataModels.Pin> {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }

		public PinRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			ILogger<PinRepository> log
		) : base(log) {
			DbContext = dbContext;
			UserContext = userContext;
		}

		protected override List<DataModels.Pin> GetRecords() => DbContext.Pins.Where(item => item.UserId == UserContext.ApplicationUser.Id).OrderByDescending(item => item.Id).ToList();
	}
}