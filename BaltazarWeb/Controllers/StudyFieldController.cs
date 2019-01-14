using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    [Authorize]
    public class StudyFieldController : Controller
    {
        private readonly MongoHelper DB;

        public StudyFieldController(MongoHelper DB)
        {
            this.DB = DB;
        }

        public IActionResult Index()
        {
            return View(DB.All<StudyField>());
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Add(StudyField StudyField)
        {
            if (DB.Any<StudyField>(i => i.Name == StudyField.Name))
            {
                ModelState.AddModelError("", "استان با این نام قبلا وارد شده است!");
                return View(StudyField);
            }
            DB.Save(StudyField);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            if (DB.Any<Course>(c => c.StudyFieldId == objId))
                ModelState.AddModelError("", "قابل حذف نیست!");
            else
                DB.DeleteOne<StudyField>(ObjectId.Parse(id));
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(string id)
        {
            return View(DB.FindById<StudyField>(id));
        }

        [HttpPost]
        public IActionResult Edit(StudyField StudyField, string id)
        {
            var updateDef = Builders<StudyField>.Update.Set(a => a.Name, StudyField.Name);
            DB.UpdateOne(a => a.Id == ObjectId.Parse(id), updateDef);
            return RedirectToAction(nameof(Index));
        }
    }
}