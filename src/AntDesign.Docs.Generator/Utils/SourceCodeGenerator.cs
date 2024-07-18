using System;
using System.Collections;
using System.Text;

public class SourceCodeGenerator
{
    public static string GenerateSourceCode(object obj, string nameSpace, string className)
    {
        StringBuilder sourceCode = new StringBuilder();
        sourceCode.AppendLine($"namespace {nameSpace};");
        sourceCode.AppendLine($"public static class {className}");
        sourceCode.AppendLine("{");
        sourceCode.AppendLine(GeneratePropertyCode(obj, "Data"));
        sourceCode.AppendLine("}");

        return sourceCode.ToString();
    }

    private static string GeneratePropertyCode(object obj, string propertyName)
    {
        StringBuilder propertyCode = new StringBuilder();
        var isEnumerableType = typeof(IEnumerable).IsAssignableFrom(obj.GetType()) && obj.GetType() != typeof(string);
        var propertyTypeName = isEnumerableType ? GetGenericTypeName(obj.GetType()) : obj.GetType().FullName;

        propertyCode.AppendLine($"    public static {propertyTypeName} {propertyName}");
        propertyCode.AppendLine("    {");
        propertyCode.AppendLine("        get");
        propertyCode.AppendLine("        {");

        if (obj.GetType().IsPrimitive || obj is string)
        {
            propertyCode.AppendLine($"            return {GetValueCode(obj)};");
        }
        else if (isEnumerableType)
        {
            propertyCode.AppendLine($"           return new {GetGenericTypeName(obj.GetType())}");
            propertyCode.AppendLine("                {");

            IEnumerable list = (IEnumerable)obj;
            foreach (var item in list)
            {
                propertyCode.AppendLine($"                    {GetValueCode(item)},");
            }

            propertyCode.AppendLine("                };");
        }
        else
        {
            propertyCode.AppendLine($"            return {GetComplexValueCode(obj)};");
        }

        propertyCode.AppendLine("        }");
        propertyCode.AppendLine("    }");

        return propertyCode.ToString();
    }

    private static string GetValueCode(object value)
    {
        if (value == null)
        {
            return "null";
        }
        else if (value is bool)
        {
            return value.ToString().ToLower();
        }
        else if (value is string)
        {
            return GetFormattedStringValue((string)value);
        }
        else if (!value.GetType().IsValueType)
        {
            return GetComplexValueCode(value);
        }
        return value.ToString();
    }

    private static string GetFormattedStringValue(string value)
    {
        return value==null? "null": "@\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string GetGenericTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericArguments = type.GetGenericArguments();
            var genericTypeName = type.FullName.Substring(0, type.FullName.IndexOf('`'));
            var genericTypeArguments = string.Join(", ", genericArguments.Select(GetGenericTypeName));
            return $"{genericTypeName}<{genericTypeArguments}>";
        }
        return type.FullName;
    }

    private static string GetComplexValueCode(object obj)
    {
        StringBuilder propertyCode = new StringBuilder();
        propertyCode.AppendLine($"           new {obj.GetType().FullName}");
        propertyCode.AppendLine("            {");

        foreach (var property in obj.GetType().GetProperties())
        {
            if (property.PropertyType == typeof(Type))
            {
                var typeName = obj.GetType().GetProperty(property.Name + "Name").GetValue(obj);
                propertyCode.AppendLine($"                {property.Name} = typeof({typeName}),");
                continue;
            }

            if (property.GetValue(obj) == null)
                continue;

            if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string))
            {
                propertyCode.AppendLine($"                {property.Name} = new {GetGenericTypeName(property.PropertyType)}");
                propertyCode.AppendLine("                {");

                IEnumerable list = (IEnumerable)property.GetValue(obj);
                foreach (var item in list)
                {
                    propertyCode.AppendLine($"                    {GetValueCode(item)},");
                }

                propertyCode.AppendLine("                },");
            }
            else if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
            {
                propertyCode.AppendLine($"                {property.Name} = {GeneratePropertyCode(property.GetValue(obj), property.Name)},");
            }
            else if (property.PropertyType == typeof(string))
            {
                propertyCode.AppendLine($"                {property.Name} = {GetFormattedStringValue((string)property.GetValue(obj))},");
            }
            else if (property.PropertyType == typeof(Type))
            {
                var typeName= obj.GetType().GetProperty(property.Name+"Name").GetValue(obj);
                propertyCode.AppendLine($"                {property.Name} = {typeName},");
            }
            else
            {
                propertyCode.AppendLine($"                {property.Name} = {GetValueCode(property.GetValue(obj))},");
            }
        }

        propertyCode.AppendLine("            }");

        return propertyCode.ToString();
    }
}