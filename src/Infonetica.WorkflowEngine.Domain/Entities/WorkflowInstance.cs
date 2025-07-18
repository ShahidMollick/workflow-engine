namespace Infonetica.WorkflowEngine.Domain.Entities;

public record HistoryEntry(string Action, DateTime Timestamp);

public class WorkflowInstance
{
    public string Id { get; set; } = string.Empty;
    public string DefinitionId { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public List<HistoryEntry> History { get; set; } = new();
}
