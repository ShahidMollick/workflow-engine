using Infonetica.WorkflowEngine.Application.Interfaces;
using Infonetica.WorkflowEngine.Domain.Entities;

namespace Infonetica.WorkflowEngine.Infrastructure.Persistence;

/// <summary>
/// In-memory implementation of workflow repository.
/// I store everything in memory - perfect for demos and testing, but don't use me in production!
/// I use locks to make sure multiple users don't corrupt data when accessing the same workflow.
/// </summary>
public class InMemoryWorkflowRepository : IWorkflowRepository
{
    private readonly Dictionary<string, WorkflowDefinition> _definitions = new();
    private readonly Dictionary<string, WorkflowInstance> _instances = new();
    private readonly object _lockObject = new(); // Thread safety for concurrent access

    public Task<WorkflowDefinition?> GetDefinitionAsync(string id)
    {
        lock (_lockObject)
        {
            if (_definitions.TryGetValue(id, out var definition))
            {
                // Return a copy to prevent modification of stored definition
                var copy = new WorkflowDefinition(
                    definition.Id,
                    new List<State>(definition.States.Select(s => new State(s.Id, s.IsInitial, s.IsFinal, s.Enabled))),
                    new List<Domain.Entities.Action>(definition.Actions.Select(a => new Domain.Entities.Action(a.Id, a.Enabled, new List<string>(a.FromStates), a.ToState)))
                );
                return Task.FromResult<WorkflowDefinition?>(copy);
            }
            return Task.FromResult<WorkflowDefinition?>(null);
        }
    }

    public Task<IEnumerable<WorkflowDefinition>> GetAllDefinitionsAsync()
    {
        lock (_lockObject)
        {
            // Return copies to prevent modification of stored definitions
            var copies = _definitions.Values.Select(definition => new WorkflowDefinition(
                definition.Id,
                new List<State>(definition.States.Select(s => new State(s.Id, s.IsInitial, s.IsFinal, s.Enabled))),
                new List<Domain.Entities.Action>(definition.Actions.Select(a => new Domain.Entities.Action(a.Id, a.Enabled, new List<string>(a.FromStates), a.ToState)))
            )).ToList();
            return Task.FromResult<IEnumerable<WorkflowDefinition>>(copies);
        }
    }

    public Task SaveDefinitionAsync(WorkflowDefinition definition)
    {
        lock (_lockObject)
        {
            _definitions[definition.Id] = definition;
            return Task.CompletedTask;
        }
    }

    public Task<WorkflowInstance?> GetInstanceAsync(string id)
    {
        lock (_lockObject)
        {
            if (_instances.TryGetValue(id, out var instance))
            {
                // Return a copy to prevent modification of stored instance
                var copy = new WorkflowInstance
                {
                    Id = instance.Id,
                    DefinitionId = instance.DefinitionId,
                    CurrentState = instance.CurrentState,
                    History = new List<HistoryEntry>(instance.History),
                    Version = instance.Version,
                    LastModified = instance.LastModified
                };
                return Task.FromResult<WorkflowInstance?>(copy);
            }
            return Task.FromResult<WorkflowInstance?>(null);
        }
    }

    public Task SaveInstanceAsync(WorkflowInstance instance)
    {
        lock (_lockObject)
        {
            instance.LastModified = DateTime.UtcNow;
            _instances[instance.Id] = instance;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Saves an instance only if the version hasn't changed (optimistic concurrency control).
    /// This prevents two users from overwriting each other's changes.
    /// </summary>
    public Task<bool> SaveInstanceWithVersionCheckAsync(WorkflowInstance instance, long expectedVersion)
    {
        lock (_lockObject)
        {
            // Check if instance exists and version matches
            if (_instances.TryGetValue(instance.Id, out var existingInstance))
            {
                if (existingInstance.Version != expectedVersion)
                {
                    return Task.FromResult(false); // Version conflict - someone else modified it
                }
            }

            // Save the instance with updated timestamp
            instance.LastModified = DateTime.UtcNow;
            _instances[instance.Id] = instance;
            return Task.FromResult(true);
        }
    }
}
