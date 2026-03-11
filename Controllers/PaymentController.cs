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

        // Nhận webhook từ SePay
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] dynamic data)
        {
            try
            {
                string content = data.content;

                if (content.StartsWith("ORDER"))
                {
                    var idText = content.Replace("ORDER", "");

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
            catch
            {
                return Ok();
            }
        }

        // API để frontend kiểm tra trạng thái
        [HttpGet("check/{orderId}")]
        public async Task<IActionResult> Check(int orderId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            return Ok(new { status = order.Status });
        }
    }
}