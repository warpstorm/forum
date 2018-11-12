require('./extensions/element');

import bbCode from './bbcode';
import easterEgg from './easter-egg';
import navigation from './navigation';
import pageActions from './page-actions';

window.onload = function () {
	bbCode();
	easterEgg();
	navigation();
	pageActions();
};