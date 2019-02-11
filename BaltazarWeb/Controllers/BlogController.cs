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
    public class BlogController : Controller
    {
        private readonly MongoHelper DB;
        private readonly string ImageUploadPath;

        public BlogController(MongoHelper DB, IHostingEnvironment env)
        {
            this.DB = DB;
            ImageUploadPath = Path.Combine(env.WebRootPath, Consts.UPLOAD_IMAGE_DIR);
            if (!Directory.Exists(ImageUploadPath))
                Directory.CreateDirectory(ImageUploadPath);
        }

        [Authorize(policy: nameof(Permission.ManageBlogs))]
        public IActionResult List()
        {
            return View(DB.Find<Blog>(_ => true).SortByDescending(b => b.DateAdded).ToEnumerable());
        }

        [Authorize(policy: nameof(Permission.ManageBlogs))]
        public IActionResult Add()
        {
            return View();
        }
        
        [Authorize(policy: nameof(Permission.ManageBlogs))]
        [HttpPost]
        public IActionResult Add(Blog item)
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
            return RedirectToAction(nameof(List));
        }

        [Authorize(policy: nameof(Permission.ManageBlogs))]
        public IActionResult Edit(string id)
        {
            return View(DB.FindById<Blog>(id));
        }

        [Authorize(policy: nameof(Permission.ManageBlogs))]
        [HttpPost]
        public IActionResult Edit(Blog item, string id)
        {
            item.Id = ObjectId.Parse(id);
            if (item.ImageFile != null)
            {
                item.HasImage = true;
                string filePath = Path.Combine(ImageUploadPath, item.Id + ".jpg");
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    item.ImageFile.CopyTo(fs);
                }
            }
            DB.Save(item);
            return RedirectToAction(nameof(List));
        }

        [Authorize(policy: nameof(Permission.ManageBlogs))]
        public IActionResult Delete(string id)
        {
            DB.DeleteOne<Blog>(ObjectId.Parse(id));
            return RedirectToAction(nameof(List));
        }
        

        public ActionResult<DataResponse<List<Blog>>> App([FromHeader] Guid token)
        {
            var student = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if(student != null)
                DB.UpdateOne<Student>(s => s.Id == student.Id, Builders<Student>.Update.Set(s => s.LastBlogVisit, DateTime.Now));

            return new DataResponse<List<Blog>>
            {
                Success = true,
                Data = DB.Find<Blog>(b => b.ShowOnApp).SortByDescending(b => b.DateAdded).ToList()
            };
        }
    }
}