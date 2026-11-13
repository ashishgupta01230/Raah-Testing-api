using HautSphere.BuildingBlocks.Configuration.Controller;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

#nullable disable
namespace HautSphere.BuildingBlocks.Configuration.Filters
{
    public class ResponseFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.Controller is not ApiControllerBase controller)
                return;

            

            // Store current controller safely
            context.HttpContext.Items["_CurrentController"] = controller;

            SetMethodType(context, controller);
            controller.SetActionArguments(context.ActionArguments);

            // Validate ModelState
            if (!controller.ModelState.IsValid)
            {
                context.Result = new BadRequestObjectResult(controller.SetValidationResult(context.ModelState));
            }
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var requestPath = context.HttpContext.Request.Path.Value ?? string.Empty;
            if (requestPath.EndsWith(".csv", StringComparison.InvariantCultureIgnoreCase))
            {
                return; // Skip CSV exports
            }
            // Preserve file/stream and status-only results so file downloads and streams are not replaced
            if (context.Result is Microsoft.AspNetCore.Mvc.FileResult ||
                context.Result is StatusCodeResult ||
                context.Result is EmptyResult)
            {
                return;
            }
            if (context.Controller is not ApiControllerBase controller)
                return;

            int? statusCode = null;
            object resultData = null;

            // If action executed successfully
            if (context.Exception == null)
            {
                if (context.Result is ObjectResult objResult)
                {
                    resultData = objResult.Value;
                    statusCode = objResult.StatusCode;
                }
                else if (context.Result is BadRequestResult)
                {
                    statusCode = (int)HttpStatusCode.BadRequest;
                }
            }

            // Safe route action name check
            var actionName = context.RouteData.Values["action"]?.ToString()?.ToLower() ?? string.Empty;

            if (!string.IsNullOrEmpty(actionName) && !actionName.Contains("export"))
            {
                context.Result = new ObjectResult(controller.SetData(resultData))
                {
                    StatusCode = statusCode
                };
            }
            else
            {
                context.Result = new ObjectResult(resultData)
                {
                    StatusCode = statusCode
                };
            }
        }

        private void SetMethodType(ActionExecutingContext context, ApiControllerBase controller)
        {
            var method = context.HttpContext.Request.Method.ToLower();

            switch (method)
            {
                case "get":
                    controller.SetResponseModel("get");
                    break;
                case "delete":
                    controller.SetResponseModel("delete");
                    break;
                case "post":
                case "put":
                    controller.SetPostResponseModel(method);
                    break;
            }
        }
    }

}
