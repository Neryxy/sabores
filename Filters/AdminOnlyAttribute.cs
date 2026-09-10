using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SaboresDeMiTierra.Filters
{
    public class AdminOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string? cargo = context.HttpContext.Session.GetString("UsuarioCargo");

            bool isAdmin = string.Equals(cargo, "Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(cargo, "Administrador", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }
    }
}
