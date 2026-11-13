using HautSphere.BuildingBlocks.Configuration.Controller;
using HautSphere.BuildingBlocks.Configuration.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace HautSphere.BuildingBlocks.Configuration.Filters
{
    public class ExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<ExceptionFilter> _logger;

        public ExceptionFilter(ILogger<ExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "Unhandled exception occurred: {Message}", context.Exception.Message);

            var statusCode = HandleExceptionAndSetStatusCode(context);

            if (context.HttpContext.Items["_CurrentController"] is not ApiControllerBase controller)
            {
                context.Result = new ObjectResult(new
                {
                    Message = statusCode == (int)HttpStatusCode.InternalServerError
                        ? "Some error occurred, Please contact System Administrator."
                        : context.Exception.InnerException?.Message ?? context.Exception.Message
                })
                { StatusCode = statusCode };
                context.ExceptionHandled = true;
                return;
            }

            if (statusCode == (int)HttpStatusCode.InternalServerError)
            {
                controller.ResponseModel.SetErrorMessage("Some error occurred, Please contact System Administrator.", context.Exception.Message);
            }
            else
            {
                controller.ResponseModel.SetErrorMessage(context.Exception.InnerException?.Message ?? context.Exception.Message, context.Exception?.StackTrace);
            }

            var result = new ObjectResult(controller.ResponseModel);
            result.StatusCode = statusCode;
            context.Result = result;
            controller.SetData(null);
            context.ExceptionHandled = true;
        }

        private int HandleExceptionAndSetStatusCode(ExceptionContext context)
        {
            if (context.Exception is ValidationFailureException)
            {
                return (int)HttpStatusCode.BadRequest;
            }
            if (context.Exception is InvalidDataException)
            {
                return (int)HttpStatusCode.BadRequest;
            }
            else if (context.Exception is KeyNotFoundException)
            {
                return (int)HttpStatusCode.NotFound;
            }
            else if (context.Exception is InvalidOperationException)
            {
                return (int)HttpStatusCode.BadRequest;
            }
            else if (context.Exception is MemberAccessException)
            {
                return (int)HttpStatusCode.Unauthorized;
            }
            else if (context.Exception is UnauthorizedAccessException)
            {
                return (int)HttpStatusCode.Forbidden;
            }
            return (int)HttpStatusCode.InternalServerError;
        }
    }
}
