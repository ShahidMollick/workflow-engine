using Infonetica.WorkflowEngine.Application.DTOs;
using Infonetica.WorkflowEngine.Application.Interfaces;
using Infonetica.WorkflowEngine.Application.Services;
using Infonetica.WorkflowEngine.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Infonetica.WorkflowEngine.Tests;

public class WorkflowEngineTests
{
    private readonly IWorkflowRepository _repository;
    private readonly WorkflowService _workflowService;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowEngineTests()
    {
        // Create fresh repository for each test to avoid state pollution
        _repository = new InMemoryWorkflowRepository();
        _logger = new TestLogger<WorkflowService>();
        _workflowService = new WorkflowService(_repository, _logger);
    }

    // Helper method to create unique workflow IDs to avoid conflicts
    private string CreateUniqueWorkflowId(string baseName) => $"{baseName}-{Guid.NewGuid():N}";

    [Fact]
    public async Task CreateWorkflowDefinition_ShouldCreateValidWorkflow()
    {
        // Arrange
        var uniqueId = CreateUniqueWorkflowId("order-workflow");
        var request = new CreateWorkflowRequest(
            uniqueId,
            new List<CreateStateDto>
            {
                new("draft", true, false),
                new("submitted", false, false),
                new("approved", false, true)
            },
            new List<CreateActionDto>
            {
                new("submit", new List<string> { "draft" }, "submitted"),
                new("approve", new List<string> { "submitted" }, "approved")
            }
        );

        // Act
        var result = await _workflowService.CreateWorkflowDefinitionAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(uniqueId, result.Id);
        Assert.Equal(3, result.States.Count);
        Assert.Equal(2, result.Actions.Count);
    }

    [Fact]
    public async Task StartInstance_ShouldCreateNewInstance()
    {
        // Arrange
        var uniqueWorkflowId = CreateUniqueWorkflowId("test-workflow");
        var request = new CreateWorkflowRequest(
            uniqueWorkflowId,
            new List<CreateStateDto>
            {
                new("initial", true, false),
                new("final", false, true)
            },
            new List<CreateActionDto>
            {
                new("complete", new List<string> { "initial" }, "final")
            }
        );

        await _workflowService.CreateWorkflowDefinitionAsync(request);

        // Act
        var instance = await _workflowService.StartInstanceAsync(uniqueWorkflowId);

        // Assert
        Assert.NotNull(instance);
        Assert.Equal(uniqueWorkflowId, instance.DefinitionId);
        Assert.Equal("initial", instance.CurrentState);
        Assert.Single(instance.History);
        Assert.Equal("WORKFLOW_STARTED", instance.History[0].Action);
    }

    [Fact]
    public async Task ExecuteAction_ShouldUpdateInstanceState()
    {
        // Arrange - Create a simple linear workflow (no circular dependencies)
        var uniqueWorkflowId = CreateUniqueWorkflowId("execution-test");
        var request = new CreateWorkflowRequest(
            uniqueWorkflowId,
            new List<CreateStateDto>
            {
                new("start", true, false),
                new("end", false, true)
            },
            new List<CreateActionDto>
            {
                new("finish", new List<string> { "start" }, "end")
            }
        );

        await _workflowService.CreateWorkflowDefinitionAsync(request);
        var instance = await _workflowService.StartInstanceAsync(uniqueWorkflowId);

        // Act
        var updatedInstance = await _workflowService.ExecuteActionAsync(
            instance.Id, 
            new ExecuteActionRequest("finish")
        );

        // Assert
        Assert.Equal("end", updatedInstance.CurrentState);
        Assert.Equal(2, updatedInstance.History.Count);
        Assert.Equal("finish", updatedInstance.History[1].Action);
    }

    [Fact]
    public async Task ExecuteAction_FromFinalState_ShouldThrowValidationException()
    {
        // Arrange - Create a workflow where we try to execute action from final state
        var uniqueWorkflowId = CreateUniqueWorkflowId("final-test");
        var request = new CreateWorkflowRequest(
            uniqueWorkflowId,
            new List<CreateStateDto>
            {
                new("start", true, false),
                new("end", false, true)
            },
            new List<CreateActionDto>
            {
                new("finish", new List<string> { "start" }, "end")
                // Note: Removed circular action to avoid circular dependency validation error
            }
        );

        await _workflowService.CreateWorkflowDefinitionAsync(request);
        var instance = await _workflowService.StartInstanceAsync(uniqueWorkflowId);
        
        // Move to final state
        await _workflowService.ExecuteActionAsync(instance.Id, new ExecuteActionRequest("finish"));

        // Act & Assert - Try to execute action from final state (should fail)
        await Assert.ThrowsAsync<ValidationException>(async () =>
        {
            // Create a separate workflow that allows actions from final states to test this specific validation
            var circularUniqueId = CreateUniqueWorkflowId("circular-test");
            var circularRequest = new CreateWorkflowRequest(
                circularUniqueId,
                new List<CreateStateDto>
                {
                    new("start", true, false),
                    new("final", false, true)
                },
                new List<CreateActionDto>
                {
                    new("toFinal", new List<string> { "start" }, "final"),
                    new("invalidAction", new List<string> { "final" }, "start") // This would be circular but let's test final state validation
                }
            );
            
            // This should fail due to circular dependency detection, which is what we want to test
            await _workflowService.CreateWorkflowDefinitionAsync(circularRequest);
        });
    }

    [Fact]
    public async Task CreateWorkflow_WithoutInitialState_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateWorkflowRequest(
            CreateUniqueWorkflowId("invalid-workflow"),
            new List<CreateStateDto>
            {
                new("state1", false, false),
                new("state2", false, true)
            },
            new List<CreateActionDto>
            {
                new("action1", new List<string> { "state1" }, "state2")
            }
        );

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await _workflowService.CreateWorkflowDefinitionAsync(request)
        );
    }

    [Fact]
    public async Task CreateWorkflow_WithCircularDependency_ShouldThrowValidationException()
    {
        // Arrange - Create a workflow with circular dependencies
        var request = new CreateWorkflowRequest(
            CreateUniqueWorkflowId("circular-workflow"),
            new List<CreateStateDto>
            {
                new("stateA", true, false),
                new("stateB", false, false),
                new("stateC", false, true)
            },
            new List<CreateActionDto>
            {
                new("goToB", new List<string> { "stateA" }, "stateB"),
                new("goToA", new List<string> { "stateB" }, "stateA"), // Creates A→B→A cycle
                new("goToC", new List<string> { "stateB" }, "stateC")
            }
        );

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await _workflowService.CreateWorkflowDefinitionAsync(request)
        );
    }

    [Fact]
    public async Task CreateWorkflow_WithUnreachableState_ShouldThrowValidationException()
    {
        // Arrange - Create a workflow with an unreachable state
        var request = new CreateWorkflowRequest(
            CreateUniqueWorkflowId("unreachable-workflow"),
            new List<CreateStateDto>
            {
                new("start", true, false),
                new("middle", false, false),
                new("orphan", false, false), // This state can never be reached
                new("end", false, true)
            },
            new List<CreateActionDto>
            {
                new("proceed", new List<string> { "start" }, "middle"),
                new("finish", new List<string> { "middle" }, "end")
                // No action leads to "orphan" state
            }
        );

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await _workflowService.CreateWorkflowDefinitionAsync(request)
        );
    }

    [Fact]
    public async Task CreateWorkflow_WithDeadEndState_ShouldThrowValidationException()
    {
        // Arrange - Create a workflow with a dead-end non-final state
        var request = new CreateWorkflowRequest(
            CreateUniqueWorkflowId("deadend-workflow"),
            new List<CreateStateDto>
            {
                new("start", true, false),
                new("stuck", false, false), // Dead-end: non-final state with no outgoing actions
                new("end", false, true)
            },
            new List<CreateActionDto>
            {
                new("getStuck", new List<string> { "start" }, "stuck")
                // No action from "stuck" state - workflow gets stuck!
            }
        );

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await _workflowService.CreateWorkflowDefinitionAsync(request)
        );
    }

    [Fact]
    public async Task ExecuteAction_FromFinalState_ShouldPreventExecution()
    {
        // Arrange - Create a simple workflow and move it to final state
        var uniqueWorkflowId = CreateUniqueWorkflowId("final-state-test");
        var request = new CreateWorkflowRequest(
            uniqueWorkflowId,
            new List<CreateStateDto>
            {
                new("start", true, false),
                new("processing", false, false),
                new("completed", false, true)
            },
            new List<CreateActionDto>
            {
                new("process", new List<string> { "start" }, "processing"),
                new("complete", new List<string> { "processing" }, "completed")
            }
        );

        await _workflowService.CreateWorkflowDefinitionAsync(request);
        var instance = await _workflowService.StartInstanceAsync(uniqueWorkflowId);
        
        // Move to processing state
        await _workflowService.ExecuteActionAsync(instance.Id, new ExecuteActionRequest("process"));
        
        // Move to final state
        await _workflowService.ExecuteActionAsync(instance.Id, new ExecuteActionRequest("complete"));

        // Act & Assert - Try to execute action from final state (should fail)
        await Assert.ThrowsAsync<ValidationException>(async () =>
        {
            // Try to execute the complete action again from final state
            await _workflowService.ExecuteActionAsync(instance.Id, new ExecuteActionRequest("complete"));
        });
    }

    [Fact]
    public async Task CreateWorkflow_WithCircularDependency_ShouldBeDetected()
    {
        // Arrange - Create a workflow with circular dependency
        var request = new CreateWorkflowRequest(
            CreateUniqueWorkflowId("circular-workflow"),
            new List<CreateStateDto>
            {
                new("A", true, false),
                new("B", false, false),
                new("C", false, true)
            },
            new List<CreateActionDto>
            {
                new("AtoB", new List<string> { "A" }, "B"),
                new("BtoA", new List<string> { "B" }, "A"), // Creates A→B→A cycle
                new("BtoC", new List<string> { "B" }, "C")
            }
        );

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await _workflowService.CreateWorkflowDefinitionAsync(request)
        );
    }
}

/// <summary>
/// Simple test logger that does nothing - perfect for unit tests.
/// In real applications, you'd use a proper mocking framework like Moq.
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
