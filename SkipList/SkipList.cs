﻿namespace CustomerLeaderboard.SkipList
{
    /// <summary>
    /// 客户积分排行跳表 仿照redis的实现
    /// </summary>
    public class SkipList<T> where T : class
    {
        private readonly Random random;

        /// <summary>
        /// 表示最底层的原始链表层的索引
        /// </summary>
        private const int ORIGINAL_LEVEL = 0;

        /// <summary>
        /// 控制节点升级的概率
        /// </summary>
        private static readonly double PROMOTE_PROBABILITY = 0.5;

        private SkipListNode<T> header;
        private SkipListNode<T>? tail;
        private uint currentLevel;
        private uint maxLevel;
        private Comparison<T> comparison;
        public uint Count { get; private set; }

        /// <summary>
        /// 仿照redis的实现的跳表
        /// </summary>
        /// <param name="comparison">从小到大排，返回1，表示要交换，-1，表示不要交换，0表示是同一个元素, 注意不同元素不能返回0</param>
        /// <param name="maxLevel"></param>
        public SkipList(Comparison<T> comparison, uint maxLevel = 32)
        {
            this.comparison = comparison;
            random = new Random();
            this.maxLevel = maxLevel;
            currentLevel = 1;
            Count = 0;
            header = new SkipListNode<T>(null, maxLevel);
            for (int i = 0; i < maxLevel; i++)
            {
                header.LevelsInfo[i].Next = null;
                header.LevelsInfo[i].Span = 0;
            }
            header.Prev = null;
            tail = null;
        }

        public void Add(T item)
        {
            //创建一个更新数组，用来记录每一层的前驱节点
            //TODO 考虑优化这个内存分配耗时
            SkipListNode<T>[] needUpdate = new SkipListNode<T>[maxLevel];
            uint[] rank = new uint[maxLevel];

            // 从头节点开始，从上到下，从右到左，查找插入位置的前驱节点
            SkipListNode<T> currentNode = header;
            //Level等级从高到低,计算levelMoveSpan和update,currentNode会依次往后遍历
            for (int i = (int)currentLevel - 1; i >= ORIGINAL_LEVEL; i--)
            {
                //初始化rank[i]为上一层的rank值，rank[i]记录在第curentLevel - 1 到 i层上移动的span值总和
                rank[i] = ((i + 1 == currentLevel) ? 0 : rank[i + 1]);
                //然后从当前的SkipListNode往后找，在第i层的Rank累加Span, 直到找到第一个比item大的才结束
                //把rank的所有层span值累加就是item的排名
                while (currentNode.LevelsInfo[i].Next != null
                    && comparison(item, currentNode.LevelsInfo[i].Next.Item) > 0)
                {
                    rank[i] += currentNode.LevelsInfo[i].Span;
                    currentNode = currentNode.LevelsInfo[i].Next;
                }
                //记录在第i层结束的节点，恰好比item小,后续插入节点时需要更新needUpdate里的span等
                needUpdate[i] = currentNode;
            }

            //随机一个插入节点的层高
            uint newNodeLevel = RandomLevel();
            //如果新的层数大于当前的层数，更新更新数组和当前的层数
            if (newNodeLevel > currentLevel)
            {
                for (int i = (int)currentLevel; i < newNodeLevel; i++)
                {
                    //在大于等于currentLevel层上移动的span为0
                    rank[i] = 0;
                    //Header后续没有Node,表示跨了Length
                    header.LevelsInfo[i].Next = null;
                    header.LevelsInfo[i].Span = (uint)Count;

                    //大于等于currentLevel层上结束的节点为Header
                    needUpdate[i] = header;
                }
                currentLevel = newNodeLevel;
            }

            //创建一个新的节点
            SkipListNode<T> newNode = new SkipListNode<T>(item, newNodeLevel);
            //从上到下，插入新的节点到每一层的链表中
            for (int i = 0; i < newNodeLevel; i++)
            {
                //在needUpdate[i] 和needUpdate[i].Next中间插入newNode
                newNode.LevelsInfo[i].Next = needUpdate[i].LevelsInfo[i].Next;
                needUpdate[i].LevelsInfo[i].Next = newNode;

                //设置newNode每层上跨越的span数
                newNode.LevelsInfo[i].Span = needUpdate[i].LevelsInfo[i].Span - (rank[0] - rank[i]);
                //修改needUpdate各层上的span,用第0层的rank减去第i层的rank可以计算得出
                needUpdate[i].LevelsInfo[i].Span = (rank[0] - rank[i]) + 1;
            }

            //大于newNode层高的needUpdate的Span需要加1
            for (int i = (int)newNodeLevel; i < currentLevel; i++)
            {
                needUpdate[i].LevelsInfo[i].Span++;
            }

            //记录newNode在第0层上的前一个Node
            newNode.Prev = (needUpdate[0] == header) ? null : needUpdate[0];
            //如果newNode在第0层上的下个节点非null,就修改下个节点的前置节点为newNode，
            //否则说明newNode是最后一个节点，那么就更新Tail为newNode
            if (newNode.LevelsInfo[0].Next != null)
            {
                newNode.LevelsInfo[0].Next.Prev = newNode;
            }
            else
            {
                this.tail = newNode;
            }
            Count++;
        }

        public bool Remove(T item)
        {
            // 创建一个更新数组，用来记录每一层的前驱节点
            SkipListNode<T>[] needUpdate = new SkipListNode<T>[maxLevel];
            // 从头节点开始，从上到下，从右到左，查找删除位置的前驱节点
            SkipListNode<T> currentNode = header;
            // 遍历所有层，记录删除节点后需要被修改的节点到 update 数组  
            for (int i = (int)currentLevel - 1; i >= 0; i--)
            {
                while (currentNode.LevelsInfo[i].Next != null
                    && comparison(item, currentNode.LevelsInfo[i].Next.Item) > 0)
                    currentNode = currentNode.LevelsInfo[i].Next;
                needUpdate[i] = currentNode;
            }

            // 获取要删除的节点
            SkipListNode<T> removeNode = currentNode.LevelsInfo[0].Next;
            if (removeNode != null && removeNode.Item.Equals(item))
            {
                DeleteNode(removeNode, needUpdate);
                return true;
            }
            return false;

        }

        private void DeleteNode(SkipListNode<T> removeNode, SkipListNode<T>[] update)
        {
            // 从上到下，从每一层的链表中删除节点
            for (int i = ORIGINAL_LEVEL; i < currentLevel; i++)
            {
                //update的下个节点是要移除的node的话，需要修改下Next，并合并span的值
                if (update[i].LevelsInfo[i].Next == removeNode)
                {
                    //update[i]->level[i]的后继等于要删除节点x  
                    update[i].LevelsInfo[i].Span += removeNode.LevelsInfo[i].Span - 1;
                    update[i].LevelsInfo[i].Next = removeNode.LevelsInfo[i].Next;
                }
                else
                {
                    update[i].LevelsInfo[i].Span -= 1;
                }
            }

            //更新第0层的前驱节点
            if (removeNode.LevelsInfo[ORIGINAL_LEVEL].Next != null)
            {
                removeNode.LevelsInfo[ORIGINAL_LEVEL].Next.Prev = removeNode.Prev;
            }
            else
            {
                //删除的是最后一个节点，需要更新tail
                tail = removeNode.Prev;
            }
            //收缩level，最少要有1层
            while (currentLevel > 1 && header.LevelsInfo[currentLevel - 1].Next == null)
            {
                currentLevel--;
            }

            Count--;
        }

        public uint GetIndex(T item)
        {
            SkipListNode<T> currentNode = header;
            uint rank = 0;
            for (int i = (int)currentLevel - 1; i >= ORIGINAL_LEVEL; i--)
            {
                while (currentNode.LevelsInfo[i].Next != null
                    && comparison(item, currentNode.LevelsInfo[i].Next.Item) >= 0)
                {
                    rank += currentNode.LevelsInfo[i].Span;//排名，加上该层跨越的节点数目  
                    currentNode = currentNode.LevelsInfo[i].Next;
                }

                if (currentNode.Item != null && currentNode.Item.Equals(item))
                {
                    return rank;
                }
            }
            return 0;
        }

        /// <summary>
        /// 根据索引获取节点
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public SkipListNode<T>? GetNodeByIndex(uint index)
        {
            //记录已经遍历过的节点的个数
            uint traversed = 0;

            SkipListNode<T> currentNode = header;
            for (int i = (int)currentLevel - 1; i >= ORIGINAL_LEVEL; i--)
            {
                while (currentNode.LevelsInfo[i].Next != null 
                    && (traversed + currentNode.LevelsInfo[i].Span) <= index)
                {
                    traversed += currentNode.LevelsInfo[i].Span;//排名  
                    currentNode = currentNode.LevelsInfo[i].Next;
                }
                if (traversed == index)
                {
                    return currentNode;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据索引获取元素值
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T? GetItem(uint index)
        {
            var node = GetNodeByIndex(index);
            return node?.Item;
        }

        /// <summary>
        /// 跳表的根据排名获取节点的私有方法
        /// </summary>
        /// <param name="rank"></param>
        /// <param name="startNode"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private SkipListNode<T> GetNodeByRank(int rank, SkipListNode<T> startNode)
        {
            //记录已经遍历过的节点的个数
            uint traversed = 0;

            // 判断排名是否合法
            if (rank < 1 || rank > Count)
            {
                //throw new ArgumentOutOfRangeException(nameof(rank));
            }
            // 从指定的节点开始，从上到下，从左到右，查找目标节点
            SkipListNode<T> current = startNode;           

            for (int i = (int)(currentLevel - 1); i >= ORIGINAL_LEVEL; i--)
            {
                while (current.LevelsInfo[i].Next != null
                    && traversed + current.LevelsInfo[i].Span <= rank)
                {
                    // 更新当前节点和当前排名
                    traversed += current.LevelsInfo[i].Span;
                    current = current.LevelsInfo[i].Next;
                }
                // 判断是否找到目标节点
                if (traversed == rank)
                {
                    // 找到则返回目标节点
                    return current;
                }
            }
            // 没有找到则返回空
            return null;
        }

        // 跳表的根据排名获取节点的公共方法
        public SkipListNode<T> GetNodeByRank(int rank)
        {
            // 调用私有的方法 GetNodeByRank，从头节点开始查找
            return GetNodeByRank(rank, header);
        }

        public List<T> GetItems()
        {
            List<T> rankList = new List<T>();
            SkipListNode<T> currentNode = header;
            while (currentNode.LevelsInfo[ORIGINAL_LEVEL].Next != null)
            {
                currentNode = currentNode.LevelsInfo[ORIGINAL_LEVEL].Next;
                rankList.Add(currentNode.Item);
            }
            return rankList;
        }

        public void RemoveRange(uint start, uint end, out List<T> deleteList)
        {
            deleteList = new List<T>((int)(end - start + 1));
            SkipListNode<T>[] needUpdate = new SkipListNode<T>[maxLevel];
            uint rank = 0;

            //找到start排名的update节点
            SkipListNode<T> curretNode = this.header;
            for (int i = (int)currentLevel - 1; i >= ORIGINAL_LEVEL; i--)
            {
                while (curretNode.LevelsInfo[i].Next != null && (rank + curretNode.LevelsInfo[i].Span) < start)
                {
                    rank += curretNode.LevelsInfo[i].Span;//排名  
                    curretNode = curretNode.LevelsInfo[i].Next;
                }
                needUpdate[i] = curretNode;
            }

            //到start节点
            rank++; //rank可能会小于start,也可能大于end(end为0）
            curretNode = curretNode.LevelsInfo[ORIGINAL_LEVEL].Next;

            //删除start到end之间的节点
            while (curretNode != null && rank <= end)
            {
                SkipListNode<T> next = curretNode.LevelsInfo[ORIGINAL_LEVEL].Next;
                DeleteNode(curretNode, needUpdate);//删除节点 
                deleteList.Add(curretNode.Item);
                rank++;
                curretNode = next;
            }
        }

        /// <summary>
        /// 为新的skiplist节点生成该节点level数目
        /// 根据概率决定是否将节点提升到更高的层级
        /// </summary>
        /// <returns></returns>
        private uint RandomLevel()
        {
            uint level = 1;
            while (random.NextDouble() < PROMOTE_PROBABILITY)
            {
                level += 1;
            }
            return (level < maxLevel) ? level : maxLevel;
        }

    }
}
