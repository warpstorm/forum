using Forum.Services.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Views.Shared.Components.EventsList {
	using ViewModels = Models.ViewModels.Topics;

	public class EventsListViewComponent : ViewComponent {
		ApplicationDbContext DbContext { get; }

		public EventsListViewComponent(ApplicationDbContext dbContext) => DbContext = dbContext;

		public async Task<IViewComponentResult> InvokeAsync() {
			var now = DateTime.Now;
			var today = DateTime.Now.Date;

			var eventsQuery = from eventDetails in DbContext.Events
							  join topic in DbContext.Topics on eventDetails.TopicId equals topic.Id
							  where eventDetails.End >= (eventDetails.AllDay ? today : now)
							  orderby eventDetails.Start
							  select new ViewModels.EventPreview {
								  TopicId = topic.Id,
								  Title = topic.FirstMessageShortPreview,
								  Start = eventDetails.Start,
								  End = eventDetails.End,
								  AllDay = eventDetails.AllDay
							  };

			var eventsList = await eventsQuery.Take(4).ToListAsync();

			return View("Default", eventsList);
		}
	}
}
