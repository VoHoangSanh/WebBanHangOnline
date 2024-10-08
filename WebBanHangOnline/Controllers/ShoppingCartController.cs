﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanHangOnline.Models;
using WebBanHangOnline.Models.EF;

namespace WebBanHangOnline.Controllers
{
    public class ShoppingCartController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        // GET: ShoppingCart
        public ActionResult Index()
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null)
            {
                ViewBag.CheckCart = cart;
            }

            return View();
        }

        public ActionResult CheckOut_Success()
        {
            return View();
        }

        public ActionResult Partial_CheckOut()
        {
            return PartialView();
        }
        public ActionResult CheckOut()
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null && cart.Items.Any())
            {
                ViewBag.CheckCart = cart;
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CheckOut(OrderViewModel req)
        {
            var code = new { Success = false, Code = -1 };
            if (ModelState.IsValid)
            {
                ShoppingCart cart = (ShoppingCart)Session["Cart"];
                if (cart != null)
                {
                    Order order = new Order();
                    order.CustumerName = req.CustumerName;
                    order.Phone = req.Phone;
                    order.Address = req.Address;
                    order.Email = req.Email;
                    cart.Items.ForEach(x => order.OrderDetails.Add(new OrderDetail
                    {

                        ProductId = x.ProductId,
                        Quantity = x.Quantity,
                        Price = x.Price,
                    }));
                    order.TotalAmount = cart.Items.Sum(x => (x.Price * x.Quantity));
                    order.TypePayment = req.TypePayment;
                    order.CreatedDate = DateTime.Now;
                    order.ModifiedDate = DateTime.Now;
                    order.CreatedBy = req.Phone;
                    Random rd = new Random();
                    order.Code = "DH" + rd.Next(0, 100) + rd.Next(0, 9) + rd.Next(0, 9) + rd.Next(0, 9) + rd.Next(0, 9) + rd.Next(0, 9) + rd.Next(0, 9);
                    db.Orders.Add(order);
                    db.SaveChanges();

                    //send mail
                    var strSanPham = "";
                    double thanhtien =0;
                    double tongtien =0;
                    foreach(var sp in cart.Items){
                        strSanPham += "<tr>";
                        strSanPham += "<td>"+sp.ProductName+"</td>";
                        strSanPham += "<td>"+sp.Quantity+"</td>";
                        strSanPham += "<td>"+sp.TotalPrice+"</td>";
                        strSanPham += "</tr>";
                        thanhtien += sp.Price * sp.Quantity;
                    
                    }
                    tongtien = thanhtien;
                    //user
                    string contentCustumer = System.IO.File.ReadAllText(Server.MapPath("~/Content/templateSendEmail/send2.html"));
                    contentCustumer = contentCustumer.Replace("{{MaDon}}", order.Code);
                    contentCustumer = contentCustumer.Replace("{{SanPham}}", strSanPham);
                    contentCustumer = contentCustumer.Replace("{{NgayDat}}", DateTime.Now.ToString("dd/mm/yyyy"));
                    contentCustumer = contentCustumer.Replace("{{TenKhachHang}}", order.CustumerName);
                    contentCustumer = contentCustumer.Replace("{{Phone}}", order.Phone);
                    contentCustumer = contentCustumer.Replace("{{Email}}", req.Email);
                    contentCustumer = contentCustumer.Replace("{{DiaChiNhanHang}}", order.Address);
                    contentCustumer = contentCustumer.Replace("{{ThanhTien}}", thanhtien.ToString());
                    contentCustumer = contentCustumer.Replace("{{TongTien}}", tongtien.ToString() );
                    WebBanHangOnline.Common.Common.SendEmail("HelmetShop", "Đơn hàng #" + order.Code, contentCustumer.ToString(), req.Email);

                    //admin
                    string contentAdmin = System.IO.File.ReadAllText(Server.MapPath("~/Content/templateSendEmail/send1.html"));
                    contentAdmin = contentAdmin.Replace("{{MaDon}}", order.Code);
                    contentAdmin = contentAdmin.Replace("{{SanPham}}", strSanPham);
                    contentAdmin = contentAdmin.Replace("{{NgayDat}}", DateTime.Now.ToString("dd/mm/yyyy"));
                    contentAdmin = contentAdmin.Replace("{{TenKhachHang}}", order.CustumerName);
                    contentAdmin = contentAdmin.Replace("{{Phone}}", order.Phone);
                    contentAdmin = contentAdmin.Replace("{{Email}}", req.Email);
                    contentAdmin = contentAdmin.Replace("{{DiaChiNhanHang}}", order.Address);
                    contentAdmin = contentAdmin.Replace("{{ThanhTien}}", thanhtien.ToString());
                    contentAdmin = contentAdmin.Replace("{{TongTien}}", tongtien.ToString());
                    WebBanHangOnline.Common.Common.SendEmail("HelmetShop", "Đơn hàng mới #" + order.Code, contentAdmin.ToString(), ConfigurationManager.AppSettings["EmailAdmin"]);
                    cart.ClearCart();
                    return RedirectToAction("CheckOut_Success");
                }
            }
            return Json(code);
        }

        public ActionResult Partial_Item_Cart()
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null && cart.Items.Any())
            {
                return PartialView(cart.Items);

            }

            return PartialView();
        }

        public ActionResult Partial_Item_CheckOut()
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null && cart.Items.Any())
            {
                return PartialView(cart.Items);

            }

            return PartialView();
        }


        public ActionResult ShowCount()
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null) {
                return Json(new {  Count = cart.Items.Count },JsonRequestBehavior.AllowGet);

            }
            return Json( new { Success = false ,Count = 0}, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult AddToCart( int id, int quantity)
        {
            var code = new { Success = false, msg = "", code = -1 , Count = 0};
            var db = new ApplicationDbContext();
            var checkProduct = db.Products.FirstOrDefault(x => x.Id == id);
            if (checkProduct != null) {
                ShoppingCart cart = (ShoppingCart)Session["Cart"];

                if (cart == null) {
                    cart = new ShoppingCart();
                }
                ShoppingCartItem item = new ShoppingCartItem
                {
                    ProductId = checkProduct.Id,
                    ProductName = checkProduct.Title,
                    CategoryName = checkProduct.ProductCategory.Title,
                    Alias = checkProduct.Alias,
                    Quantity = quantity,
                };

                if (checkProduct.ProductImage.FirstOrDefault(x => x.isDefault) != null)
                {
                    item.ProductImg = checkProduct.ProductImage.FirstOrDefault(x => x.isDefault).Image;
                }

                item.Price = checkProduct.Price;
                if (checkProduct.PriceSale > 0)
                {
                    item.Price = (double)checkProduct.PriceSale;
                }
                item.TotalPrice = item.Quantity * item.Price;
                cart.AddToCart(item, quantity);
                Session["Cart"] = cart;
                code = new { Success = true, msg = "Thêm sản phẩm vào giỏi hàng thành công!", code = 1, Count = cart.Items.Count  };
            }
            return Json(code);
        }

       

        [HttpPost]
        public ActionResult Delete(int id) {
            var code = new { Success = false, msg = "", code = -1, Count = 0 };
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null)
            {
                var checkProduct = cart.Items.FirstOrDefault(x => x.ProductId == id);
                if (checkProduct != null) {
                    cart.Romove(id);
                     code = new { Success = true, msg = "", code = 1, Count = cart.Items.Count };
                }
            }
            return Json(code);
        }

        [HttpPost]
        public ActionResult DeleteAll()
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null)
            {
                cart.ClearCart();
                return Json(new { Success = true });
            }
            return Json(new { Success = false });
        }

        [HttpPost]
        public ActionResult Update(int id, int quantity)
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null)
            {
                cart.UpdateQuantity(id, quantity);
                return Json(new { Success = true });
            }
            return Json(new { Success = false });
        }
    }
}