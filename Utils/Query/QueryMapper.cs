using System;
using System.Collections.Generic;
using System.Linq;
using VSuiteLab.Models;
using QueryFilterVm = VSuiteLab.Models.Contexts.QueryFilterVm;
using QueryGroupVm = VSuiteLab.Models.Contexts.QueryGroupVm;
using QuerySortVm = VSuiteLab.Models.Contexts.QuerySortVm;

namespace VSuiteLab.Utils;

public static class QueryMapper
{
    public static QueryHelper.Query ToQueryModel(
        IEnumerable<QueryFilterVm> filters,
        IEnumerable<QuerySortVm>? sorts = null,
        IEnumerable<QueryGroupVm>? groups = null)
    {
        return new QueryHelper.Query
        {
            Filters = filters.Select(f => new QueryHelper.Filter
            {
                Property = f.SelectedField?.Path ?? string.Empty,
                Operator = f.OperatorType,
                Value = f.Value switch
                {
                    Enum e => e,
                    null => null,
                    _ => f.Value
                }
            }).ToList(),

            Sorts = sorts?.Select(s => new QueryHelper.SortRule
            {
                Property = s.SelectedField?.Path ?? "",
                Descending = s.Descending
            }).ToList() ?? new(),

            Groups = groups?.Select(g => new QueryHelper.GroupRule
            {
                Property = g.SelectedField?.Path ?? ""
            }).ToList() ?? new()
        };
    }
}