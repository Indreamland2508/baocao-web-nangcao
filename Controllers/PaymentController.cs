using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
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
        // WEBHOOK SEPAY
        // =============================
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] WebhookModel data)
        {
            if (data == null)
                return BadRequest();

            var order = await _context.Orders
                .FirstOrDefaultAsync(x => x.Id == data.orderId);

            if (order == null)
                return NotFound();

            // Kiểm tra trạng thái thanh toán
            if (data.status == "success")
            {
                order.Status = "Paid";
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        // =============================
        // API KIỂM TRA TRẠNG THÁI ORDER
        // =============================
        [HttpGet("check/{orderId}")]
        public async Task<IActionResult> Check(int orderId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order == null)
                return NotFound();

            return Json(new
            {
                status = order.Status
            });
        }
    }

    // =============================
    // MODEL NHẬN WEBHOOK
    // =============================
    public class WebhookModel
    {
        public int orderId { get; set; }

        public decimal amount { get; set; }

        public string status { get; set; }
    }
}