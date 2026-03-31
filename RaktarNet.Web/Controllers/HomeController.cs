using Microsoft.AspNetCore.Mvc;
using RaktarNet.Web.Models;
using RaktarNet.Web.Services;
using System.Text.Json;

namespace RaktarNet.Web.Controllers;

public class HomeController : Controller
{
    private readonly DatabaseService _db;

    public HomeController(DatabaseService db)
    {
        _db = db;
    }

    private SessionUser? CurrentUser()
    {
        var raw = HttpContext.Session.GetString("user");
        return raw is null ? null : JsonSerializer.Deserialize<SessionUser>(raw);
    }

    public IActionResult Index(string search = "")
    {
        var user = CurrentUser();
        if (user is null)
            return RedirectToAction("Login", "Account");

        var vm = new DashboardViewModel
        {
            CurrentUser = user,
            Products = _db.GetProducts(search),
            Logs = _db.GetLogs(),
            Users = user.Role == "admin" ? _db.GetUsers() : new List<UserItem>(),
            Search = search
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult AddProduct(string nev, string kod, int mennyiseg, int egysegar)
    {
        try { _db.AddProduct(nev, kod, mennyiseg, egysegar); }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult UpdateProduct(string oldKod, string nev, string kod, int mennyiseg, int egysegar)
    {
        try { _db.UpdateProduct(oldKod, nev, kod, mennyiseg, egysegar); }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult DeleteProduct(string kod)
    {
        try { _db.DeleteProduct(kod); }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult MoveStock(string kod, string tipus, int mennyiseg, string megjegyzes)
    {
        var user = CurrentUser();
        if (user is null) return RedirectToAction("Login", "Account");

        try { _db.MoveStock(kod, tipus, mennyiseg, megjegyzes, user.Username); }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult AddUser(string username, string password, string role)
    {
        var user = CurrentUser();
        if (user is null || user.Role != "admin")
            return RedirectToAction("Index");

        try { _db.AddUser(username, password, role); }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult DeleteUser(int id)
    {
        var user = CurrentUser();
        if (user is null || user.Role != "admin")
            return RedirectToAction("Index");

        try { _db.DeleteUser(id); }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("Index");
    }
}
