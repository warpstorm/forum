using System;

namespace Forum3.Helpers {
	public class ChildMessageException : Exception {
		public int ChildId { get; private set; }
		public int? ParentId { get; private set; }

		public ChildMessageException(int childId, int? parentId) {
			ChildId = childId;
			ParentId = parentId;
		}
	}
}
