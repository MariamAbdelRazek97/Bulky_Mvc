
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
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CompanyController(IUnitOfWork dBContext)
        {
            _unitOfWork = dBContext;
        }
        public IActionResult Index()
        {
            List<Company> categoriesList = _unitOfWork.Company.GetAll().ToList();
            
            return View(categoriesList);
        }
        public IActionResult UPSert(int? id)
        {
            var Company = new Company();
           
            if (id == 0 || id == null)
            {
                //create
                return View(Company);
            }
            else {
                //update
                Company = _unitOfWork.Company.Get(u=>u.Id == id);
                return View(Company);
            }

        }
        [HttpPost]
        public IActionResult UPSert(Company Company)
        {
            if (ModelState.IsValid)
            {
                if (Company.Id == 0) 
                {
                    _unitOfWork.Company.Add(Company);
                }
                else
                {
                    _unitOfWork.Company.Update(Company);
                }
                _unitOfWork.Save();
                TempData["success"] = "Company Created Successfully ";
                return RedirectToAction("Index");
            }
            else
            {
                return View(Company);
            }
           
        }
        
        #region
        [HttpGet]
        public IActionResult GetAll() {
            List<Company> CompanysList = _unitOfWork.Company.GetAll().ToList();
            return Json(new { data = CompanysList });
        }
        [HttpDelete]
        public IActionResult Delete(int? id) {
            Company? CompanyDb = _unitOfWork.Company.Get(u => u.Id == id);
            if (CompanyDb == null)
            {
                return Json(new {success=false,message="Error while deleting"});
            }
            _unitOfWork.Company.Remove(CompanyDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}
