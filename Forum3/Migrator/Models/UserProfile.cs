using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Migrator.Models {
	[Table("UserProfile")]
	public class UserProfile
	{
        private DateTime _lastOnline = DateTime.Now;
	    private DateTime _registered = DateTime.Now;
        private DateTime _birthday = new DateTime(1900, 1, 1);

	    [Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int UserId { get; set; }
	
		[EmailAddress]
		public string UserName { get; set; }

        [StringLength(64)]
        public string DisplayName { get; set; }

	    public DateTime Birthday {
	        get { return _birthday; }
	        set { _birthday = value; }
	    }

	    public DateTime LastOnline {
	        get { return _lastOnline; }
	        set { _lastOnline = value; }
	    }

	    public DateTime Registered {
	        get { return _registered; }
	        set { _registered = value; }
	    }

	    public virtual List<InviteOnlyTopicUsers> Invitations { get; set; }
	}
}
