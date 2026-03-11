using BAOCAOWEBNANGCAO.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BAOCAOWEBNANGCAO.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : Controller
    {
        private readonly CampingDbContext _context;

        public PaymentController(CampingDbContext context)
        {
            _context = context;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] WebhookModel data)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(x => x.Id == data.orderId);

            if (order == null)
                return NotFound();

            order.Status = "Paid";

            await _context.SaveChangesAsync();

            return Ok();
        }
    }

    public class WebhookModel
    {
        public int orderId { get; set; }
        public decimal amount { get; set; }
        public string status { get; set; }
    }
}