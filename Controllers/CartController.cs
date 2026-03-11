using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BAOCAOWEBNANGCAO.Controllers
{
    public class CartController : Controller
    {
        private readonly CampingDbContext _context;

        private const string CartSessionKey = "Cart";

        public CartController(CampingDbContext context)
        {
            _context = context;
        }

        // =============================
        // LẤY GIỎ HÀNG
        // =============================
        private List<CartItem> GetCart()
        {
            var session = HttpContext.Session.GetString(CartSessionKey);

            if (session != null)
            {
                return JsonConvert.DeserializeObject<List<CartItem>>(session);
            }

            return new List<CartItem>();
        }

        // =============================
        // LƯU GIỎ HÀNG
        // =============================
        private void SaveCart(List<CartItem> cart)
        {
            var json = JsonConvert.SerializeObject(cart);
            HttpContext.Session.SetString(CartSessionKey, json);
        }

        // =============================
        // XEM GIỎ HÀNG
        // =============================
        public IActionResult Index()
        {
            var cart = GetCart();

            ViewBag.TotalAmount = cart.Sum(x => x.TotalPrice);

            return View(cart);
        }

        // =============================
        // THÊM VÀO GIỎ
        // =============================
        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            var cart = GetCart();

            var product = _context.Products.FirstOrDefault(p => p.Id == productId);

            if (product == null)
                return NotFound();

            var cartItem = cart.FirstOrDefault(x => x.Product.Id == productId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    Product = new Product
                    {
                        Id = product.Id,
                        Name = product.Name,
                        PricePerDay = product.PricePerDay,
                        ImageUrl = product.ImageUrl
                    },
                    Quantity = quantity
                });
            }

            SaveCart(cart);

            return RedirectToAction("Index");
        }

        // =============================
        // XÓA SẢN PHẨM
        // =============================
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(x => x.Product.Id == productId);

            if (item != null)
            {
                cart.Remove(item);
            }

            SaveCart(cart);

            return RedirectToAction("Index");
        }

        // =============================
        // TRANG CHECKOUT
        // =============================
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = GetCart();

            if (cart.Count == 0)
            {
                return RedirectToAction("Index");
            }

            return View(cart);
        }

        // =============================
        // XỬ LÝ ĐẶT HÀNG
        // =============================
        [HttpPost]
        public async Task<IActionResult> Checkout(
            string customerName,
            string customerPhone,
            string customerEmail,
            string shippingAddress,
            string note,
            DateTime rentalStart,
            DateTime rentalEnd)
        {
            var cart = GetCart();

            if (cart.Count == 0)
                return RedirectToAction("Index");

            // TÍNH SỐ NGÀY THUÊ
            int rentalDays = (rentalEnd - rentalStart).Days;

            if (rentalDays <= 0)
                rentalDays = 1;

            // TÍNH TIỀN
            decimal cartTotal = cart.Sum(x => x.TotalPrice);

            decimal finalTotal = cartTotal * rentalDays;

            // TẠO ORDER
            var order = new Order
            {
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                CustomerEmail = customerEmail,
                ShippingAddress = shippingAddress,
                Note = note,
                RentalStartDate = DateTime.SpecifyKind(rentalStart, DateTimeKind.Utc),
                RentalEndDate = DateTime.SpecifyKind(rentalEnd, DateTimeKind.Utc),
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                TotalAmount = finalTotal
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // LƯU ORDER DETAILS
            foreach (var item in cart)
            {
                var detail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.Product.Id,
                    Quantity = item.Quantity,
                    PricePerUnit = item.Product.PricePerDay
                };

                _context.OrderDetails.Add(detail);
            }

            await _context.SaveChangesAsync();

            // XÓA GIỎ HÀNG
            HttpContext.Session.Remove(CartSessionKey);

            // CHUYỂN ĐẾN QR PAYMENT
            return RedirectToAction("Payment", new { id = order.Id });
        }

        // =============================
        // TRANG THANH TOÁN QR
        // =============================
        public async Task<IActionResult> Payment(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // =============================
        // TRANG THÀNH CÔNG
        // =============================
        public IActionResult OrderSuccess()
        {
            return View();
        }
    }
}