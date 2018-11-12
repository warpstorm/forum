import { XhrOptions } from "../models/xhr-options";
import { XhrResult } from "../models/xhr-result";
import { throwIfNull } from "../helpers";
import { HttpMethod } from "../definitions/http-method";

export module Xhr {
	export function request(options: XhrOptions) {
		throwIfNull(options, "options");

		if (isFormMethod(options.method) && options.body) {
			throw new Error("Provided XhrOptions contained a body with a method of POST/PUT. This was probably not intentional.");
		}

		return new Promise<XhrResult>((resolve, reject) => {
			let xhr = createXhr(options);

			xhr.ontimeout = () => reject('Request timed out.');
			xhr.onerror = () => reject(xhr.statusText);
			xhr.onload = () => resolve(createXhrResult(xhr));

			if (isFormMethod(options.method)) {
				xhr.send(JSON.stringify(options.body));
			}
			else {
				xhr.send();
			}
		});
	}

	export function createXhr(options: XhrOptions) {
        let xhr = new XMLHttpRequest();

		if (isFormMethod(options.method)) {
            options.headers['Content-Type'] = 'application/json';
		}

		Object.keys(options.headers).forEach(key => xhr.setRequestHeader(key, options.headers[key]));

		xhr.timeout = options.timeout;
		xhr.open(options.method, options.url);

        return xhr;
	}

	export function createXhrResult(xhr: XMLHttpRequest): XhrResult {
		return new XhrResult({
			status: xhr.status,
			statusText: xhr.statusText,
			data: xhr.responseText
		});
	}

	export function isFormMethod(method: HttpMethod): boolean { return [HttpMethod.Post, HttpMethod.Post].includes(method); }
}