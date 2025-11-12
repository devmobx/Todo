using Microsoft.AspNetCore.Mvc;

namespace TodoApi.App.Attributes
{
    public class ApiRouteAttribute : RouteAttribute
    {
        public ApiRouteAttribute(string template = "[controller]/[action]")
            : base($"v{{version:apiVersion}}/{template}")
        {
        }
    }
}



