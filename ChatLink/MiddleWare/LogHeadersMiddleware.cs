namespace ChatLink.MiddleWare;

public class LogHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public LogHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // Accessing and logging request headers
        foreach (var header in context.Request.Headers)
        {
            // Log or inspect headers as needed
            Console.WriteLine($"{header.Key}: {header.Value}");
        }

        /*   // Log request body content
           using (StreamReader reader = new StreamReader(context.Request.Body))
           {
               string requestBody = await reader.ReadToEndAsync();
               Console.WriteLine($"Request Body: {requestBody}");
           }

           context.Request.Body.Position = 0; */

        await _next(context);
    }
}
