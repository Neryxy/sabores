using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SaboresDeMiTierra.Filters
{
    public class LoginFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            string? controller = context.RouteData.Values["controller"]?.ToString();
            string? action = context.RouteData.Values["action"]?.ToString();

            // Login precisa ficar acessível para quem ainda não entrou.
            if (controller == "Login")
                return;

            // Permite a página de erro padrão.
            if (controller == "Home" && action == "Error")
                return;

            int? usuarioId = context.HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
            {
                context.Result = new RedirectToActionResult("Index", "Login", null);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
