using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Forum3.Annotations;
using Microsoft.AspNetCore.Mvc;

namespace Forum3.Controllers {
	[RequireRemoteHttps]
	public class Boards : Controller {
	}
}