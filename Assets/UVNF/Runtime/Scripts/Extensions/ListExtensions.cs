using System;
using System.Collections.Generic;

namespace UVNF.Extensions
{
    public static class ListExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            Random rng = new Random();
            Shuffle(list, rng);
        }

        public static void Shuffle<T>(this IList<T> list, int seed)
        {
            Random rng = new Random(seed);
            Shuffle(list, rng);
        }

        public static void Shuffle<T>(this IList<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
