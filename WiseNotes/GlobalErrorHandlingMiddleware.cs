using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace WiseNotes;

public class GlobalErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        string type = string.Empty, title = string.Empty;

        var statusCode = HttpStatusCode.InternalServerError;

        if (exception is Exception || exception is Exception)
        {
            statusCode = HttpStatusCode.BadRequest;
            type = "https://example.com/errors/bad-request";
            title = "Bad Request";
        }
        if (exception is Exception || exception is Exception)
        {
            statusCode = HttpStatusCode.NotFound;
            type = "https://example.com/errors/not-found";
            title = "Resource Not Found";
        }

        var problemDetails = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = (int)statusCode,
            Detail = exception.Message,
            Instance = context.Request.Path,
        };

        problemDetails.Extensions.Add("traceId", context.TraceIdentifier);
        problemDetails.Extensions.Add("timestamp", DateTime.UtcNow);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsJsonAsync(problemDetails);
    }
}
