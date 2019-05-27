import flatpickr from "flatpickr";

export class EventCreate {
	init() {
		flatpickr('#Start', {
			enableTime: true,
			dateFormat: "Y-m-d H:i",
		});

		flatpickr('#End', {
			enableTime: true,
			dateFormat: "Y-m-d H:i",
		});
	}
}
