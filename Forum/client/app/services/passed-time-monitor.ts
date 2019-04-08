import { setTimeout } from "timers";

export class PassedTimeMonitor {
	constructor(private doc: Document) { }

	init(): void {
		this.updateTags();
	}

	updateTags = () => {
		this.doc.querySelectorAll('time').forEach(element => {
			let datetime = element.getAttribute('datetime');
			let date = new Date(datetime || '');
			let passedTime = this.convertToPassedTime(date);

			element.textContent = passedTime;
		});

		setTimeout(this.updateTags, 10000);
	}

	convertToPassedTime(date: Date): string {
		let now = new Date();

		let nowTime = now.getTime();
		let dateTime = date.getTime();

		var difference = nowTime - dateTime;

		var differenceSeconds = difference / 1000;
		var differenceMinutes = difference / 60000;
		var differenceHours = difference / 3600000;
		var differenceDays = difference / 86400000;

		var returnText = "just now";

		if (differenceDays >= 1) {
			var years = now.getFullYear() - date.getFullYear();

			var nowMonth = now.getMonth();
			var dateMonth = date.getMonth();

			if (nowMonth < dateMonth) {
				nowMonth += 11;
			}

			var months = Math.floor(differenceDays / 30);
			var years = Math.floor(months / 12);

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
			else if (differenceDays >= 2) {
				returnText = Math.round(differenceDays) + " days ago";
			}
			else {
				returnText = "1 day ago";
			}
		}
		else if (differenceHours >= 1 && differenceHours < 2) {
			returnText = "1 hour ago";
		}
		else if (differenceHours >= 2) {
			returnText = Math.round(differenceHours) + " hours ago";
		}
		else if (differenceMinutes >= 1 && differenceMinutes < 2) {
			returnText = "1 minute ago";
		}
		else if (differenceMinutes >= 2) {
			returnText = Math.round(differenceMinutes) + " minutes ago";
		}
		else if (differenceSeconds >= 1 && differenceSeconds < 2) {
			returnText = "1 second ago";
		}
		else if (differenceSeconds >= 2) {
			returnText = Math.round(differenceSeconds) + " seconds ago";
		}

		return returnText;
	}
}