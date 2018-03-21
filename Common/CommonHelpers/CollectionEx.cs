using Functional;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonHelpers
{
    using static OptionHelpers;
    public static class CollectionEx
    {
        public static IEnumerable<string> EnumLines(this StringReader reader)
        {
            while (true)
            {
                string line = reader.ReadLine();
                if (null == line) yield break;

                yield return line;
            }
        }

        public static Option<T> Lookup<K, T>(this IDictionary<K, T> dict, K key)
        {
            T value;
            return dict.TryGetValue(key, out value) ? Some(value) : None;
        }
    }
}
