namespace Leaderboard.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<ScoreEvent> ScoreEvents { get; set; } = new List<ScoreEvent>();

    public UserAggregate? Aggregate { get; set; }
}