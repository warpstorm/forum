using Microsoft.AspNetCore.Mvc;
using System;

namespace Forum.Interfaces.Services {
	using ServiceModels = Forum.Models.ServiceModels;

	public interface IForumViewResult {
		IActionResult RedirectToReferrer(Controller controller);
		IActionResult RedirectFromService(Controller controller, ServiceModels.ServiceResponse serviceResponse, Func<IActionResult> failureCallback = null);
		IActionResult RedirectToLocal(Controller controller, string returnUrl);
		IActionResult ViewResult(Controller controller);
		IActionResult ViewResult(Controller controller, object model);
		IActionResult ViewResult(Controller controller, string viewName, object model = null);
	}
}