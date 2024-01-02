namespace CustomerLeaderboard.Entity
{
    public class Customer
    {
        public long CustomerID { get; set; }
        public decimal Score { get; set; }
        public int Rank { get; set; }
        public Customer[]? NextCustomers { get; set; }

        public int Level { get; set; } = 0;
    }
}