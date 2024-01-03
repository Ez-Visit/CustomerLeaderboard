using CustomerLeaderboard.BizService;
using CustomerLeaderboard.Entity;
using Microsoft.AspNetCore.Mvc;

namespace CustomerLeaderboard.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LeaderboardController : ControllerBase
    {
        private readonly CustomerRankingManager rankingManager = CustomerRankingManager.GetInstance();
        public LeaderboardController()
        {
        }

        [HttpGet]
        public async Task<ActionResult<List<Customer>>> GetCustomersByRank(int start, int end)
        {
            var leaderboard = await rankingManager.GetCustomersByRank(start, end);
            return leaderboard;
        }

        [HttpGet("{customerid}")]
        public async Task<ActionResult<List<Customer>>> GetCustomersByCustomerId(long customerid, int high = 0, int low = 0)
        {
            var result = await rankingManager.GetCustomersByCustomerId(customerid, high, low);
            return result;
        }
    }
}
