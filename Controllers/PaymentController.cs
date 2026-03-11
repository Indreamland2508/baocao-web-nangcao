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

        // =============================
        // WEBHOOK NHẬN THANH TOÁN
        // =============================
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] SepayWebhook data)
        {
            if (data == null)
                return BadRequest();

            if (string.IsNullOrEmpty(data.content))
                return Ok();

            // ví dụ content = ORDER15
            if (data.content.StartsWith("ORDER"))
            {
                var idText = data.content.Replace("ORDER", "");

                if (int.TryParse(idText, out int orderId))
                {
                    var order = await _context.Orders
                        .FirstOrDefaultAsync(x => x.Id == orderId);

                    if (order != null)
                    {
                        order.Status = "Paid";
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return Ok();
        }

        // =============================
        // API CHECK TRẠNG THÁI ORDER
        // =============================
        [HttpGet("check/{orderId}")]
        public async Task<IActionResult> Check(int orderId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order == null)
                return NotFound();

            return Json(new { status = order.Status });
        }
    }

    public class SepayWebhook
    {
        public string content { get; set; }

        public decimal amount { get; set; }
    }
}