namespace Eventum.DTO;

public class PaginatedResult<T>
{
    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int Count { get; set; }

    public IEnumerable<T> Items { get; set; } = new List<T>();
}