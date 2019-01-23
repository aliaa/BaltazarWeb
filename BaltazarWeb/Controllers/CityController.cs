using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    [Authorize]
    public class CityController : Controller
    {
        private readonly MongoHelper DB;

        public CityController(MongoHelper DB)
        {
            this.DB = DB;
        }

        public IActionResult Index()
        {
            return View(DB.All<City>());
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Add(City City)
        {
            if (DB.Any<City>(i => i.Name== City.Name))
            {
                ModelState.AddModelError("", "شهر با این نام قبلا وارد شده است!");
                return View(City);
            }
            DB.Save(City);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            if (DB.Any<Student>(s => s.CityId == objId))
                ModelState.AddModelError("", "شهر قابل حذف نیست!");
            else
                DB.DeleteOne<City>(objId);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(string id)
        {
            return View(DB.FindById<City>(id));
        }

        [HttpPost]
        public IActionResult Edit(City city, string id)
        {
            var updateDef = Builders<City>.Update.Set(a => a.Name, city.Name);
            DB.UpdateOne(a => a.Id == ObjectId.Parse(id), updateDef);
            return RedirectToAction(nameof(Index));
        }
    }
}