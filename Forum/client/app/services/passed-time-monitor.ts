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

		var difference = (nowTime - dateTime) / 1000;
		var differenceSeconds = difference;
		var differenceMinutes = difference / 60;
		var differenceHours = difference / 3600;
		var differenceDays = difference / 86400;

		var returnText = "just now";

		if (differenceDays >= 1 && differenceDays < 2) {
			returnText = "1 day ago";
		}
		else if (differenceDays >= 2) {
			var years = now.getFullYear() - date.getFullYear() - 1;

			var nowMonth = now.getMonth();
			var dateMonth = date.getMonth();

			if (nowMonth < dateMonth) {
				nowMonth += 11;
			}

			var months = nowMonth - dateMonth;

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
				returnText = Math.round(differenceDays) + " days ago";
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