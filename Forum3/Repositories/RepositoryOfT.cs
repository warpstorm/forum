using Forum3.Errors;
using Forum3.Interfaces.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Repositories {
	public abstract class Repository<T> : IRepository<T> where T : class {
		public T this[int i] => Records[i];

		protected List<T> Records => _Records ?? (_Records = GetRecords());
		List<T> _Records;

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

		protected abstract List<T> GetRecords();
	}
}