namespace Forum3.Interfaces {
	using InputModels = Forum3.Models.InputModels;
	using ViewModels = Forum3.Models.ViewModels;

	public interface IControllerProcess {
		ViewModels.Delay Start();
		ViewModels.Delay Continue(InputModels.Continue input);
	}
}