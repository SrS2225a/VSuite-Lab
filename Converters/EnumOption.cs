namespace VSuiteLab.Converters;

public class EnumOption<T>
{
    public T Value { get; }
    public string Label { get; }

    public EnumOption(T value, string label)
    {
        Value = value;
        Label = label;
    }
}