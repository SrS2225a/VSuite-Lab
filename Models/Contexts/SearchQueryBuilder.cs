using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using VSuiteLab.Converters;
using VSuiteLab.Utils;

namespace VSuiteLab.Models.Contexts;

public class SearchQueryBuilder
{
    public ObservableCollection<QueryFieldDescriptor> AvailableFields { get; } = new();
    public List<QueryHelper.QueryOperator> AvailableSearchOperators { get; } =
        Enum.GetValues(typeof(QueryHelper.QueryOperator))
            .Cast<QueryHelper.QueryOperator>()
            .ToList();

    public ObservableCollection<QueryFilterVm> Filters { get; } = new();
    public ObservableCollection<QuerySortVm> Sorts { get; } = new();
    public ObservableCollection<QueryGroupVm> Groups { get; } = new();
    
    public void SetAvailableFields(IEnumerable<QueryFieldDescriptor> fields)
    {
        AvailableFields.Clear();

        foreach (var f in fields)
            AvailableFields.Add(f);
        
        foreach (var filter in Filters)
            filter.NotifySchemaChanged();
        
        foreach (var sort in Sorts)
            sort.NotifySchemaChanged();
        
        foreach (var group in Groups)
            group.NotifySchemaChanged();
    }
}

public partial class QueryFilterVm : ObservableObject
{
    private readonly SearchQueryBuilder _builder;

    public QueryFilterVm(SearchQueryBuilder builder)
    {
        _builder = builder;
    }

    public ObservableCollection<QueryFieldDescriptor> AvailableFields
        => _builder.AvailableFields;

    public List<QueryHelper.QueryOperator> AvailableSearchOperators
        => _builder.AvailableSearchOperators;
    
    public void NotifySchemaChanged()
    {
        OnPropertyChanged(nameof(AvailableFields));
        OnPropertyChanged(nameof(EnumValues));
        OnPropertyChanged(nameof(AvailableSearchOperators));
    }

    [ObservableProperty]
    private QueryHelper.QueryOperator operatorType;

    [ObservableProperty]
    private object? value;

    [ObservableProperty]
    private QueryFieldType fieldType;
    
    [ObservableProperty] 
    private QueryFieldDescriptor? selectedField;
    
    public object Editor => FieldType switch
    {
        QueryFieldType.Number => new NumberEditorVm(this),
        QueryFieldType.Boolean => new BoolEditorVm(this),
        QueryFieldType.Date => new DateEditorVm(this),
        QueryFieldType.Enum => new EnumEditorVm(this),
        _ => new TextEditorVm(this)
    };
    
    public IEnumerable<object>? EnumValues =>
        SelectedField?.EnumType?.IsEnum == true
            ? Enum.GetValues(SelectedField.EnumType).Cast<object>()
            : null;
    
    partial void OnSelectedFieldChanged(QueryFieldDescriptor? newField)
    {
        var newType = newField?.Type ?? QueryFieldType.Text;
        
        if (Equals(newType, FieldType))
            return;

        FieldType = newType;

        Value = newType switch
        {
            QueryFieldType.Date => DateTime.Now,
            QueryFieldType.Number => 0,
            QueryFieldType.Boolean => false,
            QueryFieldType.Text => string.Empty,
            QueryFieldType.MultiSelect => new List<string>(),
            QueryFieldType.ObjectPath => string.Empty,
            QueryFieldType.Enum => null,
            _ => null
        };

        OnPropertyChanged(nameof(Editor));
        OnPropertyChanged(nameof(EnumValues));
    }
}

public partial class QuerySortVm : ObservableObject
{
    private readonly SearchQueryBuilder _builder;

    public QuerySortVm(SearchQueryBuilder builder)
    {
        _builder = builder;
    }
    
    public void NotifySchemaChanged()
    {
        OnPropertyChanged(nameof(AvailableFields));
        OnPropertyChanged(nameof(AvailableSearchOperators));
    }


    public ObservableCollection<QueryFieldDescriptor> AvailableFields
        => _builder.AvailableFields;

    public List<QueryHelper.QueryOperator> AvailableSearchOperators
        => _builder.AvailableSearchOperators;
    
    [ObservableProperty] 
    private QueryFieldDescriptor? selectedField;

    [ObservableProperty]
    private bool descending;
}

public partial class QueryGroupVm : ObservableObject
{
    private readonly SearchQueryBuilder _builder;

    public QueryGroupVm(SearchQueryBuilder builder)
    {
        _builder = builder;
    }
    
    public void NotifySchemaChanged()
    {
        OnPropertyChanged(nameof(AvailableFields));
        OnPropertyChanged(nameof(AvailableSearchOperators));
    }


    public ObservableCollection<QueryFieldDescriptor> AvailableFields
        => _builder.AvailableFields;

    public List<QueryHelper.QueryOperator> AvailableSearchOperators
        => _builder.AvailableSearchOperators;
    
    [ObservableProperty] 
    private QueryFieldDescriptor? selectedField;
}