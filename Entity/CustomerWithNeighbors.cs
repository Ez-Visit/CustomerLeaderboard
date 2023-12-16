namespace CustomerLeaderboard.Entity
{
    public class CustomerWithNeighbors
    {

        public static CustomerWithNeighbors EMPTY = new CustomerWithNeighbors();

        public bool IsEmpty()
        {
            return Equals(EMPTY);
        }

        public Customer? Customer { get; set; }
        public List<Customer>? Neighbors { get; set; }
    }
}
