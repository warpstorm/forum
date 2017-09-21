using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Models.MigrationModels {
	[Table("Messages")]
	public class Message
	{
		public Message()
		{
			TimePosted = DateTime.Now;
			TimeEdited = DateTime.Now;
		    LastChildId = 0;
            LastChildTimePosted = DateTime.Now;
		}

		public int Id { get; set; }

		public string Subject { get; set; }

		[DataType(DataType.MultilineText)]
		public string Body { get; set; }

		[DataType(DataType.MultilineText)]
		public string OriginalBody { get; set; }

		public DateTime TimePosted { get; set; }
		public DateTime TimeEdited { get; set; }
		public DateTime LastChildTimePosted { get; set; }

		public int PostedById { get; set; }
	    public virtual UserProfile PostedBy { get; set; }

		public int EditedById { get; set; }
	    public virtual UserProfile EditedBy { get; set; }

        public int ParentId { get; set; }
        public int ReplyId { get; set; }
        public int LastChildId { get; set; }

        public int LastChildById { get; set; }
        public virtual UserProfile LastChildBy { get; set; }

	    public int Views { get; set; }
        public int Replies { get; set; }

        public virtual List<MessageThought> Thoughts { get; set; } 
	}
}