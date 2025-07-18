using Infonetica.WorkflowEngine.Domain.Entities;

namespace Infonetica.WorkflowEngine.Application.Interfaces;

public interface IWorkflowRepository
{
    Task<WorkflowDefinition?> GetDefinitionAsync(string id);
    Task SaveDefinitionAsync(WorkflowDefinition definition);
    Task<WorkflowInstance?> GetInstanceAsync(string id);
    Task SaveInstanceAsync(WorkflowInstance instance);
}
