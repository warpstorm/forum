import initBBCode from './bbcode';
import initEasterEgg from './easter-egg';
import initNavigation from './navigation';
import pageActions from './page-actions';

window.onload = function () {
	initBBCode();
	initEasterEgg();
	initNavigation();
	pageActions();
};