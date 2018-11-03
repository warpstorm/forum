export default function () {
	var easterEgg = new EasterEgg();
	easterEgg.addEasterEggListener();
}

export class EasterEgg {
	addEasterEggListener(): void {
		let element = document.getElementById('easter-egg');

		element.addEventListener("mouseenter", function () {
			document.getElementById("danger-sign").classList.remove("hidden");
		});

		element.addEventListener("mouseleave", function () {
			document.getElementById("danger-sign").classList.add("hidden");
		});
	}
}