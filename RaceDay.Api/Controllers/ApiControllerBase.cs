using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Controllers;

/// <summary>Base class for every controller. [ApiController] checks the validation rules on the DTOs for us.</summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>The Id of the logged-in user, read from the session.</summary>
    protected int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
