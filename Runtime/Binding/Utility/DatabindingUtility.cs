using System.Collections.Generic;
using System.Text;
namespace Bodardr.Databinding.Runtime
{
    public static class DatabindingUtility
    {
        public static string PrintPath(this List<BindingPropertyEntry> entries)
        {
            if (entries.Count < 1)
                return string.Empty;

            var str = new StringBuilder();
            foreach (var member in entries)
            {
                str.Append(member.Name);
                str.Append('.');
            }

            //We remove the last dot.
            str.Remove(str.Length - 1, 1);
            return str.ToString();
        }
    }
}
