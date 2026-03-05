using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BAOCAOWEBNANGCAO.Controllers
{
    public class AccountsController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        // THÊM DÒNG NÀY: Quản lý đăng nhập/đăng xuất
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountsController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager) // THÊM SignInManager VÀO ĐÂY
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager; // GÁN GIÁ TRỊ
        }

        // --- HÀM LOGOUT MỚI THÊM VÀO ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            // Sau khi thoát thì quay về trang chủ khách hàng
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(string email, string password, string phoneNumber)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync("Staff"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Staff"));
                    }

                    await _userManager.AddToRoleAsync(user, "Staff");
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                if (user.Email.ToLower() == "admin@gmail.com" || user.UserName == User.Identity.Name)
                {
                    TempData["Error"] = "Không thể xóa tài khoản Quản trị viên!";
                    return RedirectToAction(nameof(Index));
                }

                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.RoleName = roles.FirstOrDefault() ?? "Chưa gán quyền";

            return View(user);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string phoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Accounts");
            }

            user.PhoneNumber = phoneNumber;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi lưu thay đổi.";
            }

            return RedirectToAction(nameof(MyProfile));
        }
    }
}