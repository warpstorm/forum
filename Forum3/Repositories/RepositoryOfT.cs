using Forum3.Errors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Repositories {
	public partial class Repository<T> : IEnumerable<T> where T : class {
		public T this[int i] => Records[i];

		protected List<T> Records { get; set; }

		protected ILogger Log { get; }

		public Repository(
			ILogger log
		) {
			Log = log;
		}

		public T First(Func<T, bool> predicate) {
			var record = Records.FirstOrDefault(predicate);

			if (record == default(T)) {
				Log.LogError("No record was found matching the predicate.");
				throw new HttpNotFoundError();
			}

			return record;
		}

		public IEnumerator<T> GetEnumerator() => Records.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}