using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Forum.Interfaces.Services {
	using ServiceModels = Forum.Models.ServiceModels;

	public interface IForumViewResult {
		IActionResult RedirectToReferrer(Controller controller);
		Task<IActionResult> RedirectFromService(Controller controller, ServiceModels.ServiceResponse serviceResponse, Func<Task<IActionResult>> failureCallback);
		IActionResult RedirectToLocal(Controller controller, string returnUrl);
		Task<IActionResult> ViewResult(Controller controller);
		Task<IActionResult> ViewResult(Controller controller, object model);
		Task<IActionResult> ViewResult(Controller controller, string viewName, object model = null);
	}
}