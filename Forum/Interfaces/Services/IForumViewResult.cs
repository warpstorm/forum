using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Forum.Interfaces.Services {
	using ServiceModels = Forum.Models.ServiceModels;

	public interface IForumViewResult {
		IActionResult RedirectToReferrer(Controller controller);
		Task<IActionResult> RedirectFromService(Controller controller, ServiceModels.ServiceResponse serviceResponse, Func<Task<IActionResult>> failureCallback);
		IActionResult RedirectToLocal(Controller controller, string returnUrl);
		IActionResult ViewResult(Controller controller);
		IActionResult ViewResult(Controller controller, object model);
		IActionResult ViewResult(Controller controller, string viewName, object model = null);
	}
}