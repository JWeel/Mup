using System.Collections.Generic;

namespace Mup.Helpers
{
    public class Generate
    {
        public static IEnumerable<int> Range(int start, int end) =>
            Generate.Range(start, end, 1);

        public static IEnumerable<int> Range(int start, int end, int step)
        {
            while (start < end)
            {
                yield return start;
                start += step;
            }
        }
    }
}