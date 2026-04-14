using System;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VSuiteLab.Utils.Query;

public partial class QueryFieldDescriptor : ObservableObject
{
    public string Path { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public QueryFieldType Type { get; init; }
    public Type? EnumType { get; init; }
    
    public override bool Equals(object? obj) => ReferenceEquals(this, obj);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}


public enum QueryFieldType
{
    Text,
    Number,
    Boolean,
    Date,
    Enum,
    MultiSelect,
    ObjectPath
}