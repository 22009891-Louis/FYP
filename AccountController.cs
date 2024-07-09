using FYP.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RP.SOI.DotNet.Utils;
using System.Data;
using System.Security.Claims;

namespace FYP.Controllers;

public class AccountController : Controller
{
    private const string LOGIN_SQL =
        @"SELECT * FROM SysUser WHERE UserID = '{0}' AND UserPw = HASHBYTES('SHA1', '{1}')";
    private const string ROLE_COL = "UserRole";
    private const string NAME_COL = "Name";

    [AllowAnonymous] // does not require authentication
    public IActionResult Login(string returnUrl = null!)
    {
        TempData["ReturnUrl"] = returnUrl; // redirect user after successful login
        return View("UserLogin");
    }

    [AllowAnonymous]
    [HttpPost]
    public IActionResult Login(UserLogin user) // allow user to login
    {
        if (!AuthenticateUser(user.UserID, user.Password, out ClaimsPrincipal principal))
        {
            ViewData["Message"] = "Incorrect User ID or password";
            ViewData["MsgType"] = "warning";
            return View("UserLogin");
        }
        else
        {
            HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties // change properties to remember user
                {
                    IsPersistent = false
                });
            return RedirectToAction("ViewProduct", "Product");
        }
    }

    [Authorize] // only authorized users
    public IActionResult LogOut(string returnUrl = null!) // signs out user
    {
        HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("ViewProduct", "Product");
    }

    [AllowAnonymous]
    public IActionResult DenyAccess()
    {
        return View();
    }

    [Authorize(Roles = "admin")]
    public IActionResult Users()
    {
        List<SysUser> list = DBUtl.GetList<SysUser>("SELECT * FROM SysUser WHERE UserRole ='student' ");
        return View(list);
    }

    [Authorize(Roles = "admin")]
    public IActionResult Delete(string id)
    {
        string delete = "DELETE FROM SysUser WHERE UserID ='{0}'";
        int result = DBUtl.ExecSQL(delete, id);
        if (result == 1)
        {
            TempData["Message"] = "User Deleted!";
            TempData["MsgType"] = "success";
        }
        else
        {
            TempData["Message"] = DBUtl.DB_Message;
            TempData["MsgType"] = "danger";
        }

        return RedirectToAction("Users");
    }

    [AllowAnonymous]
    public IActionResult SignUp()
    {
        return View("UserSignUp");
    }

    [AllowAnonymous]
    [HttpPost] // allow people to sign up for account
    public IActionResult SignUp(SysUser usr)
    {
        ModelState.Remove("UserRole");
        if (!ModelState.IsValid)
        {
            return View("UserSignUp", usr);
        }
        else
        {
            string insert =
               @"INSERT INTO SysUser(UserID, UserPw, Name, Email, UserRole) VALUES
                 ('{0}', HASHBYTES('SHA1', '{1}'), '{2}', '{3}', 'student')";
            if (DBUtl.ExecSQL(insert, usr.UserID, usr.UserPw, usr.Name, usr.Email) == 1)
            {
                string template = @"Hi {0},<br/><br/> Welcome to Your Cart!
                                    <br/><br/> Regards,<br/><br/> Admin";
                string title = "Account Registration Successful!";
                string message = string.Format(template, usr.Name, usr.UserID, usr.UserPw);

                if (EmailUtl.SendEmail(usr.Email, title, message, out string result))
                {
                    ViewData["Message"] = "User created successfully";
                    ViewData["MsgType"] = "success";
                }
                else
                {
                    ViewData["Message"] = "User created without sending email";
                    ViewData["MsgType"] = "warning";
                }
            }
            else
            {
                ViewData["Message"] = DBUtl.DB_Message;
                ViewData["MsgType"] = "danger";
            }
            return RedirectToAction("Login");
        }
    }

    [AllowAnonymous] // identifies and tells user if user id is already in use
    public IActionResult VerifyUserID(string userID)
    {
        string select = $"SELECT * FROM SysUser WHERE UserID='{userID}'";
        if (DBUtl.GetTable(select).Rows.Count > 0)
        {
            return Json($"{userID} is already in use, please use another ID");
        }
        return Json(true);
    }
    private static bool AuthenticateUser(string userID, string pw, out ClaimsPrincipal principal)
    {
        principal = null!;

        DataTable ds = DBUtl.GetTable(LOGIN_SQL, userID, pw);
        if (ds.Rows.Count == 1)
        {
            principal =
               new ClaimsPrincipal(
                  new ClaimsIdentity(
                    [
                    new Claim(ClaimTypes.NameIdentifier, userID),
                    new Claim(ClaimTypes.Name, ds.Rows[0][NAME_COL]!.ToString()!),
                    new Claim(ClaimTypes.Role, ds.Rows[0][ROLE_COL]!.ToString()!)
                     ], "Basic" // <-- simple form of authentication
                  )
               );
            return true;
        }
        return false;
    }
}
