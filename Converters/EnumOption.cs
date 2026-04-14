namespace VSuiteLab.Converters;

public class EnumOption<T>(T value, string label)
{
    public T Value { get; } = value;
    public string Label { get; } = label;
}