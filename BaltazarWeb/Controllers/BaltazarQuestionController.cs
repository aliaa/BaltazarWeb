using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    [Authorize(policy: nameof(Permission.ManageLeagueQuestions))]
    public class BaltazarQuestionController : Controller
    {
        private readonly MongoHelper DB;
        private readonly string ImageUploadPath;
        public BaltazarQuestionController(MongoHelper DB, IHostingEnvironment env)
        {
            this.DB = DB;
            ImageUploadPath = Path.Combine(env.WebRootPath, Consts.UPLOAD_IMAGE_DIR);
            if (!Directory.Exists(ImageUploadPath))
                Directory.CreateDirectory(ImageUploadPath);
        }
        
        public IActionResult Index()
        {
            return View(DB.Find<BaltazarQuestion>(_ => true).SortByDescending(q => q.CreateDate).ToEnumerable());
        }
        
        public IActionResult Add()
        {
            return View();
        }
        
        [HttpPost]
        public IActionResult Add(BaltazarQuestion item)
        {
            item.HasImage = item.ImageFile != null;
            DB.Save(item);
            if (item.ImageFile != null)
            {
                string filePath = Path.Combine(ImageUploadPath, item.Id + ".jpg");
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    item.ImageFile.CopyTo(fs);
                }
            }
            return RedirectToAction(nameof(Index));
        }
        
        public IActionResult Edit(string id)
        {
            return View(DB.FindById<BaltazarQuestion>(id));
        }
        
        [HttpPost]
        public IActionResult Edit(BaltazarQuestion item, string id)
        {
            item.Id = ObjectId.Parse(id);
            DB.Save(item);
            return RedirectToAction(nameof(Index));
        }
        
        public IActionResult Delete(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            if (DB.Any<Answer>(a => a.QuestionId == objId))
                ModelState.AddModelError("", "آیتم قابل حذف نیست!");
            else
                DB.DeleteOne<BaltazarQuestion>(objId);
            return RedirectToAction(nameof(Index));
        }
    }
}