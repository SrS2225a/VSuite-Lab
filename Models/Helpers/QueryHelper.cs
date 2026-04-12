using System.Collections.Generic;
using VSuiteLab.Utils;

namespace VSuiteLab.Models;

public class QueryHelper
{
    public enum QueryOperator
    {
        Equals,
        Contains,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
    }
    
    
    public class Filter
    {
        public string Property { get; set; } = "";
        public object? Value { get; set; }
        public QueryOperator Operator { get; set; }
    }
    
    public class SortRule 
    {
        public string Property { get; set; } = "";
        public bool Descending { get; set; }
    }
    
    public class GroupRule
    {
        public string Property { get; set; } = string.Empty;
    }
    
    public class Query
    {
        public List<Filter> Filters { get; set; } = new();
        public List<SortRule> Sorts { get; set; } = new();
        public List<GroupRule> Groups { get; set; } = new();
    }

    public class GroupItems<T>
    {
        public object? Key { get; set; }
        public List<T> Items { get; set; } = new();
    }
}