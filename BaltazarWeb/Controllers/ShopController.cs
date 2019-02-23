using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Models.ApiModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    public class ShopController : Controller
    {
        private readonly MongoHelper DB;
        private readonly string ImageUploadPath;
        public ShopController(MongoHelper DB, IHostingEnvironment env)
        {
            this.DB = DB;
            ImageUploadPath = Path.Combine(env.WebRootPath, Consts.UPLOAD_IMAGE_DIR);
            if (!Directory.Exists(ImageUploadPath))
                Directory.CreateDirectory(ImageUploadPath);
        }

        [Authorize(policy: nameof(Permission.ManageShops))]
        public IActionResult Index()
        {
            return View(DB.All<ShopItem>());
        }

        [Authorize(policy: nameof(Permission.ManageShops))]
        public IActionResult Add()
        {
            return View();
        }

        [Authorize(policy: nameof(Permission.ManageBlogs))]
        public IActionResult Details(string id)
        {
            return View(DB.FindById<ShopItem>(id));
        }

        [Authorize(policy: nameof(Permission.ManageShops))]
        [HttpPost]
        public IActionResult Add(ShopItem item)
        {
            item.HasImage = item.ImageFile != null;
            DB.Save(item);
            if(item.ImageFile != null)
            {
                string filePath = Path.Combine(ImageUploadPath, item.Id + ".jpg");
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    item.ImageFile.CopyTo(fs);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(policy: nameof(Permission.ManageShops))]
        public IActionResult Delete(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            if (DB.Any<ShopOrder>(s => s.ShopItemId == objId))
                ModelState.AddModelError("", "آیتم قابل حذف نیست!");
            else
                DB.DeleteOne<ShopItem>(objId);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(policy: nameof(Permission.ManageShops))]
        public IActionResult Edit(string id)
        {
            return View(DB.FindById<ShopItem>(id));
        }

        [Authorize(policy: nameof(Permission.ManageShops))]
        [HttpPost]
        public IActionResult Edit(ShopItem item, string id)
        {
            UpdateDefinition<ShopItem> update = Builders<ShopItem>.Update
                .Set(i => i.Name, item.Name)
                .Set(i => i.CoinCost, item.CoinCost)
                .Set(i => i.Enabled, item.Enabled);
            if(item.ImageFile != null)
            {
                item.HasImage = true;
                string filePath = Path.Combine(ImageUploadPath, item.Id + ".jpg");
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    item.ImageFile.CopyTo(fs);
                }
                update = update.Set(i => i.HasImage, true);
            }
            DB.UpdateOne<ShopItem>(i =>  i.Id == ObjectId.Parse(id), update);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(policy: nameof(Permission.ManageShops))]
        [Route("Shop/OrdersList/{status?}")]
        public IActionResult OrdersList(ShopOrder.OrderStatus status = ShopOrder.OrderStatus.WaitForApprove)
        {
            ViewBag.Status = AliaaCommon.Utils.GetDisplayNameOfMember(typeof(ShopOrder.OrderStatus), status.ToString());
            return View(DB.Find<ShopOrder>(o => o.Status == status).SortBy(o => o.OrderDate).ToEnumerable());
        }

        [Authorize(policy: nameof(Permission.ManageShops))]
        public IActionResult ApproveOrder(string id)
        {
            DB.UpdateOne<ShopOrder>(o => o.Id == ObjectId.Parse(id), Builders<ShopOrder>.Update.Set(o => o.Status, ShopOrder.OrderStatus.Approved));
            return RedirectToAction(nameof(OrdersList), new { status = ShopOrder.OrderStatus.WaitForApprove });
        }

        [Authorize(policy: nameof(Permission.ManageShops))]
        public IActionResult RejectOrder(string id)
        {
            DB.UpdateOne<ShopOrder>(o => o.Id == ObjectId.Parse(id), Builders<ShopOrder>.Update.Set(o => o.Status, ShopOrder.OrderStatus.Rejected));
            return RedirectToAction(nameof(OrdersList), new { status = ShopOrder.OrderStatus.WaitForApprove });
        }

        public ActionResult<DataResponse<List<ShopItem>>> ListShopItems([FromHeader] Guid token)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();
            else
                DB.UpdateOne<Student>(s => s.Id == student.Id, Builders<Student>.Update.Set(s => s.LastShopVisit, DateTime.Now));
            var list = DB.Find<ShopItem>(i => i.Enabled).SortByDescending(i => i.DateAdded).ToList();
            return new DataResponse<List<ShopItem>> { Success = true, Data = list };
        }

        public ActionResult<CommonResponse> AddOrder([FromHeader] Guid token, string shopItemId)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null || student.IsTeacher)
                return Unauthorized();
            if(student.CityId == ObjectId.Empty || string.IsNullOrWhiteSpace(student.Address))
                return new CommonResponse { Message = "ثبت سفارش انجام نشد! لطفا اطلاعات پروفایل خود را قبل از سفارش دادن تکمیل نمائید!" };
            ShopItem shopItem = DB.FindById<ShopItem>(shopItemId);
            if (shopItem == null || !shopItem.Enabled)
                return new CommonResponse { Message = "آیتم برای خرید یافت نشد!" };
            if (student.Coins < shopItem.CoinCost)
                return new CommonResponse { Message = "تعداد سکه های شما برای خرید این آیتم کافی نیست!" };

            if (DB.Any<ShopOrder>(o => o.ShopItemId == shopItem.Id && o.UserId == student.Id && 
                    (o.Status == ShopOrder.OrderStatus.WaitForApprove || o.Status == ShopOrder.OrderStatus.Approved)))
                return new CommonResponse { Message = "این سفارش قبلا برای شما ثبت شده است! لطفا منتظر تحویل بمانید." };

            ShopOrder order = new ShopOrder
            {
                CoinCost = shopItem.CoinCost,
                ShopItemId = shopItem.Id,
                UserId = student.Id
            };
            DB.Save(order);

            student.CoinTransactions.Add(new CoinTransaction
            {
                Type = CoinTransaction.TransactionType.Buy,
                Amount = -shopItem.CoinCost,
                SourceId = order.Id
            });
            student.Coins -= shopItem.CoinCost;
            DB.Save(student);

            return new CommonResponse { Success = true, Message = "سفارش شما ثبت شد!" };
        }

        public ActionResult<DataResponse<List<ShopOrder>>> MyOrders([FromHeader] Guid token)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null || student.IsTeacher)
                return Unauthorized();
            List<ShopOrder> orders = DB.Find<ShopOrder>(s => s.UserId == student.Id).ToList();
            foreach (var item in orders)
                item.ShopItemName = DB.FindById<ShopItem>(item.ShopItemId)?.Name;
            return new DataResponse<List<ShopOrder>> { Success = true, Data = orders };
        }

        [HttpDelete]
        [Route("Shop/{id}")]
        public ActionResult<CommonResponse> CancelOrder([FromHeader] Guid token, [FromRoute] string id)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();
            ShopOrder order = DB.FindById<ShopOrder>(id);
            if (order == null)
                return new CommonResponse { Message = "سفارش یافت نشد!" };
            if (order.UserId != student.Id)
                return new CommonResponse { Message = "این سفارش برای شما نیست!" };
            if (order.Status != ShopOrder.OrderStatus.WaitForApprove)
                return new CommonResponse { Message = "سفارش قابل حذف نیست!" };

            var transaction = student.CoinTransactions.LastOrDefault(t => t.SourceId == order.Id);
            if (transaction != null)
            {
                student.CoinTransactions.Remove(transaction);
                student.Coins += Math.Abs(transaction.Amount);
            }
            DB.DeleteOne(order);
            DB.Save(student);

            return new CommonResponse { Success = true, Message = "سفارش شما حذف شد!" };
        }
    }
}