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

    public IActionResult Index(
        string search = "",
        string logSearch = "",
        string logTipus = "",
        string logUser = "",
        string logDateFrom = "",
        string logDateTo = "")
    {
        var user = CurrentUser();
        if (user is null)
            return RedirectToAction("Login", "Account");

        var vm = new DashboardViewModel
        {
            CurrentUser = user,
            Products = _db.GetProducts(search),
            Logs = _db.GetLogs(logSearch, logTipus, logUser, logDateFrom, logDateTo),
            Users = user.Role == "admin" ? _db.GetUsers() : new List<UserItem>(),
            Search = search,
            LogSearch = logSearch,
            LogTipus = logTipus,
            LogUser = logUser,
            LogDateFrom = logDateFrom,
            LogDateTo = logDateTo,
            MaiMozgasokSzama = _db.GetTodayLogCount(),
            MaiBevetelezesekSzama = _db.GetTodayLogCountByType("Bevételezés"),
            MaiKiadasokSzama = _db.GetTodayLogCountByType("Kiadás"),
            AlacsonyKeszletuTermekekSzama = _db.GetLowStockCount()
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult UpdateProduct(string oldKod, string nev, string kod, int mennyiseg, int egysegar)
    {
        var user = CurrentUser();
        if (user is null)
            return RedirectToAction("Login", "Account");

        if (user.Role != "vezeto" && user.Role != "admin")
        {
            TempData["Error"] = "Nincs jogosultságod a termék módosításához.";
            return RedirectToAction("Index");
        }

        if (string.IsNullOrWhiteSpace(oldKod) ||
            string.IsNullOrWhiteSpace(nev) ||
            string.IsNullOrWhiteSpace(kod))
        {
            TempData["Error"] = "A termék neve és kódja kötelező.";
            return RedirectToAction("Index");
        }

        if (mennyiseg < 0)
        {
            TempData["Error"] = "A mennyiség nem lehet negatív.";
            return RedirectToAction("Index");
        }

        if (egysegar < 0)
        {
            TempData["Error"] = "Az egységár nem lehet negatív.";
            return RedirectToAction("Index");
        }

        try
        {
            _db.UpdateProduct(oldKod.Trim(), nev.Trim(), kod.Trim(), mennyiseg, egysegar);
            TempData["Success"] = "A termék sikeresen módosítva lett.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult DeleteProduct(string kod)
    {
        var user = CurrentUser();
        if (user is null)
            return RedirectToAction("Login", "Account");

        if (user.Role != "vezeto" && user.Role != "admin")
        {
            TempData["Error"] = "Nincs jogosultságod a termék törléséhez.";
            return RedirectToAction("Index");
        }

        if (string.IsNullOrWhiteSpace(kod))
        {
            TempData["Error"] = "A törléshez érvényes termékkód szükséges.";
            return RedirectToAction("Index");
        }

        try
        {
            _db.DeleteProduct(kod.Trim());
            TempData["Success"] = "A termék sikeresen törölve lett.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult AddProduct(string nev, string kod, int mennyiseg, int egysegar)
    {
        var user = CurrentUser();
        if (user is null)
            return RedirectToAction("Login", "Account");

        if (string.IsNullOrWhiteSpace(nev) || string.IsNullOrWhiteSpace(kod))
        {
            TempData["Error"] = "A termék neve és kódja kötelező.";
            return RedirectToAction("Index");
        }

        if (mennyiseg <= 0)
        {
            TempData["Error"] = "A mennyiségnek 0-nál nagyobbnak kell lennie.";
            return RedirectToAction("Index");
        }

        if (egysegar < 0)
        {
            TempData["Error"] = "Az egységár nem lehet negatív.";
            return RedirectToAction("Index");
        }

        try
        {
            _db.AddProduct(nev.Trim(), kod.Trim(), mennyiseg, egysegar, user.Username);
            TempData["Success"] = "Az új termék sikeresen rögzítve lett.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult MoveStock(string kod, string tipus, int mennyiseg, string megjegyzes)
    {
        var user = CurrentUser();
        if (user is null)
            return RedirectToAction("Login", "Account");

        if (string.IsNullOrWhiteSpace(kod))
        {
            TempData["Error"] = "A termékkód megadása kötelező.";
            return RedirectToAction("Index");
        }

        if (string.IsNullOrWhiteSpace(tipus))
        {
            TempData["Error"] = "A művelet típusa kötelező.";
            return RedirectToAction("Index");
        }

        if (mennyiseg <= 0)
        {
            TempData["Error"] = "A mennyiségnek 0-nál nagyobbnak kell lennie.";
            return RedirectToAction("Index");
        }

        if (string.IsNullOrWhiteSpace(megjegyzes))
        {
            TempData["Error"] = "A megjegyzés megadása kötelező!";
            return RedirectToAction("Index");
        }

        try
        {
            _db.MoveStock(kod.Trim(), tipus.Trim(), mennyiseg, megjegyzes.Trim(), user.Username);
            TempData["Success"] = $"A(z) {tipus} sikeresen végrehajtva.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult AddUser(string username, string password, string role)
    {
        var user = CurrentUser();
        if (user is null || user.Role != "admin")
        {
            TempData["Error"] = "Nincs jogosultságod új felhasználó létrehozásához.";
            return RedirectToAction("Index");
        }

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(role))
        {
            TempData["Error"] = "A felhasználónév, jelszó és jogosultság megadása kötelező.";
            return RedirectToAction("Index");
        }

        try
        {
            _db.AddUser(username.Trim(), password, role.Trim());
            TempData["Success"] = "Az új felhasználó sikeresen létrehozva.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult DeleteUser(int id)
    {
        var user = CurrentUser();
        if (user is null || user.Role != "admin")
        {
            TempData["Error"] = "Nincs jogosultságod a felhasználó törléséhez.";
            return RedirectToAction("Index");
        }

        if (id <= 0)
        {
            TempData["Error"] = "Érvénytelen felhasználó azonosító.";
            return RedirectToAction("Index");
        }

        try
        {
            _db.DeleteUser(id);
            TempData["Success"] = "A felhasználó sikeresen törölve lett.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index");
    }
}
