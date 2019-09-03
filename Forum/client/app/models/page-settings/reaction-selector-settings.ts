import { ReactionImage } from "../reaction-image";

export class ReactionSelectorSettings {
	public imgurName: string = '';
	public reactionImages: ReactionImage[] = [];

	public constructor(init?: Partial<object>) {
		Object.assign(this, init);
	}
}
