
using Bulky.Models;
using Bulky.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc.Rendering;
using Bulky.Models.ViewModel;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork dBContext, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = dBContext;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Product> categoriesList = _unitOfWork.Product.GetAll(includeProperties:"Category").ToList();
            
            return View(categoriesList);
        }
        public IActionResult UPSert(int? id)
        {
            ProductVM productVM = new (){
                Product=new Product(),
                CategoryListItems= _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString(),
                })
            };
            if (id == 0 || id == null)
            {
                //create
                return View(productVM);
            }
            else { 
                //update
                productVM.Product=_unitOfWork.Product.Get(u=>u.Id == id);
                return View(productVM);
            }


        }
        [HttpPost]
        public IActionResult UPSert(ProductVM productVM,IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file!=null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath= Path.Combine(wwwRootPath, @"images\products");
                    if(!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                    {
                        string imageOLdPath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('\\'));
                        if(System.IO.File.Exists(imageOLdPath))
                        {
                            System.IO.File.Delete(imageOLdPath);
                        }
                    }
                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    productVM.Product.ImageUrl=@"\images\products\" + fileName;
                }
                if (productVM.Product.Id == 0) 
                {
                    _unitOfWork.Product.Add(productVM.Product);
                }
                else
                {
                    _unitOfWork.Product.Update(productVM.Product);
                }
                _unitOfWork.Save();
                TempData["success"] = "Product Created Successfully ";
                return RedirectToAction("Index");
            }
            else
            {
                productVM.CategoryListItems = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString(),
                });
                return View(productVM);
            }
           
        }
        
        #region
        [HttpGet]
        public IActionResult GetAll() {
            List<Product> productsList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = productsList });
        }
        [HttpDelete]
        public IActionResult Delete(int? id) {
            Product? ProductDb = _unitOfWork.Product.Get(u => u.Id == id);
            if (ProductDb == null)
            {
                return Json(new {success=false,message="Error while deleting"});
            }
            string imageOLdPath = Path.Combine(_webHostEnvironment.WebRootPath, ProductDb.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(imageOLdPath))
            {
                System.IO.File.Delete(imageOLdPath);
            }
            _unitOfWork.Product.Remove(ProductDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}
