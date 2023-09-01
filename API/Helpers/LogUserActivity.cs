using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Helpers
{
    public class LogUserActivity : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var resultContext = await next();

            if(!resultContext.HttpContext.User.Identity.IsAuthenticated) return;

            var userId = resultContext.HttpContext.User.GetUserId();

            var unit = resultContext.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
            var user = await unit.UserRepository.GetUserByIdAsync(userId);
            user.LastActive = DateTime.UtcNow;
            await unit.Complete();
            
        }
    }
}
