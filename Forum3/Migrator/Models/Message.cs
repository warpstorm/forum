using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Migrator.Models {
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
		public int EditedById { get; set; }
        public int ParentId { get; set; }
        public int ReplyId { get; set; }
        public int LastChildId { get; set; }
        public int LastChildById { get; set; }
	    public int Views { get; set; }
        public int Replies { get; set; }
	}
}