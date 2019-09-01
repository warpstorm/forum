import { throwIfNull } from "../helpers";
import { HttpMethod } from "../definitions/http-method";
import { App } from "../app";
import { Xhr } from "./xhr";

import { XhrOptions } from "../models/xhr-options";
import { WhosOnlineMonitorSettings } from "../models/page-settings/whos-online-monitor-settings";

function getSettings(): WhosOnlineMonitorSettings {
	let genericWindow = <any>window;

	return new WhosOnlineMonitorSettings({
		users: genericWindow.users
	});
}

export class WhosOnlineMonitor {
	private settings: WhosOnlineMonitorSettings;
	private recentRequest: boolean = false;

	constructor(private doc: Document, private app: App) {
		throwIfNull(doc, 'doc');
		throwIfNull(app, 'app');

		this.settings = getSettings();
	}

	init(): void {
		if (document.querySelector("#sidebar-whos-online")) {
			if (this.app.hub) {
				this.bindHubActions();
			}

			this.bindChicletMonitor();
		}
	}

	bindHubActions(): void {
		if (!this.app.hub) {
			throw new Error('Hub not defined.');
		}

		this.app.hub.on('whos-online', this.hubWhosOnline);
	}

	bindChicletMonitor(): void {
		this.doc.querySelectorAll('.whos-online-chiclet').forEach(element => {
			let chicletTimeValue = element.getAttribute('time');

			if (chicletTimeValue) {
				let chicletTime = new Date(chicletTimeValue);
				
				// 5 minute expiration
				let expiration = new Date(chicletTime.getTime() + 5 * 60 * 1000); 

				let difference = expiration.getTime() - new Date().getTime();

				setTimeout(() => {
					element.classList.remove('chiclet-green');
					element.classList.add('chiclet-gray');
				}, difference);
			}
		});
	}

	updateChicletTimes(): void {
		this.settings.users.forEach(user => {
			this.doc.querySelectorAll(`.whos-online-chiclet[user="${user.id}"]`).forEach(element => {
				element.setAttribute('time', user.time);
			});
		});
	}

	hubWhosOnline = async () => {
		if (!this.recentRequest) {
			this.recentRequest = true;

			let requestOptions = new XhrOptions({
				method: HttpMethod.Get,
				url: '/Home/Dynamic/OnlineUsersList',
				responseType: 'document'
			});

			await Xhr.loadView(requestOptions, this.doc);

			this.settings = getSettings();
			this.updateChicletTimes();
			this.bindChicletMonitor();
			this.app.navigation.init();
						
			setTimeout(() => {
				this.recentRequest = false;
			}, 10000);
		}
	}
}
