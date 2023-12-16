using CustomerLeaderboard.BizService;
using Microsoft.AspNetCore.Mvc;

namespace CustomerLeaderboard.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomerController(CustomerRankingService customerRankingService) : ControllerBase
    {
        private readonly CustomerRankingService customerRankingService = customerRankingService;

        [HttpPost("{customerid}/score/{score}")]
        public async Task<ActionResult<decimal>> UpdateScore(long customerId, decimal score)
        {
            var updatedScore = await customerRankingService.UpdateScore(customerId, score);
            return updatedScore;
        }
    }
}
