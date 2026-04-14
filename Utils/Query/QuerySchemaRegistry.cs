using System;
using System.Collections.Generic;
using System.Linq;

namespace VSuiteLab.Utils.Query;

public static class QuerySchemaRegistry
{
    private static readonly Dictionary<Type, List<QueryFieldDescriptor>> SchemaRegistry = new();
    
    public static void Register<T>(List<QueryFieldDescriptor> fields)
    {
        SchemaRegistry[typeof(T)] = fields;
    }
    
    public static List<QueryFieldDescriptor> Get<T>()
    {
        return SchemaRegistry.TryGetValue(typeof(T), out var fields)
            ? fields
            : new List<QueryFieldDescriptor>();
    }
    
    public static QueryFieldDescriptor Resolve(string path, Type modelType)
    {
        var schema = SchemaRegistry.TryGetValue(modelType, out var fields)
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