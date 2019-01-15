import { OnlineUser } from "../online-user";

export class WhosOnlineMonitorSettings {
	public users: OnlineUser[] = [];

	public constructor(init?: Partial<object>) {
		Object.assign(this, init);
	}
}
