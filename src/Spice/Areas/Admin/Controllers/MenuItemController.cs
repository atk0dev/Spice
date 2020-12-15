using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.ManagerUser)]
    public class MenuItemController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostingEnvironment;

        [BindProperty]
        public MenuItemViewModel MenuItemViewModel { get; set; }
  
        public MenuItemController(ApplicationDbContext db, IWebHostEnvironment hostingEnvironment)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;

            this.MenuItemViewModel = new MenuItemViewModel()
            {
                Category = _db.Category,
                MenuItem = new Models.MenuItem()
            };
        }

        public async  Task<IActionResult> Index()
        {
            var menuItems = await _db.MenuItem
                .Include(m=>m.Category)
                .Include(m=>m.SubCategory)
                .ToListAsync();
                
            return View(menuItems);
        }

        //GET - CREATE
        public IActionResult Create()
        {
            return View(this.MenuItemViewModel);
        }

        [HttpPost,ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMenuItem()
        {
            this.MenuItemViewModel.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());

            if(!ModelState.IsValid)
            {
                return View(this.MenuItemViewModel);
            }

            await _db.MenuItem.AddAsync(this.MenuItemViewModel.MenuItem);
            await _db.SaveChangesAsync();

            var webRootPath = _hostingEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            var menuItemFromDb = await _db.MenuItem.FindAsync(this.MenuItemViewModel.MenuItem.Id);

            if(files.Count>0)
            {
                //files has been uploaded
                var uploads = Path.Combine(webRootPath, "images");
                var extension = Path.GetExtension(files[0].FileName);

                await using (var filesStream = new FileStream(Path.Combine(uploads, this.MenuItemViewModel.MenuItem.Id + extension), FileMode.Create))
                {
                    await files[0].CopyToAsync(filesStream);
                }

                menuItemFromDb.Image = $@"\images\{this.MenuItemViewModel.MenuItem.Id}{extension}";
            }
            else
            {
                //no file was uploaded, so use default
                var uploads = Path.Combine(webRootPath, @"images\" + SD.DefaultFoodImage);
                System.IO.File.Copy(uploads, webRootPath + @"\images\" + this.MenuItemViewModel.MenuItem.Id + ".png");
                menuItemFromDb.Image = @"\images\" + this.MenuItemViewModel.MenuItem.Id + ".png";
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        //GET - EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (id==null)
            {
                return NotFound();
            }

            this.MenuItemViewModel.MenuItem = await _db.MenuItem
                .Include(m => m.Category)
                .Include(m => m.SubCategory)
                .SingleOrDefaultAsync(m => m.Id == id);
            
            this.MenuItemViewModel.SubCategory = await _db.SubCategory
                .Where(s => s.CategoryId == this.MenuItemViewModel.MenuItem.CategoryId)
                .ToListAsync();

            if (this.MenuItemViewModel.MenuItem ==null)
            {
                return NotFound();
            }

            return View(this.MenuItemViewModel);
        }

        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMenuItem(int? id)
        {
            if (id==null)
            {
                return NotFound();
            }

            this.MenuItemViewModel.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());

            if (!ModelState.IsValid)
            {
                this.MenuItemViewModel.SubCategory = await _db.SubCategory
                    .Where(s => s.CategoryId == this.MenuItemViewModel.MenuItem.CategoryId)
                    .ToListAsync();

                return View(this.MenuItemViewModel);
            }

            var webRootPath = _hostingEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            var menuItemFromDb = await _db.MenuItem.FindAsync(this.MenuItemViewModel.MenuItem.Id);

            if (files.Count > 0)
            {
                //New Image has been uploaded
                var uploads = Path.Combine(webRootPath, "images");
                var newExtension = Path.GetExtension(files[0].FileName);

                //Delete the original file
                var imagePath = Path.Combine(webRootPath, menuItemFromDb.Image.TrimStart('\\'));

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                //we will upload the new file
                await using (var filesStream = new FileStream(Path.Combine(uploads, this.MenuItemViewModel.MenuItem.Id + newExtension), FileMode.Create))
                {
                    await files[0].CopyToAsync(filesStream);
                }

                menuItemFromDb.Image = @"\images\" + this.MenuItemViewModel.MenuItem.Id + newExtension;
            }

            menuItemFromDb.Name = this.MenuItemViewModel.MenuItem.Name;
            menuItemFromDb.Description = this.MenuItemViewModel.MenuItem.Description;
            menuItemFromDb.Price = this.MenuItemViewModel.MenuItem.Price;
            menuItemFromDb.Spicyness = this.MenuItemViewModel.MenuItem.Spicyness;
            menuItemFromDb.CategoryId = this.MenuItemViewModel.MenuItem.CategoryId;
            menuItemFromDb.SubCategoryId = this.MenuItemViewModel.MenuItem.SubCategoryId;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        //GET : Details MenuItem
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            this.MenuItemViewModel.MenuItem = await _db.MenuItem
                .Include(m => m.Category)
                .Include(m => m.SubCategory)
                .SingleOrDefaultAsync(m => m.Id == id);

            if (this.MenuItemViewModel.MenuItem == null)
            {
                return NotFound();
            }

            return View(this.MenuItemViewModel);
        }

        //GET : Delete MenuItem
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            this.MenuItemViewModel.MenuItem = await _db.MenuItem
                .Include(m => m.Category)
                .Include(m => m.SubCategory)
                .SingleOrDefaultAsync(m => m.Id == id);

            if (this.MenuItemViewModel.MenuItem == null)
            {
                return NotFound();
            }

            return View(this.MenuItemViewModel);
        }

        //POST Delete MenuItem
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            string webRootPath = _hostingEnvironment.WebRootPath;
            MenuItem menuItem = await _db.MenuItem.FindAsync(id);

            if (menuItem != null)
            {
                var imagePath = Path.Combine(webRootPath, menuItem.Image.TrimStart('\\'));

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
                
                _db.MenuItem.Remove(menuItem);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}