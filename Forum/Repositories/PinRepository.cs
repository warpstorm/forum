using Forum.Contexts;
using Forum.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Repositories {
	using DataModels = Models.DataModels;

	public class PinRepository : IRepository<DataModels.Pin> {
		public async Task<List<DataModels.Pin>> Records() {
			if (_Records is null) {
				var records = await DbContext.Pins.Where(r => r.UserId == UserContext.ApplicationUser.Id).ToListAsync();
				_Records = records.OrderByDescending(item => item.Id).ToList();
			}

			return _Records;
		}
		List<DataModels.Pin> _Records;

		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }

		public PinRepository(
			ApplicationDbContext dbContext,
			UserContext userContext
		) {
			DbContext = dbContext;
			UserContext = userContext;
		}
	}
}