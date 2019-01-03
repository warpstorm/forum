import { BBCode } from './services/bbcode';
import { EasterEgg } from './services/easter-egg';
import { Navigation } from './services/navigation';
import { PassedTimeMonitor } from './services/passed-time-monitor';
import { SmileySelector } from './services/smiley-selector';
import { WhosOnlineMonitor } from './services/whos-online-monitor';

import { TopicIndex } from './pages/topic-index';
import { TopicDisplay } from './pages/topic-display';
import { ManageBoards } from './pages/manage-boards';
import { MessageCreate } from './pages/message-create';

import * as SignalR from "@aspnet/signalr";

window.onload = function () {
	let app = new App();
	app.boot();
};

export class App {
	hub?: SignalR.HubConnection = undefined;
	bbCode: BBCode;
	easterEgg: EasterEgg;
	navigation: Navigation;
	passedTimeMonitor: PassedTimeMonitor;
	smileySelector: SmileySelector;
	whosOnlineMonitor: WhosOnlineMonitor;

	constructor() {
		this.bbCode = new BBCode(document);
		this.easterEgg = new EasterEgg(document);
		this.navigation = new Navigation(document);
		this.smileySelector = new SmileySelector(document);

		this.passedTimeMonitor = new PassedTimeMonitor(document);
		this.whosOnlineMonitor = new WhosOnlineMonitor(document, this);
	}

	boot() {
		this.establishHubConnection();

		this.bbCode.init();
		this.easterEgg.init();
		this.navigation.addListeners();
		this.smileySelector.init();
		this.passedTimeMonitor.init();
		this.whosOnlineMonitor.init();

		let pageActions = (<any>window).pageActions;

		switch (pageActions) {
			case 'manage-boards':
				let manageBoards = new ManageBoards(document, this);
				manageBoards.init();
				break;

			case 'message-create':
				let messageCreate = new MessageCreate(document, this);
				messageCreate.init();
				break;

			case 'topic-display':
				let topicDisplay = new TopicDisplay(document, this);
				topicDisplay.init();
				break;

			case 'topic-index':
				let topicIndex = new TopicIndex(document, this);
				topicIndex.init();
				break;
		}
	}

	establishHubConnection = () => {
		this.hub = new SignalR.HubConnectionBuilder().withUrl('/hub').build();
		this.hub.start().catch(err => console.log('Error while starting connection: ' + err));
		console.log('Hub connection established.');
	}
}