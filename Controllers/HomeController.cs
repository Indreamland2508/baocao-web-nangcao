using BAOCAOWEBNANGCAO.Data; // Thêm dòng này
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Thêm dòng này

namespace BAOCAOWEBNANGCAO.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CampingDbContext _context; // Khai báo DbContext

        // Inject DbContext vào Constructor
        public HomeController(ILogger<HomeController> logger, CampingDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy danh sách sản phẩm kèm theo tên Loại (Category)
            var products = await _context.Products.Include(p => p.Category).ToListAsync();
            return View(products); // Truyền sang View
        }
        // GET: Home/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }
        [AllowAnonymous]
        public async Task<IActionResult> CheckMyRole([FromServices] UserManager<IdentityUser> userManager)
        {
            if (!User.Identity.IsAuthenticated)
                return Content("BẠN CHƯA ĐĂNG NHẬP!");

            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return Content("LỖI: Đã đăng nhập nhưng Cookie bị cũ hoặc sai lệch với Database. HÃY ĐĂNG XUẤT VÀ ĐĂNG NHẬP LẠI!");

            var roles = await userManager.GetRolesAsync(user);

            string roleString = roles.Any() ? string.Join(", ", roles) : "KHÔNG CÓ BẤT KỲ QUYỀN NÀO!";

            return Content($"Tài khoản hiện tại: {user.Email}\nQuyền (Role) hệ thống đang nhận diện: {roleString}");
        }
        // Action để hiện trang danh sách lều trại riêng
        public async Task<IActionResult> ProductList(string search, int? categoryId)
        {
            // Lấy danh sách sản phẩm và kèm theo Category
            var productsQuery = _context.Products.Include(p => p.Category).AsQueryable();

            // Lọc theo từ khóa tìm kiếm nếu có
            if (!string.IsNullOrEmpty(search))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(search));
            }

            // Lọc theo danh mục nếu khách bấm vào
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId);
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(await productsQuery.ToListAsync());
        }
    }
}