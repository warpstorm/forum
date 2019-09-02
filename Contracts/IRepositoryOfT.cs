using System.Collections.Generic;
using System.Threading.Tasks;

namespace Forum.Contracts {
	public interface IRepository<T> where T : class {
		Task<List<T>> Records();
	}
}
