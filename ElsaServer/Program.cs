using System.Net;
using System.Net.Mail;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddElsa(elsa =>
{
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore());
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore());
    
    elsa.UseIdentity(identity =>
    {
        identity.TokenOptions = options => options.SigningKey = "sufficiently-large-secret-signing-key"; // This key needs to be at least 256 bits long.
        identity.UseAdminUserProvider();
    });
    
    elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());
    elsa.UseWorkflowsApi();
    elsa.UseRealTimeWorkflows();
    elsa.UseCSharp();
    elsa.UseJavaScript(options => options.AllowClrAccess = true);
    elsa.UseHttp();
    elsa.UseEmail(email =>
    {
        email.ConfigureOptions = options => builder.Configuration.GetSection("Email").Bind(options);
    });
    elsa.UseWebhooks(webhooks => webhooks.WebhookOptions = options => builder.Configuration.GetSection("Webhooks").Bind(options));
    elsa.UseScheduling();
    elsa.AddActivitiesFrom<Program>();
    elsa.AddWorkflowsFrom<Program>();
});

builder.Services.AddCors(cors => cors
    .AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("x-elsa-workflow-instance-id"))); // Required for Elsa Studio in order to support running workflows from the designer. Alternatively, you can use the `*` wildcard to expose all headers.

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors();
app.UseRouting(); // Required for SignalR.
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi(); // Use Elsa API endpoints.
app.UseWorkflows(); // Use Elsa middleware to handle HTTP requests mapped to HTTP Endpoint activities.
app.UseWorkflowsSignalRHubs(); // Optional SignalR integration. Elsa Studio uses SignalR to receive real-time updates from the server. 

app.Run("http://localhost:5000");
