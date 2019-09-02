import { BBCode } from './services/bbcode';
import { EasterEgg } from './services/easter-egg';
import { Navigation } from './services/navigation';
import { PassedTimeMonitor } from './services/passed-time-monitor';
import { SmileySelector } from './services/smiley-selector';
import { ReactionSelector } from './services/reaction-selector';
import { WhosOnlineMonitor } from './services/whos-online-monitor';

import { TopicIndex } from './pages/topic-index';
import { TopicDisplay } from './pages/topic-display';
import { ManageBoards } from './pages/manage-boards';
import { MessageCreate } from './pages/message-create';
import { EventEdit } from './pages/event-edit';
import { AccountDetails } from './pages/account-details';
import { Process } from './pages/process';

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
	reactionSelector: ReactionSelector;
	whosOnlineMonitor: WhosOnlineMonitor;

	constructor() {
		this.bbCode = new BBCode(document);
		this.easterEgg = new EasterEgg(document);
		this.navigation = new Navigation(document);
		this.smileySelector = new SmileySelector(document);
		this.reactionSelector = new ReactionSelector(document);

		this.passedTimeMonitor = new PassedTimeMonitor(document);
		this.whosOnlineMonitor = new WhosOnlineMonitor(document, this);
	}

	boot() {
		this.establishHubConnection();

		this.bbCode.init();
		this.easterEgg.init();
		this.navigation.init();
		this.smileySelector.init();
		this.reactionSelector.init();
		this.passedTimeMonitor.init();
		this.whosOnlineMonitor.init();

		let pageActions = (<any>window).pageActions;

		switch (pageActions) {
			case 'process':
				let process = new Process();
				process.init();
				break;

			case 'manage-boards':
				let manageBoards = new ManageBoards();
				manageBoards.init();
				break;

			case 'message-create':
				let messageCreate = new MessageCreate();
				messageCreate.init();
				break;

			case 'event-edit':
				let eventEdit = new EventEdit();
				eventEdit.init();
				break;

			case 'topic-display':
				let topicDisplay = new TopicDisplay(this);
				topicDisplay.init();
				break;

			case 'topic-index':
				let topicIndex = new TopicIndex(this);
				topicIndex.init();
				break;

			case 'account-details':
				let accountDetails = new AccountDetails();
				accountDetails.init();
				break;
		}
	}

	establishHubConnection = () => {
		this.hub = new SignalR.HubConnectionBuilder().withUrl('/hub').build();
		this.hub.start().catch(err => console.log('Error while starting connection: ' + err));
		console.log('Hub connection established.');
	}
}