using System.Collections.Generic;
using System;
using QueryFilterVm = VSuiteLab.Models.Contexts.QueryFilterVm;

namespace VSuiteLab.Converters;
public abstract class FilterEditorVm(QueryFilterVm filter)
{
    protected readonly QueryFilterVm Filter = filter;
}

// TEXT
public class TextEditorVm(QueryFilterVm f) : FilterEditorVm(f)
{
    public string? Value
    {
        get => (string?)Filter.Value;
        set => Filter.Value = value;
    }
}

// NUMBER
public class NumberEditorVm(QueryFilterVm f) : FilterEditorVm(f)
{
    public decimal Value
    {
        get => Convert.ToDecimal(Filter.Value ?? 0);
        set => Filter.Value = value;
    }
}

// BOOL
public class BoolEditorVm : FilterEditorVm
{
    public BoolEditorVm(QueryFilterVm f) : base(f) { }

    public bool Value
    {
        get => Filter.Value is bool b && b;
        set => Filter.Value = value;
    }
}

// DATE
public class DateEditorVm(QueryFilterVm f) : FilterEditorVm(f)
{
    public DateTimeOffset? Value
    {
        get => Filter.Value as DateTimeOffset?;
        set => Filter.Value = value;
    }
}

// ENUM
public class EnumEditorVm(QueryFilterVm f) : FilterEditorVm(f)
{
    public IEnumerable<object>? Values => Filter.EnumValues;

    public object? Value
    {
        get => Filter.Value;
        set => Filter.Value = value;
    }
}