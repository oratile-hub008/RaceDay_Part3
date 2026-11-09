using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Dtos;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RaceDay.Api.Swagger;

// Fills the "Request body" box on the Swagger page with a ready-made example,
// so you only have to change the values and press Execute.
public class RequestExamples : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        // Each request class gets one complete example.
        if (context.Type == typeof(RegisterDto))
            schema.Example = new OpenApiObject
            {
                ["firstName"] = new OpenApiString("Sipho"),
                ["lastName"] = new OpenApiString("Dlamini"),
                ["email"] = new OpenApiString("sipho@example.com"),
                ["password"] = new OpenApiString("Password123!"),
                ["confirmPassword"] = new OpenApiString("Password123!"),
                ["role"] = new OpenApiString("Participant"),
                ["phoneNumber"] = new OpenApiString("0821234567")
            };

        else if (context.Type == typeof(LoginDto))
            schema.Example = new OpenApiObject
            {
                ["email"] = new OpenApiString("thandi.organiser@raceday.co.za"),
                ["password"] = new OpenApiString("Password123!"),
                ["rememberMe"] = new OpenApiBoolean(false)
            };

        else if (context.Type == typeof(UpdateProfileDto))
            schema.Example = new OpenApiObject
            {
                ["firstName"] = new OpenApiString("Sipho"),
                ["lastName"] = new OpenApiString("Dlamini"),
                ["phoneNumber"] = new OpenApiString("0821234567")
            };

        else if (context.Type == typeof(EventRequestDto))
            schema.Example = new OpenApiObject
            {
                ["name"] = new OpenApiString("Soweto Community 10K"),
                ["description"] = new OpenApiString("A friendly community run through Soweto."),
                ["eventDate"] = new OpenApiDateTime(DateTime.UtcNow.AddDays(30)),   // always a future date
                ["location"] = new OpenApiString("Soweto, Johannesburg"),
                ["distanceKm"] = new OpenApiInteger(10),
                ["eventType"] = new OpenApiString("Run")
            };

        else if (context.Type == typeof(CategoryRequestDto))
            schema.Example = new OpenApiObject
            {
                ["name"] = new OpenApiString("10km Open"),
                ["description"] = new OpenApiString("Open to all adults.")
            };

        else if (context.Type == typeof(EnrolRequestDto))
            schema.Example = new OpenApiObject
            {
                ["categoryId"] = new OpenApiInteger(1)
            };

        else if (context.Type == typeof(EnrolmentStatusDto))
            schema.Example = new OpenApiObject
            {
                ["status"] = new OpenApiString("Confirmed")
            };

        else if (context.Type == typeof(ResultRequestDto))
            schema.Example = new OpenApiObject
            {
                ["finishTime"] = new OpenApiString("00:52:30"),
                ["position"] = new OpenApiInteger(1)
            };
    }
}
