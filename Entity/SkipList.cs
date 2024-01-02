namespace CustomerLeaderboard.Entity
{
    /// <summary>
    /// 客户积分排行跳表
    /// </summary>
    public class SkipList
    {
        public readonly int MaxLevels;
        private readonly CustomerNode _head;
        private readonly Random _random;
        /// <summary>
        /// 表示最底层的原始链表层的索引
        /// </summary>
        private const int ORIGINAL_LEVEL = 0;
        /// <summary>
        /// 控制节点升级的概率
        /// </summary>
        private static readonly double PROMOTE_PROBABILITY = 0.5;

        public SkipList(int maxLevels)
        {
            MaxLevels = maxLevels;
            _head = new CustomerNode { LowerLevels = new List<CustomerNode>() };
            _random = new Random();
        }

        public bool Search(decimal score, out CustomerNode? position)
        {
            position = null;
            var current = _head;
            //从最稀疏的索引层开始查询
            for (int level = MaxLevels - 1; level >= 0; level--)
            {
                //当前节点的右侧节点的积分小于目标积分，指针继续往右移动
                while (current.Next != null && current.Next.Score < score)
                {
                    current = current.Next;
                }

                if (level > ORIGINAL_LEVEL && current.LowerLevels.Count > level - 1)
                {
                    //只要不是最底层的原始链表层(level>0)就往下一层匹配
                    current = current.LowerLevels[level - 1];
                }

                if (level == ORIGINAL_LEVEL)
                {
                    position = current;
                }

            }
            return position != null;
        }

        /// <summary>
        /// 根据概率决定是否将节点提升到更高的层级
        /// </summary>
        /// <returns></returns>
        public bool ShouldPromote()
        {
            return _random.NextDouble() < PROMOTE_PROBABILITY;
        }

        public CustomerNode Promote(CustomerNode node, int level)
        {
            var newNode = new CustomerNode
            {
                CustomerID = node.CustomerID,
                Score = node.Score,
                Next = node,
                LowerLevels = new List<CustomerNode>(MaxLevels)
            };

            if (level < MaxLevels - 1)
            {
                newNode.LowerLevels.Add(node.LowerLevels[level]);
            }

            if (level > 0)
            {
                newNode.LowerLevels.Add(Promote(node, level - 1));
            }

            return newNode;
        }

        public void InsertAndUpdateRanks(CustomerNode newNode)
        {
            var current = newNode;
            int rank = current.Rank;
            while (current.Next != null)
            {
                current.Next.Rank = ++rank;
                current = current.Next;
            }
        }

        public void UpdateRank(Customer customer)
        {
            var prevRank = customer.Rank;
            var newRank = 1;
            var prevCustomer = customer;

            while (prevCustomer.Level > 0)
            {
                prevCustomer = prevCustomer.NextCustomers[prevCustomer.Level - 1];

                while (prevCustomer != null && prevCustomer.Score > customer.Score)
                {
                    newRank += prevCustomer.Rank;
                    prevCustomer = prevCustomer.NextCustomers[prevCustomer.Level - 1];
                }

                if (prevCustomer != null && prevCustomer.Score == customer.Score && prevCustomer.CustomerID < customer.CustomerID)
                {
                    newRank += prevCustomer.Rank;
                    prevCustomer = prevCustomer.NextCustomers[prevCustomer.Level - 1];
                }
            }

            if (prevRank != newRank)
            {
                customer.Rank = newRank;
                UpdateNextCustomers(customer, prevRank, newRank);
            }
        }

        private void UpdateNextCustomers(Customer customer, int prevRank, int newRank)
        {
            if (newRank > prevRank)
            {
                if (newRank > customer.Level)
                {
                    var newLevel = customer.Level + 1;
                    var newNextCustomers = new Customer[newLevel];
                    Array.Copy(customer.NextCustomers, newNextCustomers, customer.Level);
                    newNextCustomers[newLevel - 1] = null;
                    customer.NextCustomers = newNextCustomers;
                    customer.Level = newLevel;
                }

                for (int i = prevRank; i < newRank; i++)
                {
                    customer.NextCustomers[i] = customer.NextCustomers[i - 1];
                    customer.NextCustomers[i - 1] = customer;
                }
            }
            else if (newRank < prevRank)
            {
                for (int i = customer.Level - 1; i >= newRank; i--)
                {
                    customer.NextCustomers[i] = null;
                }

                customer.Level = newRank;
            }
        }

        public void UpdateRanks(CustomerNode newNode)
        {
            var current = _head.Next;
            int rank = 1;
            while (current != null)
            {
                if ((current.Score < newNode.Score
                    && current.CustomerID != newNode.CustomerID)
                    || (current.Score == newNode.Score
                    && current.CustomerID < newNode.CustomerID))
                {
                    rank++;
                }
                current.Rank = rank;
                rank++;
                current = current.Next;
            }
        }

        public void InsertIntoSkipList(CustomerNode newNode, CustomerNode position, int level)
        {
            for (int i = 0; i <= level; i++)
            {
                newNode.LowerLevels.Add(position.LowerLevels[i]);
                position.LowerLevels[i] = newNode;
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


        public CustomerNode FindNodeByRank(int rank)
        {
            var current = _head;
            for (int level = MaxLevels - 1; level >= 0; level--)
            {
                while (current.Next != null && current.Next.Rank <= rank)
                {
                    current = current.Next;
                }
                if (level > 0)
                {
                    current = current.LowerLevels[level - 1];
                }
            }
            return current;
        }

    }
}
