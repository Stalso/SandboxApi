using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SandboxApi.Filters
{
    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            // Check for authorize attribute
            var hasAuthorize =
                context.ControllerActionDescriptor.FilterDescriptors.Any((x) =>
                    x.Filter.GetType() == typeof(AuthorizeFilter));

            if (hasAuthorize)
            {
                hasAuthorize =
                    context.ControllerActionDescriptor.FilterDescriptors.All(x => x.Filter.GetType() != typeof(AllowAnonymousFilter));
                if (hasAuthorize)
                {
                    operation.Responses.Add("401", new Response { Description = "Unauthorized. May contain header: www-authenticate →Bearer error=\"invalid_token\", error_description=\"The token is expired\". Then wee need to refresh token" });
                    operation.Responses.Add("403", new Response { Description = "Forbidden" });
                }
                
            }
        }
    }
}
