using CustomerLeaderboard.BizService;
using CustomerLeaderboard.Entity;
using Microsoft.AspNetCore.Mvc;

namespace CustomerLeaderboard.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LeaderboardController : ControllerBase
    {
        private readonly CustomerRankingService customerRankingService;
        public LeaderboardController(CustomerRankingService customerRankingService)
        {
            this.customerRankingService = customerRankingService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Customer>>> GetCustomersByRank(int start, int end)
        {
            var leaderboard = await customerRankingService.GetCustomersByRank(start, end);
            return leaderboard;
        }

        [HttpGet("{customerid}")]
        public async Task<ActionResult<CustomerWithNeighbors>> GetCustomersByCustomerId(long customerid, int high = 0, int low = 0)
        {
            var result = await customerRankingService.GetCustomersByCustomerId(customerid, high, low);
            return result;
        }
    }
}
