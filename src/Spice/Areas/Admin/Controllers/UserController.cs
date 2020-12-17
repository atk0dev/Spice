namespace Spice.Areas.Admin.Controllers
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Spice.Data;
    using Spice.Utility;

    [Area("Admin")]
    [Authorize(Roles = SD.ManagerUser)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;

        public UserController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);
            var users = await _db.ApplicationUser.Where(u => u.Id != claim.Value).ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> Lock(string id)
        {
            return await this.ChangeUserLockoutEndDate(id, DateTime.Now.AddYears(1000));
        }

        public async Task<IActionResult> UnLock(string id)
        {
            return await this.ChangeUserLockoutEndDate(id, DateTime.Now);
        }

        private async Task<IActionResult> ChangeUserLockoutEndDate(string id, DateTime date)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationUser = await _db.ApplicationUser.FirstOrDefaultAsync(m => m.Id == id);

            if (applicationUser == null)
            {
                return NotFound();
            }

            applicationUser.LockoutEnd = date;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}