using Microsoft.AspNetCore.Mvc;
using RaktarNet.Web.Models;
using RaktarNet.Web.Services;
using System.Text.Json;

namespace RaktarNet.Web.Controllers;

public class AccountController : Controller
{
    private readonly DatabaseService _db;

    public AccountController(DatabaseService db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("user") is not null)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel());
    }

    [HttpPost]
    public IActionResult Login(LoginViewModel model)
    {
        var user = _db.Login(model.Username, model.Password);
        if (user is null)
        {
            model.Error = "Hibás felhasználónév vagy jelszó.";
            return View(model);
        }

        HttpContext.Session.SetString("user", JsonSerializer.Serialize(user));
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
