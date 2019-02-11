using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly MongoHelper DB;

        public AccountController(MongoHelper DB)
        {
            this.DB = DB;
        }

        public IActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                AuthUserX user = DB.CheckAuthentication(model.Username, model.Password);
                if (user != null)
                {
                    List<Claim> claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.DisplayName),
                    };

                    StringBuilder permsStr = new StringBuilder();
                    if (user.IsAdmin)
                    {
                        foreach (string p in Enum.GetNames(typeof(Permission)))
                            permsStr.Append(p).Append(",");
                        claims.Add(new Claim("IsAdmin", "true"));
                    }
                    else
                    {
                        foreach (Permission p in user.Permissions)
                            permsStr.Append(p).Append(",");
                    }
                    claims.Add(new Claim(nameof(Permission), permsStr.ToString()));
                    

                    ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    ClaimsPrincipal principal = new ClaimsPrincipal(identity);
                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                        new AuthenticationProperties { IsPersistent = model.RememberMe }).Wait();
                    return RedirectToLocal(model.ReturnUrl);
                }
            }
            ModelState.AddModelError("", "نام کاربری یا رمز عبور صحیح نیست!");
            return View(model);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (returnUrl != null && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            else
                return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [Authorize(policy: "Admin")]
        public IActionResult List()
        {
            return View(DB.All<AuthUserX>());
        }

        [Authorize(policy: "Admin")]
        public IActionResult Add()
        {
            return View(new AccountViewModel());
        }

        [Authorize(policy: "Admin")]
        [HttpPost]
        public IActionResult Add(AccountViewModel model)
        {
            DB.Save(model.User);
            return RedirectToAction(nameof(List));
        }

        [Authorize(policy: "Admin")]
        public IActionResult Edit(string id)
        {
            var user = DB.FindById<AuthUserX>(id);
            var model = new AccountViewModel { User = user };
            return View(model);
        }

        [Authorize(policy: "Admin")]
        [HttpPost]
        public IActionResult Edit(AccountViewModel model)
        {
            if(DB.Any<AuthUserX>(u => u.Username == model.User.Username && u.Id != model.User.Id))
            {
                ModelState.AddModelError("Username", "نام کاربری قبلا موجود است!");
                return View(model);
            }
            
            var update = Builders<AuthUserX>.Update
                .Set(u => u.Disabled, model.User.Disabled)
                .Set(u => u.FirstName, model.User.FirstName)
                .Set(u => u.LastName, model.User.LastName)
                .Set(u => u.Username, model.User.Username)
                .Set(u => u.Permissions, model.User.Permissions);

            if (!string.IsNullOrEmpty(model.Password))
                update = update.Set(u => u.HashedPassword, model.User.HashedPassword);

            DB.UpdateOne(u => u.Id == model.User.Id, update);
            return RedirectToAction(nameof(List));
        }


        [Authorize(policy: "Admin")]
        public IActionResult Delete(string id)
        {
            var user = DB.FindById<AuthUserX>(id);
            if(!user.IsAdmin)
                DB.DeleteOne(user);
            return RedirectToAction(nameof(List));
        }

    }
}