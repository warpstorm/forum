using System.Collections.Generic;
using System.Threading.Tasks;

namespace Forum.Interfaces.Models {
	public interface IRepository<T> where T : class {
		Task<List<T>> Records();
	}
}
