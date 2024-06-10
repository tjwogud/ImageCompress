namespace ImageFormat
{
    public class KmpSearch
    {
        public struct Result(int index, int length)
        {
            public int index = index, length = length;
        }

        public static int[] GetPi<T>(ReadOnlySpan<T> target)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            int[] pi = new int[target.Length];
            int j = 0;
            for (int i = 1; i < target.Length; i++)
            {
                while (j > 0 && !comparer.Equals(target[i], target[j]))
                    j = pi[j - 1];
                if (comparer.Equals(target[i], target[j]))
                    pi[i] = ++j;
            }
            return pi;
        }

        public static Result SearchLongest<T>(ReadOnlySpan<T> space, ReadOnlySpan<T> target)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            int[] pi = GetPi(target);
            int index = -1, length = 0;

            int start = -1;
            int offset = 0;
            for (int i = 0; i < space.Length; i++)
            {
                if (comparer.Equals(space[i], target[i - offset]))
                {
                    if (start == -1)
                        start = i;
                    if (target.Length == i - start + 1)
                    {
                        index = start;
                        length = target.Length;
                        break;
                    }
                }
                else
                {
                    if (start != -1)
                    {
                        if (length < i - start)
                        {
                            index = start;
                            length = i - start;
                        }
                        i -= 1 + pi[i - offset - 1];
                        start = -1;
                        offset = i;
                    }
                    offset++;
                }
            }
            return new Result(index, length);
        }
    }
}
