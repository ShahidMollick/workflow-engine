namespace Infonetica.WorkflowEngine.Domain.Entities;

public record HistoryEntry(string Action, DateTime Timestamp);

/// <summary>
/// Represents a running instance of a workflow.
/// I keep track of where we are in the workflow and what happened so far.
/// The Version property helps prevent conflicts when multiple people try to modify me at once.
/// </summary>
public class WorkflowInstance
{
    public string Id { get; set; } = string.Empty;
    public string DefinitionId { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public List<HistoryEntry> History { get; set; } = new();
    public long Version { get; set; } = 0; // For optimistic concurrency control
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}
