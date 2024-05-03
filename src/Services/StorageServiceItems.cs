namespace FileSystemManager;

public interface IItem {}

public class Setting : IItem
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Value { get; set; }
}
