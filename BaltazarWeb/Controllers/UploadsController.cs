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

namespace BaltazarWeb.Controllers
{
    [Authorize(policy: nameof(Permission.ManageUploads))]
    public class UploadsController : Controller
    {
        private readonly MongoHelper DB;
        private readonly string FileUploadPath;
        public UploadsController(MongoHelper DB, IHostingEnvironment env)
        {
            this.DB = DB;
            FileUploadPath = Path.Combine(env.WebRootPath, Consts.UPLOAD_FILE_DIR);
            if (!Directory.Exists(FileUploadPath))
                Directory.CreateDirectory(FileUploadPath);
        }

        public IActionResult Index()
        {
            return View(DB.All<UploadedFile>());
        }

        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        public IActionResult New(UploadedFile uploadedFile)
        {
            if (uploadedFile.File == null)
                return View();
            uploadedFile.FileName = uploadedFile.File.FileName;
            uploadedFile.Id = ObjectId.GenerateNewId();
            uploadedFile.Date = DateTime.Now;
            string uploadPath = Path.Combine(FileUploadPath, uploadedFile.Id + Path.GetExtension(uploadedFile.FileName));
            using (FileStream fs = new FileStream(uploadPath, FileMode.CreateNew))
                uploadedFile.File.CopyTo(fs);
            DB.Save(uploadedFile);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(string id)
        {
            var file = DB.FindById<UploadedFile>(id);
            string filePath = Path.Combine(FileUploadPath, file.Id + Path.GetExtension(file.FileName));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
            DB.DeleteOne<UploadedFile>(file.Id);
            return RedirectToAction(nameof(Index));
        }
    }
}