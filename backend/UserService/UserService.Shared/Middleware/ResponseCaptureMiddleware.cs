using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace UserService.Shared.Middleware
{
    public class ResponseCaptureMiddleware
    {
        private readonly RequestDelegate _next;

        public ResponseCaptureMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;

            // Guardar el cuerpo de la respuesta para que el ErrorHandlingMiddleware pueda leerlo
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseBodyContent = await new StreamReader(responseBody).ReadToEndAsync();
            context.Items["ResponseBody"] = responseBodyContent;
        }
    }
}
