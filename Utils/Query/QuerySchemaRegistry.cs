using System;
using System.Collections.Generic;
using System.Linq;

namespace VSuiteLab.Utils;

public static class QuerySchemaRegistry
{
    private static readonly Dictionary<Type, List<QueryFieldDescriptor>> _schemaRegistry = new();
    
    public static void Register<T>(List<QueryFieldDescriptor> fields)
    {
        _schemaRegistry[typeof(T)] = fields;
    }
    
    public static List<QueryFieldDescriptor> Get<T>()
    {
        return _schemaRegistry.TryGetValue(typeof(T), out var fields)
            ? fields
            : new List<QueryFieldDescriptor>();
    }
    
    public static QueryFieldDescriptor Resolve(string path, Type modelType)
    {
        var schema = _schemaRegistry.TryGetValue(modelType, out var fields)
            ? fields
            : new List<QueryFieldDescriptor>();

        return schema.FirstOrDefault(x => x.Path == path)
               ?? new QueryFieldDescriptor
               {
                   Path = path,
                   Type = QueryFieldType.Text
               };
    }
}