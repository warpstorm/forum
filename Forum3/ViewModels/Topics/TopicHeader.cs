using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.ViewModels.Topics
{
    public class TopicHeader
    {
		public string Subject { get; internal set; }
		public int Views { get; internal set; }
		public string StartedById { get; internal set; }
	}
}
