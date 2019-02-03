using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

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
    }
}