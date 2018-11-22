require('./extensions/element');

import { BBCode } from './bbcode';
import { EasterEgg } from './easter-egg';
import { Navigation } from './navigation';
import { SmileySelector } from './smiley-selector';

import { TopicIndex } from './pages/topic-index';
import { TopicDisplay } from './pages/topic-display';
import { ManageBoards } from './pages/manage-boards';

window.onload = function () {
	let app = new App();
	app.boot();
};

export class App {
	bbCode: BBCode;
	easterEgg: EasterEgg;
	navigation: Navigation;
	smileySelector: SmileySelector;

	boot() {
		this.bbCode = new BBCode(document);
		this.bbCode.init();

		this.easterEgg = new EasterEgg(document);
		this.easterEgg.init();

		this.navigation = new Navigation(document);
		this.navigation.addListeners();

		this.smileySelector = new SmileySelector(document);
		this.smileySelector.init();

		let pageActions = (<any>window).pageActions;

		switch (pageActions) {
			case 'manage-boards':
				let manageBoards = new ManageBoards(document, this);
				manageBoards.init();
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
}