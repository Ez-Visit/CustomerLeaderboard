using CustomerLeaderboard.BizService;
using CustomerLeaderboard.Entity;
using Microsoft.AspNetCore.Mvc;

namespace CustomerLeaderboard.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LeaderboardController : ControllerBase
    {
        //private readonly CustomerRankingService customerRankingService;
        private readonly CustomerRankingBySkipListService skipListService;
        public LeaderboardController(//CustomerRankingService customerRankingService,
                                     CustomerRankingBySkipListService skipListService)
        {
            //this.customerRankingService = customerRankingService;
            this.skipListService = skipListService;
        }

        [HttpGet]
        public async Task<ActionResult<List<CustomerNode>>> GetCustomersByRank(int start, int end)
        {
            var leaderboard = await skipListService.GetCustomersByRank(start, end);
            return leaderboard;
        }

        [HttpGet("{customerid}")]
        public async Task<ActionResult<CustomerWithNeighbors>> GetCustomersByCustomerId(long customerid, int high = 0, int low = 0)
        {
            var result = await skipListService.GetCustomersByCustomerId(customerid, high, low);
            return result;
        }
    }
}
