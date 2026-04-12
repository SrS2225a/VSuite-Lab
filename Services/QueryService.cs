using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VSuiteLab.Models;

namespace VSuiteLab.Services;

public class QueryService
{
    public IEnumerable<T> ApplyQuery<T>(IEnumerable<T> items,
        IEnumerable<QueryHelper.Filter>? filters = null, IEnumerable<QueryHelper.SortRule>? sorts = null)
    {
        IEnumerable<T> query = items;
        
        //Console.WriteLine(sorts.Count());
        
        if (filters != null)
        {
            foreach (var filter in filters)
            {
                query = query.Where(t => MatchFilter(t, filter));
            }
        }

        if (sorts != null)
        {
            query = ApplySorting(query, sorts);
        }
        
        return query;
    }

    private bool MatchFilter<T>(T task, QueryHelper.Filter filter)
    {
        // Console.WriteLine($"Matching filter: {filter.Property} {filter.Operator} {filter.Value}");
        var parts = filter.Property.Split('.');
        return MatchPath(task, parts, 0, filter);
    }

    private bool MatchPath(object? current, string[] parts, int index, QueryHelper.Filter filter)
    {
        if (current == null)
            return false;

        // If we've reached the final value, compare it
        if (index >= parts.Length)
            return MatchValue(current, filter.Value!, filter.Operator);

        // Handle collections
        if (current is System.Collections.IEnumerable collection && current is not string)
        {
            foreach (var item in collection)
            {
                if (MatchPath(item, parts, index, filter))
                    return true;
            }

            return false;
        }

        var prop = current.GetType().GetProperty(parts[index],
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (prop == null)
            return false;

        var next = prop.GetValue(current);

        return MatchPath(next, parts, index + 1, filter);
    }
    
    private bool MatchValue(object value, object target, QueryHelper.QueryOperator op)
    {
        var valueType = value.GetType();
        object? convertedTarget;

        try
        {
            if (valueType == typeof(DateTimeOffset))
            {
                convertedTarget = DateTimeOffset.Parse(target.ToString()!);
            }
            else if (valueType == typeof(DateTime))
            {
                convertedTarget = DateTime.Parse(target.ToString()!);
            }
            else if (valueType.IsEnum)
            {
                convertedTarget = Enum.Parse(valueType, target.ToString()!, true);
            }
            else
            {
                convertedTarget = Convert.ChangeType(target, valueType);
            }
        }
        catch
        {
            return false;
        }

        int comparison = 0;

        if (value is IComparable comp && convertedTarget is IComparable compTarget)
        {
            comparison = comp.CompareTo(compTarget);
        }

        switch (op)
        {
            case QueryHelper.QueryOperator.Equals:
                return value.Equals(convertedTarget);

            case QueryHelper.QueryOperator.Contains:
                return value.ToString()?.Contains(
                    convertedTarget?.ToString() ?? string.Empty,
                    StringComparison.CurrentCultureIgnoreCase
                ) ?? false;

            case QueryHelper.QueryOperator.GreaterThan:
                return comparison > 0;

            case QueryHelper.QueryOperator.LessThan:
                return comparison < 0;

            case QueryHelper.QueryOperator.GreaterThanOrEqual:
                return comparison >= 0;

            case QueryHelper.QueryOperator.LessThanOrEqual:
                return comparison <= 0;

            default:
                return false;
        }
    }
    
    private IEnumerable<T> ApplySorting<T>(IEnumerable<T> query, IEnumerable<QueryHelper.SortRule>? sorts)
    {
        IOrderedEnumerable<T>? ordered = null;

        foreach (var rule in sorts ?? Enumerable.Empty<QueryHelper.SortRule>())
        {
            var props = rule.Property.Split('.');
            
            Func<T, object?> keySelector = item => ResolveSortValue(item, props, 0);

            if (ordered == null)
            {
                var enumerable = query as T[] ?? query.ToArray();
                ordered = rule.Descending ? enumerable.OrderByDescending(keySelector) : enumerable.OrderBy(keySelector);
            }
            else
            {
                ordered = rule.Descending
                    ? ordered.ThenByDescending(keySelector)
                    : ordered.ThenBy(keySelector);
            }
        }
        
        return ordered ?? query;
    }

    private object? ResolveSortValue(object current, string[] parts, int index)
    {
        if (index >= parts.Length)
            return NormalizeValue(current);

        if (current is System.Collections.IEnumerable collection && current is not string)
        {
            foreach (var item in collection)
            {
                var value = ResolveSortValue(item, parts, index + 1);
                if (value != null)
                    return value;
            }
        }
        
        var prop = current.GetType().GetProperty(parts[index],
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        
        if (prop == null)
            return null;
        
        var next = prop.GetValue(current);
        return ResolveSortValue(next, parts, index + 1);
    }

    private object? NormalizeValue(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case DateTime dt:
                return dt;
            case DateTimeOffset dto:
                return dto;
        }

        if (value.GetType().IsEnum)
            return (int)value;

        if (value is IComparable)
            return value;

        return value.ToString();
    }

    public IEnumerable<QueryHelper.GroupItems<T>> ApplyGrouping<T>(
        IEnumerable<T> items,
        IEnumerable<QueryHelper.GroupRule>? groups)
    {
        if (groups == null || !groups.Any())
            return new List<QueryHelper.GroupItems<T>>
            {
                new QueryHelper.GroupItems<T> { Key = "(Ungrouped)", Items = items.ToList() }
            };

        var rule = groups.First();
        var remaining = groups.Skip(1);

        var parts = rule.Property.Split('.');

        var grouped = items.GroupBy(item => ResolveGroupValue(item!, parts));

        var result = new List<QueryHelper.GroupItems<T>>();

        foreach (var g in grouped)
        {
            var tg = new QueryHelper.GroupItems<T>
            {
                Key = g.Key ?? "(Ungrouped)"
            };

            var groupRules = remaining as QueryHelper.GroupRule[] ?? remaining.ToArray();
            tg.Items = groupRules.Any() ? ApplyGrouping(g, groupRules).Cast<T>().ToList() : g.ToList();

            result.Add(tg);
        }

        return result;
    }
    
    private object? ResolveGroupValue(object current, string[] parts)
    {
        object? value = current;

        foreach (var part in parts)
        {
            if (value == null)
                return null;

            if (value is System.Collections.IEnumerable collection && value is not string)
            {
                var first = collection.Cast<object>().FirstOrDefault();
                if (first == null)
                    return null;

                value = first;
            }

            var prop = value.GetType().GetProperty(part,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (prop == null)
                return null;

            value = prop.GetValue(value);
        }

        return value;
    }
}