using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SandboxApi.Filters
{
    public class FormDataOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var formMediaType = context.ControllerActionDescriptor
                .ActionConstraints.OfType<ConsumesAttribute>()
                .SelectMany(attr => attr.ContentTypes)
                .FirstOrDefault(mediaType => mediaType == "application/x-www-form-urlencoded");

            if (formMediaType != null)
            {
                if (context.ControllerActionDescriptor.AttributeRouteInfo.Template == "connect/token")
                {
                    operation.Parameters = new List<IParameter>()
                    {
                        new NonBodyParameter()
                        {
                            Name = "grant_type",
                            Required = true,
                            Type = "string",
                            In = "formData"
                        },
                        new NonBodyParameter()
                        {
                            Name = "username",
                            Type = "string",
                            In = "formData"
                        },
                        new NonBodyParameter()
                        {
                            Name = "password",
                            Type = "string",
                            In = "formData"
                        },
                    };
                }
                
                operation.Consumes = new[] { formMediaType };
            }
            
        }
    }
}
