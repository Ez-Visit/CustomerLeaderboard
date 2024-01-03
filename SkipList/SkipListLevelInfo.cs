namespace CustomerLeaderboard.SkipList
{
    public class SkipListLevelInfo<T> where T : class
    {
        public SkipListNode<T>? Next;
        public uint Span;
    }
}
