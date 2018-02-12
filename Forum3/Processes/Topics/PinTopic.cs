using Forum3.Contexts;
using Forum3.Exceptions;
using System;
using System.Linq;

namespace Forum3.Processes.Topics {
	using DataModels = Models.DataModels;
	using ServiceModels = Models.ServiceModels;

	public class PinTopic {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }

		public PinTopic(
			ApplicationDbContext dbContext,
			UserContext userContext
		) {
			DbContext = dbContext;
			UserContext = userContext;
		}

		public ServiceModels.ServiceResponse Execute(int messageId) {
			var record = DbContext.Messages.Find(messageId);

			if (record is null)
				throw new HttpNotFoundException($@"No record was found with the id '{messageId}'");

			if (record.ParentId > 0)
				messageId = record.ParentId;

			var existingRecord = DbContext.Pins.FirstOrDefault(p => p.MessageId == messageId && p.UserId == UserContext.ApplicationUser.Id);

			if (existingRecord is null) {
				var pinRecord = new DataModels.Pin {
					MessageId = messageId,
					Time = DateTime.Now,
					UserId = UserContext.ApplicationUser.Id
				};

				DbContext.Pins.Add(pinRecord);
			}
			else
				DbContext.Pins.Remove(existingRecord);

			DbContext.SaveChanges();

			return new ServiceModels.ServiceResponse();
		}
	}
}