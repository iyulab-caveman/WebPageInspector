using System.Text.RegularExpressions;

namespace WebPageInspector
{
    public static partial class Functions
    {
        public static string Pretty(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            value = string.Join(Environment.NewLine, value.Split("\n").Select(p => p.Trim()).Where(p => p.Length > 0));

            return value;
        }
    }
}
