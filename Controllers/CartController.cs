using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json; // Thư viện dùng để chuyển đổi dữ liệu sang chuỗi JSON

namespace BAOCAOWEBNANGCAO.Controllers
{
    public class CartController : Controller
    {
        // 1. Khai báo Database để lấy thông tin sản phẩm
        private readonly CampingDbContext _context;

        // Tên chìa khóa để lưu giỏ hàng trong Session (đặt tên gì cũng được)
        private const string CartSessionKey = "GioHangCuaToi";

        public CartController(CampingDbContext context)
        {
            _context = context;
        }

        // 2. Action: Xem giỏ hàng
        public IActionResult Index()
        {
            // Lấy danh sách hàng từ Session ra để hiển thị
            var cart = GetCartFromSession();

            // Tính tổng tiền luôn để hiển thị (tùy chọn)
            ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);

            return View(cart);
        }

        // 3. Action: Thêm sản phẩm vào giỏ
        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity)
        {
            // Bước A: Lấy giỏ hàng cũ từ Session ra
            var cart = GetCartFromSession();

            // Bước B: Tìm xem sản phẩm này đã có trong giỏ chưa?
            var cartItem = cart.FirstOrDefault(c => c.Product.Id == productId);

            if (cartItem != null)
            {
                // TRƯỜNG HỢP 1: Đã có -> Chỉ cần cộng dồn số lượng
                cartItem.Quantity += quantity;
            }
            else
            {
                // TRƯỜNG HỢP 2: Chưa có -> Tìm thông tin từ Database và thêm mới
                var product = _context.Products.Find(productId);
                if (product != null)
                {
                    cart.Add(new CartItem
                    {
                        Product = product,
                        Quantity = quantity
                    });
                }
            }

            // Bước C: Lưu ngược lại danh sách mới vào Session
            SaveCartToSession(cart);

            // Chuyển hướng về trang xem giỏ hàng

            int totalItems = GetTotalItemsInCart();

            // Lưu con số này vào Session để Layout có thể đọc được
            HttpContext.Session.SetInt32("CartCount", totalItems);

            return RedirectToAction("Index"); // Chuyển hướng đến trang Giỏ hàng
        }
        private int GetTotalItemsInCart()
        {
            // Đổi GetObjectFromJson thành tên hàm trong Helper nếu cần 
            // Ở đây mình đã đặt tên là GetObjectFromJson trong Helper rồi nên sẽ chạy được
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            return cart.Sum(item => item.Quantity);
        }
        // 4. Action: Xóa 1 sản phẩm khỏi giỏ
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCartFromSession();


            // Tìm sản phẩm cần xóa
            var itemToRemove = cart.FirstOrDefault(c => c.Product.Id == productId);

            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove); // Xóa khỏi danh sách
                SaveCartToSession(cart);   // Lưu lại Session mới
            }

            return RedirectToAction(nameof(Index));
        }
        // --- CÁC HÀM PHỤ TRỢ (HELPER) --- 
        // Mục đích: Giúp code gọn gàng, không phải viết đi viết lại đoạn đọc/ghi Session

        // Hàm đọc: Lấy chuỗi JSON từ Session -> Chuyển thành List<CartItem>
        private List<CartItem> GetCartFromSession()
        {
            var session = HttpContext.Session;
            string json = session.GetString(CartSessionKey);

            if (!string.IsNullOrEmpty(json))
            {
                return JsonConvert.DeserializeObject<List<CartItem>>(json);
            }

            return new List<CartItem>(); // Nếu chưa có gì thì trả về danh sách rỗng
        }

        // Hàm ghi: Chuyển List<CartItem> thành chuỗi JSON -> Lưu vào Session
        private void SaveCartToSession(List<CartItem> cart)
        {
            var session = HttpContext.Session;
            string json = JsonConvert.SerializeObject(cart);
            session.SetString(CartSessionKey, json);
        }
        // 5. GET: Hiển thị trang thanh toán
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = GetCartFromSession();

            // Nếu giỏ hàng rỗng thì không cho vào trang thanh toán
            if (cart.Count == 0)
            {
                return RedirectToAction("Index");
            }

            ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);
            return View(cart); // Truyền danh sách giỏ hàng sang để hiển thị bên cột phải
        }

        // POST: Xử lý đặt hàng
        [HttpPost]
        public async Task<IActionResult> Checkout(string customerName, string customerPhone, string customerEmail, string shippingAddress, string? note, DateTime rentalStart, DateTime rentalEnd)
        {
            var cart = GetCartFromSession();
            if (cart.Count == 0) return RedirectToAction("Index");

            // 1. Tính số ngày thuê
            TimeSpan duration = rentalEnd - rentalStart;
            int rentalDays = duration.Days;
            if (rentalDays <= 0) rentalDays = 1;

            // 2. Tính tổng tiền
            decimal cartTotal = cart.Sum(item => item.TotalPrice);
            decimal finalTotal = cartTotal * rentalDays;

            // 3. Tạo Order (Không còn CitizenId)
            var order = new Order
            {
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                CustomerEmail = customerEmail,
                ShippingAddress = shippingAddress, // Lưu địa chỉ
                Note = note,                       // Lưu ghi chú
                RentalStartDate = rentalStart,
                RentalEndDate = rentalEnd,
                OrderDate = DateTime.Now,
                Status = "Pending",
                TotalAmount = finalTotal
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // 4. Lưu chi tiết đơn hàng
            foreach (var item in cart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.Product.Id,
                    Quantity = item.Quantity,
                    PricePerUnit = item.Product.PricePerDay
                };
                _context.OrderDetails.Add(orderDetail);
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("GioHangCuaToi");
            return RedirectToAction("OrderSuccess");
        }
        // 7. Trang thông báo mua thành công
        public IActionResult OrderSuccess()
        {
            return View();
        }
    }
}