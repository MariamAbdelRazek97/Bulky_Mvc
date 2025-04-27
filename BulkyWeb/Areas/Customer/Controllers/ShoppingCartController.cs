using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModel;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.BillingPortal;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public ShoppingCartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;    
        }
        public IActionResult Index()
        {
            var claimsIndentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIndentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartVM = new ShoppingCartVM()
            {
                shoppingCartsList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties : "Product"),
                orderHeader=new OrderHeader()
            };
            foreach (var cart in ShoppingCartVM.shoppingCartsList)
            {
                cart.Price = priceDependOnQuantity(cart);
                ShoppingCartVM.orderHeader.OrderTotal += cart.Count * cart.Price;
            }
            return View(ShoppingCartVM);
        }
        public IActionResult Plus(int CartId)
        {
            var cartItem=_unitOfWork.ShoppingCart.Get(u=>u.Id == CartId);
            cartItem.Count += 1;
            _unitOfWork.ShoppingCart.Update(cartItem);
            _unitOfWork.Save();
            return RedirectToAction("Index");
        }
        public IActionResult Minus(int CartId)
        {
            var cartItem = _unitOfWork.ShoppingCart.Get(u => u.Id == CartId);
            if (cartItem.Count <= 1)
            {
                _unitOfWork.ShoppingCart.Remove(cartItem);
            }
            else
            {
                cartItem.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cartItem);
            }
           
            _unitOfWork.Save();
            return RedirectToAction("Index");
        }
        public IActionResult Remove(int CartId)
        {
            var cartItem = _unitOfWork.ShoppingCart.Get(u => u.Id == CartId);
            _unitOfWork.ShoppingCart.Remove(cartItem);
            _unitOfWork.Save();
            return RedirectToAction("Index");
        }
        public IActionResult Summary()
        {
            var claimsIndentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIndentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartVM = new ShoppingCartVM()
            {
                shoppingCartsList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                orderHeader = new OrderHeader()
            };
            ShoppingCartVM.orderHeader.ApplicationUser=_unitOfWork.ApplicationUser.Get(u=>u.Id== userId);
            ShoppingCartVM.orderHeader.Name = ShoppingCartVM.orderHeader.ApplicationUser.Name;
            ShoppingCartVM.orderHeader.StreetAddress = ShoppingCartVM.orderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.orderHeader.PhoneNumber = ShoppingCartVM.orderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.orderHeader.City = ShoppingCartVM.orderHeader.ApplicationUser.City;
            ShoppingCartVM.orderHeader.State = ShoppingCartVM.orderHeader.ApplicationUser.State;
            ShoppingCartVM.orderHeader.PostalCode = ShoppingCartVM.orderHeader.ApplicationUser.PostalCode;


            foreach (var cart in ShoppingCartVM.shoppingCartsList)
            {
                cart.Price = priceDependOnQuantity(cart);
                ShoppingCartVM.orderHeader.OrderTotal += cart.Count * cart.Price;
            }
            return View(ShoppingCartVM);
        }
        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPost()
        {
            var claimsIndentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIndentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
            ShoppingCartVM.shoppingCartsList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");
            ShoppingCartVM.orderHeader.OrderDate=System.DateTime.Now;
            ShoppingCartVM.orderHeader.ApplicationUserId=userId;
            foreach (var cart in ShoppingCartVM.shoppingCartsList)
            {
                cart.Price = priceDependOnQuantity(cart);
                ShoppingCartVM.orderHeader.OrderTotal += cart.Count * cart.Price;
            }
            if(applicationUser.CompanyId.GetValueOrDefault()==0)
            {
                ShoppingCartVM.orderHeader.PaymentStatus=SD.PaymentStatusPending;
                ShoppingCartVM.orderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                ShoppingCartVM.orderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVM.orderHeader.OrderStatus = SD.StatusApproved;
            }
            _unitOfWork.OrderHeader.Add(ShoppingCartVM.orderHeader);
            _unitOfWork.Save();
            foreach (var item in ShoppingCartVM.shoppingCartsList)
            {
                OrderDetail detail=new OrderDetail()
                {
                    ProductId = item.ProductId,
                    Count = item.Count,
                    Price = item.Price,
                    OrderId=ShoppingCartVM.orderHeader.Id
                };
                _unitOfWork.OrderDetails.Add(detail);
                _unitOfWork.Save();
            }
            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //stripe logic
                var domain = "https://localhost:7170/";
                var options = new Stripe.Checkout.SessionCreateOptions
                {
                   
                    SuccessUrl = domain+ $"Customer/ShoppingCart/OrderConfirmation?id={ShoppingCartVM.orderHeader.Id}",
                    CancelUrl= domain+ "Customer/ShoppingCart/Index",
                    LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
                    Mode = "payment",
                };
                foreach (var item in ShoppingCartVM.shoppingCartsList)
                {
                    var sessionLineItem = new Stripe.Checkout.SessionLineItemOptions()
                    {
                        PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions()
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = "usd",
                            ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions()
                            {
                                Name = item.Product.Title
                            }
                        },
                        Quantity=item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }
                var service = new Stripe.Checkout.SessionService();
                Stripe.Checkout.Session session = service.Create(options);
                _unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCartVM.orderHeader.Id,session.Id,session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
                
            }
            return RedirectToAction(nameof(OrderConfirmation),new {id=ShoppingCartVM.orderHeader.Id});
        }
        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader=_unitOfWork.OrderHeader.Get(u=>u.Id==id,includeProperties:"ApplicationUser");
            if(orderHeader.PaymentStatus!=SD.PaymentStatusDelayedPayment)
            {
                var service = new Stripe.Checkout.SessionService();
                Stripe.Checkout.Session session = service.Get(orderHeader.SessionId);
                if(session.PaymentStatus.ToLower()=="paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }
            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.GetAll(u=>u.ApplicationUserId==orderHeader.ApplicationUserId).ToList();
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();
            return View(id);
        }
        private double priceDependOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else if (shoppingCart.Count <= 100)
            {
                return shoppingCart.Product.Price50;
            }
            else {
                return shoppingCart.Product.Price100;
            }
        }
    }
}
