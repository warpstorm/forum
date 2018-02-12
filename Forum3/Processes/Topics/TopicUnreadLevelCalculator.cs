using Forum3.Contexts;
using Forum3.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Processes.Topics {
	using DataModels = Models.DataModels;

	public class TopicUnreadLevelCalculator {
		UserContext UserContext { get; }

		public TopicUnreadLevelCalculator(
			UserContext userContext
		) {
			UserContext = userContext;
		}

		public int Calculate(int messageId, DateTime lastReplyTime, List<DataModels.Participant> participation, List<DataModels.ViewLog> viewLogs) {
			var unread = 1;

			if (UserContext.IsAuthenticated) {
				foreach (var viewLog in viewLogs) {
					switch (viewLog.TargetType) {
						case EViewLogTargetType.All:
							if (viewLog.LogTime >= lastReplyTime)
								unread = 0;
							break;

						case EViewLogTargetType.Message:
							if (viewLog.TargetId == messageId && viewLog.LogTime >= lastReplyTime)
								unread = 0;
							break;
					}

					if (unread == 0)
						break;
				}
			}

			if (unread == 1 && participation.Any(r => r.MessageId == messageId))
				unread = 2;

			return unread;
		}

	}
}