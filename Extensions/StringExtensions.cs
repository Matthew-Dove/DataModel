using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal static class StringExtensions
{
    /// <summary>
    /// Checks if a string object is Null, empty or whitespace.
    /// If it is a ArgumentException is thrown.
    /// </summary>
    /// <param name="name">The name of the variable being checked</param>
    [DebuggerStepThrough]
    public static void CheckArgument(this string value, string name = "argument")
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(string.Format("The paramter {0} cannot be Null, Empty, or Whitespace.", name));
    }
}
