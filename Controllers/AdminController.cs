using BAOCAOWEBNANGCAO.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BAOCAOWEBNANGCAO.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class AdminController : Controller
    {
        private readonly CampingDbContext _context;

        public AdminController(CampingDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // ======================
            // 1. THỐNG KÊ DASHBOARD
            // ======================

            // Tổng doanh thu
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed" || o.Status == "Approved")
                .Select(o => (decimal?)o.TotalAmount)
                .SumAsync() ?? 0;

            // Đơn chờ duyệt
            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == "Pending");

            // Tổng sản phẩm
            var totalProducts = await _context.Products.CountAsync();

            // ======================
            // 2. ĐƠN HÀNG HÔM NAY
            // ======================

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var todayOrders = await _context.Orders
                .CountAsync(o => o.OrderDate >= today && o.OrderDate < tomorrow);

            // ======================
            // 3. BIỂU ĐỒ 7 NGÀY
            // ======================

            var startDate = DateTime.UtcNow.Date.AddDays(-6);

            var revenueData = await _context.Orders
                .Where(o =>
                    o.OrderDate >= startDate &&
                    (o.Status == "Completed" || o.Status == "Approved"))
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            var labels = new List<string>();
            var data = new List<decimal>();

            for (int i = 0; i < 7; i++)
            {
                var date = startDate.AddDays(i);

                var revenue = revenueData
                    .FirstOrDefault(x => x.Date == date)?.Revenue ?? 0;

                labels.Add(date.ToString("dd/MM"));
                data.Add(revenue);
            }

            // ======================
            // 4. GỬI DỮ LIỆU SANG VIEW
            // ======================

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TodayOrders = todayOrders;

            ViewBag.ChartLabels = labels;
            ViewBag.ChartData = data;

            return View();
        }
    }
}