namespace CustomerLeaderboard.Entity
{
    /// <summary>
    /// 客户积分排行跳表
    /// </summary>
    public class SkipList
    {
        private readonly int _maxLevels;
        private readonly CustomerNode _head;
        private readonly Random _random;
        private static readonly double SKIPLIST_P = 0.5;

        public SkipList(int maxLevels)
        {
            _maxLevels = maxLevels;
            _head = new CustomerNode { DownLevels = new List<CustomerNode>(maxLevels) };
            _random = new Random();
        }

        public void InsertOrUpdate(long customerId, decimal score)
        {
            var updateNodes = new CustomerNode[_maxLevels];
            var current = _head;
            //从最稀疏的索引层开始查询
            for (int level = _maxLevels - 1; level >= 0; level--)
            {
                while (current.Next != null && current.Next.Score < score)
                {
                    //当前节点的右侧节点的积分小于目标积分，指针继续往右移动
                    current = current.Next;
                }

                updateNodes[level] = current;
                if (level > 0)
                {
                    //只要不是最底层的原始链表层(level>0)就往下一层匹配
                    current = current.DownLevels[level - 1];
                }
            }

            if (updateNodes[0].Next != null && updateNodes[0].Next.Score == score)
            {
                // 分数相同的情况下，通过比较CustomerID来更新节点
                if (updateNodes[0].Next.CustomerID == customerId)
                {
                    updateNodes[0].Next.CustomerID = customerId;
                    return;
                }
            }

            var newNode = new CustomerNode
            {
                CustomerID = customerId,
                Score = score,
                Next = updateNodes[0].Next,
                DownLevels = new List<CustomerNode>(_maxLevels)
            };

            for (int level = 0; level < _maxLevels; level++)
            {
                //
                newNode.DownLevels.Add(level < _maxLevels - 1 ? updateNodes[level].Next : null);
                updateNodes[level].Next = newNode;
                if (level > 0 && ShouldPromote())
                {
                    newNode = Promote(newNode, level);
                }
            }

            UpdateRanks();

            //TODO return the new rank
        }

        private bool ShouldPromote()
        {
            //根据概率决定是否将节点提升到更高的层级
            return _random.NextDouble() < SKIPLIST_P;
        }

        private CustomerNode Promote(CustomerNode node, int level)
        {
            var newNode = new CustomerNode
            {
                CustomerID = node.CustomerID,
                Score = node.Score,
                Next = node,
                DownLevels = new List<CustomerNode>(_maxLevels)
            };

            if (level < _maxLevels - 1)
            {
                newNode.DownLevels.Add(node.DownLevels[level]);
            }

            if (level > 0)
            {
                newNode.DownLevels.Add(Promote(node, level - 1));
            }

            return newNode;
        }

        private void UpdateRanks()
        {
            var current = _head.Next;
            int rank = 1;
            while (current != null)
            {
                current.Rank = rank;
                rank++;
                current = current.Next;
            }
        }

        public List<CustomerNode> GetCustomersByRank(int start, int end)
        {
            var result = new List<CustomerNode>();
            var current = _head.Next;
            while (current != null && current.Rank <= end)
            {
                if (current.Rank >= start)
                {
                    result.Add(current);
                }
                current = current.Next;
            }
            return result;
        }

        public CustomerNode FindNodeByCustomerId(long customerId)
        {
            var current = _head;
            while (current != null)
            {
                if (current.CustomerID == customerId)
                {
                    return current;
                }
                current = current.Next;
            }
            return null;
        }


        private CustomerNode FindNodeByRank(int rank)
        {
            var current = _head;
            for (int level = _maxLevels - 1; level >= 0; level--)
            {
                while (current.Next != null && current.Next.Rank <= rank)
                {
                    current = current.Next;
                }
                if (level > 0)
                {
                    current = current.DownLevels[level - 1];
                }
            }
            return current;
        }

    }
}