using System.Collections.Generic;
using System.Text;

namespace WelsonJS.Serialization
{
    public class KVSerializer
    {
        private static Dictionary<string, string> dict = new Dictionary<string, string>();

        public void Add(string key, string value)
        {
            dict[key] = value;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var x in dict)
            {
                sb.Append($"{x.Key}={x.Value}; ");
            }
            if (sb.Length > 0) sb.Length -= 2;

            return sb.ToString();
        }
    }
}
