using System.Collections.ObjectModel;

namespace CustomerLeaderboard.Entity
{
    public class CustomersWithRank : KeyedCollection<long, Customer>
    {
        protected override long GetKeyForItem(Customer item)
        {
            return item.CustomerID;
        }
    }
}
