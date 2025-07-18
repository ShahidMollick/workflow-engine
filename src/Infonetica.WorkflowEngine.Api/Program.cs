using Infonetica.WorkflowEngine.Application.DTOs;
using Infonetica.WorkflowEngine.Application.Interfaces;
using Infonetica.WorkflowEngine.Application.Services;
using Infonetica.WorkflowEngine.Infrastructure.Persistence;
using Infonetica.WorkflowEngine.Api.Middleware;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Enhanced logging configuration
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    
    // Set log levels
    logging.SetMinimumLevel(LogLevel.Information);
});

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IWorkflowRepository, InMemoryWorkflowRepository>();
builder.Services.AddScoped<WorkflowService>();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Middleware pipeline (ORDER MATTERS!)
// 1. Request timing - tracks how long requests take
app.UseMiddleware<RequestTimingMiddleware>();

// 2. Correlation ID - adds unique tracking ID to each request
app.UseMiddleware<CorrelationIdMiddleware>();

// 3. Global exception handling - catches all errors and formats them nicely
app.UseMiddleware<GlobalExceptionMiddleware>();

// 4. Standard ASP.NET Core middleware
app.UseHttpsRedirection();
app.UseCors("AllowAll");

// API Endpoints with enhanced documentation
// Each endpoint is documented with clear purpose and expected behavior

// GET /workflows - Retrieve all workflow definitions
// Returns: 200 OK with array of WorkflowDefinition objects
// Purpose: Lists all available workflow templates that can be instantiated
app.MapGet("/workflows", async (WorkflowService workflowService) =>
{
    var definitions = await workflowService.GetAllWorkflowDefinitionsAsync();
    return Results.Ok(definitions);
})
.WithName("GetAllWorkflows")
.WithOpenApi();

// POST /workflows - Create a new workflow definition
// Body: CreateWorkflowRequest (JSON with states and actions)
// Returns: 201 Created with WorkflowDefinition object
// Validates: Exactly one initial state, unique IDs, referential integrity
// Purpose: Creates a reusable workflow template with states and allowed transitions
app.MapPost("/workflows", async (CreateWorkflowRequest request, WorkflowService workflowService) =>
{
    var definition = await workflowService.CreateWorkflowDefinitionAsync(request);
    return Results.Created($"/workflows/{definition.Id}", definition);
})
.WithName("CreateWorkflow")
.WithOpenApi();

// POST /workflows/{definitionId}/instances - Start a new workflow instance
// Path Parameter: definitionId (string) - ID of the workflow definition to instantiate
// Returns: 201 Created with WorkflowInstanceResponse object
// Validates: Definition exists, has initial state
// Purpose: Creates a new running instance of a workflow, starting at the initial state
// Note: Each instance maintains its own state and execution history
app.MapPost("/workflows/{definitionId}/instances", async (string definitionId, WorkflowService workflowService) =>
{
    var instance = await workflowService.StartInstanceAsync(definitionId);
    return Results.Created($"/instances/{instance.Id}", instance);
})
.WithName("StartWorkflowInstance")
.WithOpenApi();

// GET /instances/{instanceId} - Get current status of a workflow instance
// Path Parameter: instanceId (string) - ID of the workflow instance to query
// Returns: 200 OK with WorkflowInstanceResponse object
// Response includes: Current state, definition ID, execution history
// Purpose: Retrieves the current state and complete audit trail of a workflow instance
// Use case: Monitoring workflow progress, debugging, status reporting
app.MapGet("/instances/{instanceId}", async (string instanceId, WorkflowService workflowService) =>
{
    var instance = await workflowService.GetInstanceStatusAsync(instanceId);
    return Results.Ok(instance);
})
.WithName("GetInstanceStatus")
.WithOpenApi();

// POST /instances/{instanceId}/execute - Execute an action to transition workflow state
// Path Parameter: instanceId (string) - ID of the workflow instance to modify
// Body: ExecuteActionRequest (JSON with ActionId)
// Returns: 200 OK with updated WorkflowInstanceResponse object
// Validates: Instance exists, action is valid for current state, action is enabled, not in final state
// Purpose: Advances workflow instance through state transitions by executing allowed actions
// Side effects: Updates instance state, adds entry to execution history with timestamp
app.MapPost("/instances/{instanceId}/execute", async (string instanceId, ExecuteActionRequest request, WorkflowService workflowService) =>
{
    var instance = await workflowService.ExecuteActionAsync(instanceId, request);
    return Results.Ok(instance);
})
.WithName("ExecuteAction")
.WithOpenApi();

app.Run();
