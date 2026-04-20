namespace RaktarNet.Web.Models;

public record SessionUser(int Id, string Username, string Role);

public class Product
{
    public string Nev { get; set; } = "";
    public string Kod { get; set; } = "";
    public int Mennyiseg { get; set; }
    public int Egysegar { get; set; }
    public string Modositva { get; set; } = "";
}

public class LogEntry
{
    public string Datum { get; set; } = "";
    public string Tipus { get; set; } = "";
    public string TermekNev { get; set; } = "";
    public string TermekKod { get; set; } = "";
    public int Mennyiseg { get; set; }
    public int KeszletUtana { get; set; }
    public string Megjegyzes { get; set; } = "";
    public string Vegrehajto { get; set; } = "";
} 

public class UserItem
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Role { get; set; } = "";
    public int Aktiv { get; set; }
    public string Letrehozva { get; set; } = "";
}

public class LoginViewModel
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Error { get; set; } = "";
}
