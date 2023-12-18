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
            var validationErrors = ValidateInput(customerId, score);
            if (validationErrors.Count != 0)
            {
                foreach (var error in validationErrors)
                {
                    ModelState.AddModelError(error.Key, error.Value);
                }
                return BadRequest(ModelState);
            }

            var updatedScore = await customerRankingService.UpdateScore(customerId, score);
            return updatedScore;
        }

        private static Dictionary<string, string> ValidateInput(long customerId, decimal score)
        {
            var errors = new Dictionary<string, string>();

            if (customerId <= 0)
            {
                errors.Add("customerId", "CustomerId must be a positive number.");
            }
            if (score < -1000 || score > 1000)
            {
                errors.Add("score", "Score must be in the range [-1000, 1000].");
            }

            return errors;
        }
    }
}
