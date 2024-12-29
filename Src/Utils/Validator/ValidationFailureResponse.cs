using FluentValidation;
using LoginServer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LoginServer.Utils;

public class ValidationFailureResponse : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
      if (!context.ModelState.IsValid)
      {
        var errors = context.ModelState
          .Values
          .SelectMany(v => v.Errors)
          .Select(e => e.ErrorMessage)
          .FirstOrDefault();

        var response = new ErrorResponseDto
        {
          Message = errors ?? "회원가입 검증 실패"
        };

        context.Result = new BadRequestObjectResult(response);
      }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}