﻿using System.Collections;
using System.Diagnostics.Extensions;
using System.Linq;
using System.Reflection;

namespace System.Diagnostics
{
    /// <summary>
    ///     Source: http://stackoverflow.com/questions/852181/c-printing-all-properties-of-an-object
    /// </summary>
    internal class ObjectDumperConsole : DumperBase
    {
        public ObjectDumperConsole(DumpOptions dumpOptions) : base(dumpOptions)
        {
        }

        public static string Dump(object element, DumpOptions dumpOptions = default(DumpOptions))
        {
            var instance = new ObjectDumperConsole(dumpOptions);
            return instance.DumpElement(element);
        }

        private string DumpElement(object element)
        {
            if (this.Level > this.DumpOptions.MaxLevel)
            {
                return this.ToString();
            }

            if (element == null || element is ValueType || element is string)
            {
                this.StartLine(this.FormatValue(element));
            }
            else
            {
                var objectType = element.GetType();
                if (!typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo()))
                {
                    this.StartLine(GetClassName(element));
                    this.LineBreak();
                    this.AddAlreadyTouched(element);
                    this.Level++;
                }

                var enumerableElement = element as IEnumerable;
                if (enumerableElement != null)
                {
                    foreach (var item in enumerableElement)
                    {
                        if (item is IEnumerable && !(item is string))
                        {
                            this.Level++;
                            this.DumpElement(item);
                            this.Level--;
                        }
                        else
                        {
                            if (!this.AlreadyTouched(item))
                            {
                                this.DumpElement(item);
                            }
                            else
                            {
                                this.Write($"{GetClassName(element)} <-- bidirectional reference found");
                                this.LineBreak();
                            }
                        }
                        this.LineBreak();
                    }
                }
                else
                {
                    var publicFields = element.GetType().GetRuntimeFields().Where(f => !f.IsPrivate);
                    foreach (var fieldInfo in publicFields)
                    {
                        var value = fieldInfo.TryGetValue(element);

                        if (fieldInfo.FieldType.GetTypeInfo().IsValueType || fieldInfo.FieldType == typeof(string))
                        {
                            this.StartLine($"{fieldInfo.Name}: {this.FormatValue(value)}");
                            this.LineBreak();
                        }
                        else
                        {
                            var isEnumerable = typeof(IEnumerable).GetTypeInfo()
                                .IsAssignableFrom(fieldInfo.FieldType.GetTypeInfo());
                            this.StartLine($"{fieldInfo.Name}: {(isEnumerable ? "..." : "{ }")}");
                            this.LineBreak();

                            var alreadyTouched = !isEnumerable && this.AlreadyTouched(value);
                            this.Level++;
                            if (!alreadyTouched)
                            {
                                this.DumpElement(value);
                            }
                            else
                            {
                                this.Write($"{GetClassName(element)} <-- bidirectional reference found");
                                this.LineBreak();
                            }

                            this.Level--;
                        }
                    }

                    var properties = element.GetType().GetRuntimeProperties()
                        .Where(p => p.GetMethod != null && p.GetMethod.IsPublic && p.GetMethod.IsStatic == false)
                        .ToList();

                    if (this.DumpOptions.SetPropertiesOnly)
                    {
                        properties = properties
                            .Where(p => p.SetMethod != null && p.SetMethod.IsPublic && p.SetMethod.IsStatic == false)
                            .ToList();
                    }

                    foreach (var propertyInfo in properties)
                    {
                        var type = propertyInfo.PropertyType;
                        var value = propertyInfo.TryGetValue(element);

                        if (type.GetTypeInfo().IsValueType || type == typeof(string))
                        {
                            this.StartLine($"{propertyInfo.Name}: {this.FormatValue(value)}");
                            this.LineBreak();
                        }
                        else
                        {
                            var isEnumerable = typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
                            this.StartLine($"{propertyInfo.Name}: {(isEnumerable ? "..." : "{ }")}");
                            this.LineBreak();

                            var alreadyTouched = !isEnumerable && this.AlreadyTouched(value);
                            this.Level++;
                            if (!alreadyTouched)
                            {
                                this.DumpElement(value);
                            }
                            else
                            {
                                this.Write($"{GetClassName(element)} <-- bidirectional reference found");
                                this.LineBreak();
                            }

                            this.Level--;
                        }
                    }
                }

                if (!typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo()))
                {
                    this.Level--;
                }
            }

            return this.ToString();
        }

        private string FormatValue(object o)
        {
            if (o == null)
            {
                return "null";
            }

            if (o is string)
            {
                return $"\"{o}\"";
            }

            if (o is char && (char)o == '\0')
            {
                return string.Empty;
            }

            if (o is ValueType)
            {
                return o.ToString();
            }

            if (o is IEnumerable)
            {
                return "...";
            }

            return "{ }";
        }

        private static string GetClassName(object element)
        {
            var type = element.GetType();
            var className = type.GetFormattedName(useFullName: true);
            return $"{{{className}}}";
        }
    }
}