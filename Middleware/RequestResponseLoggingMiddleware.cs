using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log the request
        context.Request.EnableBuffering();
        var requestBody = await ReadStreamAsync(context.Request.Body);
        _logger.LogInformation($"Incoming Request: {context.Request.Method} {context.Request.Path} {requestBody}");

        // Copy the original response body stream
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Call the next middleware in the pipeline
        await _next(context);

        // Log the response
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        _logger.LogInformation($"Outgoing Response: {context.Response.StatusCode} {responseBody}");
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        // Copy the response back to the original stream
        await responseBodyStream.CopyToAsync(originalResponseBodyStream);
    }

    private async Task<string> ReadStreamAsync(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        var text = await new StreamReader(stream, Encoding.UTF8).ReadToEndAsync();
        stream.Seek(0, SeekOrigin.Begin);
        return text;
    }
}
