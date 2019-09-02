using Forum.Contracts;
using Forum.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Services.Repositories {
	using DataModels = Data.Models;

	public class SmileyRepository : IRepository<DataModels.Smiley> {
		ApplicationDbContext DbContext { get; }

		public SmileyRepository(ApplicationDbContext dbContext) => DbContext = dbContext;

		public async Task<List<DataModels.Smiley>> Records() {
			if (_Records is null) {
				var records = await DbContext.Smileys.ToListAsync();
				_Records = records.Where(r => r.Code != null).OrderBy(s => s.SortOrder).ToList();
			}

			return _Records;
		}
		List<DataModels.Smiley> _Records;

		public async Task<DataModels.Smiley> FindById(int id) {
			var records = await Records();
			return records.FirstOrDefault(item => item.Id == id);
		}

		public async Task<DataModels.Smiley> FindByCode(string code) {
			var records = await Records();
			return records.FirstOrDefault(item => item.Code == code);
		}
	}
}