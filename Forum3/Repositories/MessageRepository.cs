using System;
using System.Collections.Generic;

namespace Forum3.Repositories {
	public class MessageRepository {
		SettingsRepository Settings { get; }

		public MessageRepository(
			SettingsRepository settingsRepository
		) {
			Settings = settingsRepository;
		}

		public int GetPageNumber(int messageId, List<int> messageIds) {
			var index = (double)messageIds.FindIndex(id => id == messageId);
			index++;

			var messagesPerPage = Settings.MessagesPerPage();
			return Convert.ToInt32(Math.Ceiling(index / messagesPerPage));
		}
	}
}