using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VSuiteLab.Models;

namespace VSuiteLab.Utils;

public class QueryUtils
{
    /// <summary>
    /// Parses a string query into a structured QueryHelper.Query object containing filters, sorts, and groups.
    /// </summary>
    /// <param name="query">The query string to be parsed. If null or empty, an empty Query object is returned.</param>
    /// <returns>A QueryHelper.Query object that contains the parsed filters, sorts, and groups.</returns>
    public static QueryHelper.Query ParseQuery(string query)
    {
        var result = new QueryHelper.Query();
        
        if(string.IsNullOrEmpty(query))
            return result;
        
        var tokens = Tokenize(query);

        foreach (var token in tokens)
        {
            if (token.StartsWith("sort:", StringComparison.OrdinalIgnoreCase))
            {
                result.Sorts.Add(ParseSort(token));
            }
            else if (token.StartsWith("group:", StringComparison.OrdinalIgnoreCase))
            {
                result.Groups.Add(ParseGroup(token));
            }
            else
            {
                result.Filters.Add(ParseFilter(token));
            }
        }
        
        return result;
    }

    private static List<string> Tokenize(string query)
    {
        // Split the query string into tokens
        var matches = Regex.Matches(query, @"[^\s""]+|""([^""]*)""");

        return matches
            .Select(m => m.Value.Trim('"'))
            .ToList();
    }

    private static QueryHelper.SortRule ParseSort(string token)
    {
        // Extract the property name and sort direction (optional)
        var parts = token.Substring(5).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return new QueryHelper.SortRule
        {
            Property = parts[0],
            Descending = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase)
        };
    }
    
    
    private static QueryHelper.Filter ParseFilter(string token)
    {
        // Extract the property name, operator, and value
        var match = Regex.Match(token, @"([\w\.]+)(>=|<=|>|<|=|:)(.+)");

        if (!match.Success)
            throw new Exception($"Invalid query token: {token}");

        var property = match.Groups[1].Value;
        var op = match.Groups[2].Value;
        var value = match.Groups[3].Value;

        var operatorType = op switch
        {
            ":" => QueryHelper.QueryOperator.Contains,
            "=" => QueryHelper.QueryOperator.Equals,
            ">" => QueryHelper.QueryOperator.GreaterThan,
            "<" => QueryHelper.QueryOperator.LessThan,
            ">=" => QueryHelper.QueryOperator.GreaterThanOrEqual,
            "<=" => QueryHelper.QueryOperator.LessThanOrEqual,
            _ => throw new Exception($"Unknown operator {op}")
        };

        return new QueryHelper.Filter
        {
            Property = property,
            Operator = operatorType,
            Value = value
        };
    }
    
    private static QueryHelper.GroupRule ParseGroup(string token)
    {
        // Extract the property name
        var property = token.Substring(6);

        return new QueryHelper.GroupRule
        {
            Property = property
        };
    }
}