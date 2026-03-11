using BAOCAOWEBNANGCAO.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
        public async Task<IActionResult> Webhook()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            var json = JsonDocument.Parse(body);

            if (json.RootElement.TryGetProperty("content", out var contentValue))
            {
                string content = contentValue.GetString();

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
            }

            return Ok();
        }

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