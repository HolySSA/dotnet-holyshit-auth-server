using FluentValidation;
using LoginServer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LoginServer.Utils;

public class FluentValidationFilter : IAsyncActionFilter
{
  public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
  {
    if (!context.ModelState.IsValid)
    {
        var errors = context.ModelState
          .Values
          .SelectMany(v => v.Errors)
          .Select(e => e.ErrorMessage)
          .FirstOrDefault();

        context.Result = new BadRequestObjectResult(new ErrorResponseDto 
        { 
          Message = errors ?? "Validation failed" 
        });
        return;
    }

    await next();
  }
}