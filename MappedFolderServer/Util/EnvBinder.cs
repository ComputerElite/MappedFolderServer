namespace MappedFolderServer.Util;

using System;
using System.Reflection;

public static class EnvBinder
{
    public static void PopulateFromEnvironment(object target)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));

        var type = target.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Populate fields
        foreach (var field in fields)
        {
            var value = Environment.GetEnvironmentVariable(field.Name);
            if (value != null)
            {
                object convertedValue = Convert.ChangeType(value, field.FieldType);
                field.SetValue(target, convertedValue);
            }
        }

        // Populate properties
        foreach (var prop in properties)
        {
            if (!prop.CanWrite) continue;

            var value = Environment.GetEnvironmentVariable(prop.Name);
            if (value != null)
            {
                object convertedValue = Convert.ChangeType(value, prop.PropertyType);
                prop.SetValue(target, convertedValue);
            }
        }
    }
}
