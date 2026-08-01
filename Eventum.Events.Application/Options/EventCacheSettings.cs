namespace Eventum.Events.Application.Options;

public class EventCacheSettings
{
    public string ConnectionString { get; set; } = "localhost:6379";

    public int EventTtlSeconds { get; set; } = 300;

    public int TopEventsTtlSeconds { get; set; } = 60;
}
