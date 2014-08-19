using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal static class GenericExtensions
{
    [DebuggerStepThrough]
    public static void CheckNullArgument<T>(this T value, string name = "argument") where T : class
    {
        if (value == null)
            throw new ArgumentException(string.Format("The paramter {0} cannot be Null.", name));
    }

    [DebuggerStepThrough]
    public static void CheckDefaultArgument<T>(this T value, string name = "argument")
    {
        if (EqualityComparer<T>.Default.Equals(default(T), value))
            throw new ArgumentException(string.Format("The paramter {0} cannot be a default value.", name));
    }
}