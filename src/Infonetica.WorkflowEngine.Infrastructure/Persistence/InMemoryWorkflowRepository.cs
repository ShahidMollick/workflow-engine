using Infonetica.WorkflowEngine.Application.Interfaces;
using Infonetica.WorkflowEngine.Domain.Entities;

namespace Infonetica.WorkflowEngine.Infrastructure.Persistence;

public class InMemoryWorkflowRepository : IWorkflowRepository
{
    private readonly Dictionary<string, WorkflowDefinition> _definitions = new();
    private readonly Dictionary<string, WorkflowInstance> _instances = new();

    public Task<WorkflowDefinition?> GetDefinitionAsync(string id)
    {
        _definitions.TryGetValue(id, out var definition);
        return Task.FromResult(definition);
    }

    public Task SaveDefinitionAsync(WorkflowDefinition definition)
    {
        _definitions[definition.Id] = definition;
        return Task.CompletedTask;
    }

    public Task<WorkflowInstance?> GetInstanceAsync(string id)
    {
        _instances.TryGetValue(id, out var instance);
        return Task.FromResult(instance);
    }

    public Task SaveInstanceAsync(WorkflowInstance instance)
    {
        _instances[instance.Id] = instance;
        return Task.CompletedTask;
    }
}
