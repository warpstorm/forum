using System;

namespace Forum.Extensions {
	public static class ToPassedTimeStringExtension {
		public static string ToPassedTimeString(this DateTime date) {
			var now = DateTime.Now;

			var difference = now - date;

			var returnText = "Just now";

			if (difference.TotalDays >= 1 && difference.TotalDays < 2) {
				returnText = "1 day ago";
			}
			else if (difference.TotalDays >= 2) {
				var longDifference = DateTime.MinValue + difference;
				var years = longDifference.Year - 1;
				var months = longDifference.Month - 1;

				if (years == 1) {
					returnText = "1 year ago";
				}
				else if (years > 1) {
					returnText = years + " years ago";
				}
				else if (months == 1) {
					returnText = "1 month ago";
				}
				else if (months > 1) {
					returnText = months + " months ago";
				}
				else {
					returnText = Math.Round(difference.TotalDays) + " days ago";
				}
			}
			else if (difference.TotalHours >= 1 && difference.TotalHours < 2) {
				returnText = "1 hour ago";
			}
			else if (difference.TotalHours >= 2) {
				returnText = Math.Round(difference.TotalHours) + " hours ago";
			}
			else if (difference.TotalMinutes >= 1 && difference.TotalMinutes < 2) {
				returnText = "1 minute ago";
			}
			else if (difference.TotalMinutes >= 2) {
				returnText = Math.Round(difference.TotalMinutes) + " minutes ago";
			}
			else if (difference.TotalSeconds >= 1 && difference.TotalSeconds < 2) {
				returnText = "1 second ago";
			}
			else if (difference.TotalSeconds >= 2) {
				returnText = Math.Round(difference.TotalSeconds) + " seconds ago";
			}

			return returnText;
		}
	}
}