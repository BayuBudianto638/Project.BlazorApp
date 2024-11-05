using Newtonsoft.Json;
using System.Text;

namespace Project.ServiceApi.Middleware
{
    public class SerilogMidleware
    {
        private readonly ILogger<SerilogMidleware> _logger;
        private readonly RequestDelegate _next;

        public SerilogMidleware(RequestDelegate next, ILogger<SerilogMidleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Enable buffering to allow the request body to be read multiple times
            context.Request.EnableBuffering();

            // Create a log object for the request details
            var requestLog = new
            {
                Method = context.Request.Method, // HTTP method (GET, POST, etc.)
                Scheme = context.Request.Scheme, // HTTP or HTTPS
                Host = context.Request.Host.ToString(), // Host (e.g., localhost:5000)
                Path = context.Request.Path, // Request path (e.g., /api/values)
                QueryString = context.Request.QueryString.ToString(), // Query string (e.g., ?id=1)
                Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), // Request headers
                Body = await FormatRequest(context.Request) // Request body
            };

            // Keep the original response body stream
            var originalBodyStream = context.Response.Body;

            // Create a memory stream to temporarily hold the response
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Call the next middleware in the pipeline
            await _next(context);

            // Create a log object for the response details
            var responseLog = new
            {
                StatusCode = context.Response.StatusCode, // HTTP status code (e.g., 200, 404)
                Headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), // Response headers
                Body = await FormatResponse(context.Response) // Response body
            };

            // Combine the request and response log objects into a single log entry
            var logObject = new
            {
                Request = requestLog,
                Response = responseLog
            };

            // Serialize the log object to a JSON string for logging
            var logJson = JsonConvert.SerializeObject(logObject);

            // Log the serialized JSON string using Serilog
            _logger.LogInformation(logJson);

            // Copy the response body back to the original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }

        // Helper method to format the request body as a string
        private async Task<string> FormatRequest(HttpRequest request)
        {
            using var reader = new StreamReader(
                request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            // Reset the request body stream position so it can be read again
            request.Body.Position = 0;

            return body;
        }

        // Helper method to format the response body as a string
        private async Task<string> FormatResponse(HttpResponse response)
        {
            // Set the position of the response body stream to the beginning
            response.Body.Seek(0, SeekOrigin.Begin);

            // Read the response body as a string
            string text = await new StreamReader(response.Body).ReadToEndAsync();

            // Reset the response body stream position so it can be read again
            response.Body.Seek(0, SeekOrigin.Begin);

            return text;
        }
    }
}
