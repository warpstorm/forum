import { XhrOptions } from "../models/xhr-options";
import { XhrResult } from "../models/xhr-result";
import { throwIfNull } from "../helpers";
import { HttpMethod } from "../definitions/http-method";

export module Xhr {
	export function request(options: XhrOptions): Promise<XhrResult> {
		throwIfNull(options, "options");

		return new Promise<XhrResult>((resolve, reject) => {
			let xhr = new XMLHttpRequest();
			xhr.open(options.method, options.url);
			xhr.timeout = options.timeout;
			xhr.responseType = options.responseType;

			xhr.setRequestHeader("X-Requested-With", "XMLHttpRequest");

			if (options.method == HttpMethod.Post) {
				options.headers['Content-Type'] = 'application/x-www-form-urlencoded;charset=UTF-8';
			}

			Object.keys(options.headers).forEach(key => xhr.setRequestHeader(key, options.headers[key]));

			xhr.ontimeout = () => logRejected('Request timed out.', reject);
			xhr.onerror = () => logRejected(xhr.statusText, reject);
			xhr.onload = () => resolve(createXhrResult(xhr));

			xhr.send(options.body);
		});
	}

	export async function requestPartialView(options: XhrOptions, targetDoc: Document): Promise<XhrResult> {
		throwIfNull(options, "options");
		options.responseType = 'document';

		let xhrResult = await request(options);
		importResultDocument(targetDoc, xhrResult);
		return xhrResult;
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

	export function logRejected(reason: any, reject: (reason?: any) => void): void {
		console.log('Xhr was rejected: ' + reason);
		reject(reason);
	}

	export function isFormMethod(method: HttpMethod): boolean { return [HttpMethod.Post, HttpMethod.Post].includes(method); }

	export function importResultDocument(currentDoc: Document, result: XhrResult) {
		let responseDocument = <Document>result.response;
		let resultDocument = <HTMLElement>responseDocument.documentElement;
		let resultBody = <HTMLBodyElement>resultDocument.querySelector('body');

		resultBody.childNodes.forEach(node => {
			let newElement = node as Element;

			if (newElement && newElement.tagName) {
				if (newElement.tagName.toLowerCase() == 'script' && newElement.textContent) {
					eval(newElement.textContent);
				}
				else {
					let targetId = newElement.getAttribute('id');

					if (targetId) {
						let targetElement = currentDoc.querySelector(`#${targetId}`);

						if (targetElement) {
							targetElement.replaceWith(newElement);
						}
					}
				}
			}
		});
	}
}