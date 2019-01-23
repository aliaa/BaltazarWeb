using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        [Authorize]
        public IActionResult Index()
        {
            return View(DB.Find<ShopItem>(i => i.Enabled).ToEnumerable());
        }

        [Authorize]
        public IActionResult Add()
        {
            return View();
        }

        [Authorize]
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

        [Authorize]
        public IActionResult Delete(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            if (DB.Any<ShopOrder>(s => s.ShopItemId == objId))
                ModelState.AddModelError("", "آیتم قابل حذف نیست!");
            else
                DB.DeleteOne<ShopItem>(objId);
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public IActionResult Edit(string id)
        {
            return View(DB.FindById<ShopItem>(id));
        }

        [Authorize]
        [HttpPost]
        public IActionResult Edit(ShopItem item, string id)
        {
            item.Id = ObjectId.Parse(id);
            if(item.ImageFile != null)
            {
                item.HasImage = true;
                string filePath = Path.Combine(ImageUploadPath, item.Id + ".jpg");
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    item.ImageFile.CopyTo(fs);
                }
            }
            DB.Save(item);
            return RedirectToAction(nameof(Index));
        }

        public ActionResult<List<ShopItem>> ListShopItems(Guid token)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();
            return DB.Find<ShopItem>(i => i.Enabled).SortByDescending(i => i.DateAdded).ToList();
        }

        public ActionResult<CommonResponse> AddOrder(Guid token, string shopItemId)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();
            if(student.CityId == ObjectId.Empty || string.IsNullOrWhiteSpace(student.Address))
                return new CommonResponse { Message = "ثبت سفارش انجام نشد! لطفا اطلاعات پروفایل خود را قبل از سفارش دادن تکمیل نمائید!" };
            ShopItem shopItem = DB.FindById<ShopItem>(shopItemId);
            if (shopItem == null || !shopItem.Enabled)
                return new CommonResponse { Message = "آیتم برای خرید یافت نشد!" };
            if (student.Coins < shopItem.CoinCost)
                return new CommonResponse { Message = "تعداد سکه های شما برای خرید این آیتم کافی نیست!" };

            ShopOrder order = new ShopOrder
            {
                CoinCost = shopItem.CoinCost,
                ShopItemId = shopItem.Id,
                UserId = student.Id
            };
            DB.Save(order);
            return new CommonResponse { Success = true, Message = "سفارش شما ثبت شد!" };
        }

        public ActionResult<List<ShopOrder>> MyOrders(Guid token)
        {
            Student student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (student == null)
                return Unauthorized();
            List<ShopOrder> orders = DB.Find<ShopOrder>(s => s.UserId == student.Id).ToList();
            foreach (var item in orders)
                item.ShopItemName = DB.FindById<ShopItem>(item.ShopItemId)?.Name;
            return orders;
        }
    }
}