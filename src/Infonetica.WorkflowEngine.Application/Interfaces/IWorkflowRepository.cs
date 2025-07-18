using Infonetica.WorkflowEngine.Domain.Entities;

namespace Infonetica.WorkflowEngine.Application.Interfaces;

/// <summary>
/// Repository interface for managing workflow data.
/// I define how to store and retrieve workflows and instances safely.
/// </summary>
public interface IWorkflowRepository
{
    Task<WorkflowDefinition?> GetDefinitionAsync(string id);
    Task<IEnumerable<WorkflowDefinition>> GetAllDefinitionsAsync();
    Task SaveDefinitionAsync(WorkflowDefinition definition);
    Task<WorkflowInstance?> GetInstanceAsync(string id);
    Task SaveInstanceAsync(WorkflowInstance instance);
    
    /// <summary>
    /// Saves an instance only if its version matches the expected version.
    /// This prevents race conditions when multiple users modify the same instance.
    /// Returns true if saved successfully, false if version conflict detected.
    /// </summary>
    Task<bool> SaveInstanceWithVersionCheckAsync(WorkflowInstance instance, long expectedVersion);
}
