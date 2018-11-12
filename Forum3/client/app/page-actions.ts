import topicIndex from './pages/topic-index';

// Kind of like an inverted routing table.
export default function () {
	let pageActions = (<any>window).pageActions;

	switch (pageActions) {
		case 'topicIndex':
			topicIndex();
			break;
	}
}