namespace Forum3 {
	public static class Constants {
		public static class Keys {
			public const string User = nameof(User);
			public const string UserId = nameof(UserId);
			public const string LastPostTimestamp = nameof(LastPostTimestamp);
			public const string LastProcessedToken = nameof(LastProcessedToken);
			public const string StatusMessage = nameof(StatusMessage);
			public const string StorageConnection = nameof(StorageConnection);
		}

		public static class Settings {
			public const string AvatarSize = nameof(AvatarSize);
			public const string HistoryTimeLimit = nameof(HistoryTimeLimit);
			public const string MessagesPerPage = nameof(MessagesPerPage);
			public const string OnlineTimeLimit = nameof(OnlineTimeLimit);
			public const string PopularityLimit = nameof(PopularityLimit);
			public const string TopicsPerPage = nameof(TopicsPerPage);
		}
	}
}