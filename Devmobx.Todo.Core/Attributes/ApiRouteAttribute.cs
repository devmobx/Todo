using Microsoft.AspNetCore.Mvc;

namespace Devmobx.Todo.Core.Attributes
{
    public class ApiRouteAttribute : RouteAttribute
    {
        public ApiRouteAttribute(string template = "[controller]/[action]")
            : base($"v{{version:apiVersion}}/{template}")
        {
        }
    }
}



