namespace CustomerLeaderboard.SkipList
{
    public class SkipListNode<T> where T : class
    {
        public SkipListNode() { }

        public T Item { get; private set; }

        /// <summary>
        /// level为0那一层的前面节点
        /// </summary>
        public SkipListNode<T> Prev;

        /// <summary>
        /// 根据Level记录每一层的下一个节点以及中间间隔的距离
        /// </summary>
        public SkipListLevelInfo<T>[] LevelsInfo { get; private set; }

        public SkipListNode(T obj, uint level)
        {
            Item = obj;
            LevelsInfo = new SkipListLevelInfo<T>[level];
            for (int i = 0; i < level; i++)
            {
                LevelsInfo[i] = new SkipListLevelInfo<T>();
            }
        }
    }
}
