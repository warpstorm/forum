using System.Collections;
using System.Collections.Generic;

namespace Forum3.Repositories {
	public partial class Repository<T> : IEnumerable<T> {
		public T this[int i] => Records[i];

		protected List<T> Records { get; set; }

		public IEnumerator<T> GetEnumerator() => Records.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}