using System.Collections.Generic;
using VSuiteLab.Models;
using System;
using QueryFilterVm = VSuiteLab.Models.Contexts.QueryFilterVm;

namespace VSuiteLab.Converters;
public abstract class FilterEditorVm
{
    protected QueryFilterVm Filter;

    protected FilterEditorVm(QueryFilterVm filter)
    {
        Filter = filter;   
    }
}

// TEXT
public class TextEditorVm : FilterEditorVm
{
    public TextEditorVm(QueryFilterVm f) : base(f) { }

    public string? Value
    {
        get => (string?)Filter.Value;
        set => Filter.Value = value;
    }
}

// NUMBER
public class NumberEditorVm : FilterEditorVm
{
    public NumberEditorVm(QueryFilterVm f) : base(f) { }

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
public class DateEditorVm : FilterEditorVm
{
    public DateEditorVm(QueryFilterVm f) : base(f) { }

    public DateTimeOffset? Value
    {
        get => Filter.Value as DateTimeOffset?;
        set => Filter.Value = value;
    }
}

// ENUM
public class EnumEditorVm : FilterEditorVm
{
    public EnumEditorVm(QueryFilterVm f) : base(f) { }

    public IEnumerable<object>? Values => Filter.EnumValues;

    public object? Value
    {
        get => Filter.Value;
        set => Filter.Value = value;
    }
}