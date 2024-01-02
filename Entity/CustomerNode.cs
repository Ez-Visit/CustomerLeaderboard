namespace CustomerLeaderboard.Entity
{
    public class CustomerNode
    {
        /// <summary>
        /// 客户Id
        /// </summary>
        public long CustomerID { get; set; }
        /// <summary>
        /// 客户积分
        /// </summary>
        public decimal Score { get; set; }
        /// <summary>
        /// 积分排名
        /// </summary>
        public int Rank { get; set; }
        /// <summary>
        /// 指向下一个具有更低排名的客户
        /// </summary>
        public CustomerNode Next { get; set; }
        /// <summary>
        /// 下层索引节点
        /// </summary>
        public List<CustomerNode> LowerLevels { get; set; }

    }
}
