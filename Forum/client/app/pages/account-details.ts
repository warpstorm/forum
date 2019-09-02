import { show, hide, postToPath, throwIfNull } from "../helpers";
import { AccountDetailsSettings } from "../models/page-settings/account-details-settings";
import { App } from "../app";

function getSettings(): AccountDetailsSettings {
	let genericWindow = <any>window;

	return new AccountDetailsSettings({
		imgurClientId: genericWindow.imgurClientId
	});
}

export class AccountDetails {
	private settings: AccountDetailsSettings;

	constructor(private app: App) {
		throwIfNull(app, 'app');

		this.settings = getSettings();
	}

	init(): void {
		this.bindEvents();
	}

	bindEvents = () => {
		document.querySelectorAll('.birthday-toggle').forEach(element => {
			element.addEventListener('click', this.eventToggleBirthdaySelectors);
		});

		document.querySelectorAll('#link-imgur-account-button').forEach(element => {
			element.addEventListener('click', this.eventLinkImgurAccount);
		});
	}

	eventToggleBirthdaySelectors = (event: Event) => {
		let birthdayToggle = <HTMLInputElement>event.currentTarget;

		document.querySelectorAll('.birthday-selectors').forEach(element => {
			if (birthdayToggle.checked) {
				show(element);
			}
			else {
				hide(element);
			}
		});
	}

	eventLinkImgurAccount = (): void => {
		let self = this;
		let url = `https://api.imgur.com/oauth2/authorize?client_id=${self.settings.imgurClientId}&response_type=token`;

		var popup = window.open(url, "Imgur", 'toolbar=0,status=0,width=942,height=559,modal=yes,alwaysRaised=yes');

		let checkConnect = setInterval(function () {
			if (!popup) {
				return;
			}

			if (!popup.document.location.hash) {
				return;
			}

			let hash = popup.document.location.hash;
			console.log('hash found');
			console.log(popup.document.location.hash);

			if (hash.indexOf('access_token') != -1) {
				console.log('access_token found');
				clearInterval(checkConnect);

				let accessToken = self.find(hash, 'access_token');
				let expiresIn = self.find(hash, 'expires_in');
				let refreshToken = self.find(hash, 'refresh_token');
				let accountUsername = self.find(hash, 'account_username');
				let accountId = self.find(hash, 'account_id');

				popup.close();

				console.log('posting to path');
				console.log(`${self.app.baseUrl}/Account/LinkImgur`);

				postToPath(`${self.app.baseUrl}/Account/LinkImgur`, {
					id: accountId,
					username: accountUsername,
					accessToken: accessToken,
					expiresIn: expiresIn,
					refreshToken: refreshToken,
				});
			}
		}, 100);
	}

	find(hash: string, name: string) {
		let results = new RegExp(`[#&]${name}=([^&]*)`).exec(hash);

		if (results) {
			return results[1];
		}

		return '';
	}
}
