using System;
using System.Collections;
using System.Text;

public class SourceCodeGenerator
{
    public static string GenerateSourceCode(object obj, string className)
    {
        StringBuilder sourceCode = new StringBuilder();

        sourceCode.AppendLine($"public static class {className}");
        sourceCode.AppendLine("{");
        sourceCode.AppendLine(GeneratePropertyCode(obj, "Data"));
        sourceCode.AppendLine("}");

        return sourceCode.ToString();
    }

    private static string GeneratePropertyCode(object obj, string propertyName)
    {
        StringBuilder propertyCode = new StringBuilder();
        propertyCode.AppendLine($"    public static {obj.GetType().Name} {propertyName}");

        propertyCode.AppendLine("    {");
        propertyCode.AppendLine("        get");
        propertyCode.AppendLine("        {");

        if (obj.GetType().IsPrimitive || obj is string)
        {
            propertyCode.AppendLine($"            return {GetValueCode(obj)};");
        }
        else
        {
            propertyCode.AppendLine($"            return new {obj.GetType().Name}");
            propertyCode.AppendLine("            {");

            foreach (var property in obj.GetType().GetProperties())
            {
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
                else
                {
                    propertyCode.AppendLine($"                {property.Name} = {GetValueCode(property.GetValue(obj))},");
                }
            }

            propertyCode.AppendLine("            };");
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
        else
        {
            return value.ToString();
        }
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
            var genericTypeName = type.Name.Substring(0, type.Name.IndexOf('`'));
            var genericTypeArguments = string.Join(", ", genericArguments.Select(GetGenericTypeName));
            return $"{genericTypeName}<{genericTypeArguments}>";
        }
        return type.Name;
    }
}