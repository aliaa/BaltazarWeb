using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    public class ProvinceController : Controller
    {
        private readonly MongoHelper DB;

        public ProvinceController(MongoHelper DB)
        {
            this.DB = DB;
        }

        public IActionResult Index()
        {
            return View(DB.All<Province>());
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Add(Province Province)
        {
            if (DB.Any<Province>(i => i.Name == Province.Name))
            {
                ModelState.AddModelError("", "استان با این نام قبلا وارد شده است!");
                return View(Province);
            }
            DB.Save(Province);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            if (DB.Any<City>(c => c.ProvinceId == objId))
                ModelState.AddModelError("", "قابل حذف نیست!");
            else
                DB.DeleteOne<Province>(ObjectId.Parse(id));
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(string id)
        {
            return View(DB.FindById<Province>(id));
        }

        [HttpPost]
        public IActionResult Edit(Province Province, string id)
        {
            var updateDef = Builders<Province>.Update.Set(a => a.Name, Province.Name);
            DB.UpdateOne(a => a.Id == ObjectId.Parse(id), updateDef);
            return RedirectToAction(nameof(Index));
        }
    }
}