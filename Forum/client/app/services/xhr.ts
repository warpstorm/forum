import { XhrOptions } from "../models/xhr-options";
import { XhrResult } from "../models/xhr-result";
import { throwIfNull } from "../helpers";
import { HttpMethod } from "../definitions/http-method";

export module Xhr {
	export function request(options: XhrOptions) {
		throwIfNull(options, "options");

		return new Promise<XhrResult>((resolve, reject) => {
			let xhr = new XMLHttpRequest();
			xhr.open(options.method, options.url);
			xhr.timeout = options.timeout;
			xhr.responseType = options.responseType;

			Object.keys(options.headers).forEach(key => xhr.setRequestHeader(key, options.headers[key]));

			xhr.ontimeout = () => reject('Request timed out.');
			xhr.onerror = () => reject(xhr.statusText);
			xhr.onload = () => resolve(createXhrResult(xhr));

			xhr.send(options.body);
		});
	}

	export function createXhrResult(xhr: XMLHttpRequest): XhrResult {
		switch (xhr.responseType) {
			case 'document':
				return new XhrResult({
					status: xhr.status,
					statusText: xhr.statusText,
					response: xhr.response
				});

			default:
			case 'text':
				return new XhrResult({
					status: xhr.status,
					statusText: xhr.statusText,
					responseText: xhr.responseText
				});
		}
	}

	export function onIsRejected(reason: any) {
		console.log('Xhr was rejected: ' + reason);
	}

	export function isFormMethod(method: HttpMethod): boolean { return [HttpMethod.Post, HttpMethod.Post].includes(method); }
}