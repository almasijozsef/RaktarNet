using Microsoft.AspNetCore.Mvc;
using RaktarNet.Web.Models;
using RaktarNet.Web.Services;
using System.Text;
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

    [HttpGet]
    public IActionResult ExportLogsCsv(
        string logSearch = "",
        string logTipus = "",
        string logUser = "",
        string logDateFrom = "",
        string logDateTo = "")
    {
        var user = CurrentUser();
        if (user is null)
            return RedirectToAction("Login", "Account");

        var logs = _db.GetLogs(logSearch, logTipus, logUser, logDateFrom, logDateTo);

        var sb = new StringBuilder();
        sb.AppendLine("Dátum;Típus;Termék;Kód;Mennyiség;Készlet utána;Megjegyzés;Végrehajtotta");

        foreach (var log in logs)
        {
            sb.AppendLine(
                $"{EscapeCsv(log.Datum)};" +
                $"{EscapeCsv(log.Tipus)};" +
                $"{EscapeCsv(log.TermekNev)};" +
                $"{EscapeCsv(log.TermekKod)};" +
                $"{log.Mennyiseg};" +
                $"{log.KeszletUtana};" +
                $"{EscapeCsv(log.Megjegyzes)};" +
                $"{EscapeCsv(log.Vegrehajto)}");
        }

        var bytes = Encoding.Unicode.GetPreamble()
    .Concat(Encoding.Unicode.GetBytes(sb.ToString()))
    .ToArray();

var fileName = $"mozgasnaplo_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

return File(bytes, "text/csv; charset=utf-16", fileName);

        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        value = value.Replace("\"", "\"\"");

        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value}\"";

        return value;
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

        try
        {
            _db.UpdateProduct(oldKod, nev, kod, mennyiseg, egysegar);
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

        try
        {
            _db.DeleteProduct(kod);
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

        try
        {
            _db.AddProduct(nev, kod, mennyiseg, egysegar, user.Username);
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

        try
        {
            _db.MoveStock(kod, tipus, mennyiseg, megjegyzes, user.Username);
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

        try
        {
            _db.AddUser(username, password, role);
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
