using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using RaktarNet.Web.Models;

namespace RaktarNet.Web.Services;

public sealed class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        var dbPath = configuration["RaktarNet:DatabasePath"] ?? "raktarnet_web.db";
        _connectionString = $"Data Source={dbPath}";
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public void Initialize()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
        CREATE TABLE IF NOT EXISTS dolgozok (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            felhasznalonev TEXT NOT NULL UNIQUE,
            jelszo_hash TEXT NOT NULL,
            jogosultsag TEXT NOT NULL,
            aktiv INTEGER NOT NULL DEFAULT 1,
            letrehozva TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS termekek (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            nev TEXT NOT NULL,
            kod TEXT NOT NULL UNIQUE,
            mennyiseg INTEGER NOT NULL,
            egysegar INTEGER NOT NULL,
            modositva TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS mozgasnaplo (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            datum TEXT NOT NULL,
            tipus TEXT NOT NULL,
            termek_nev TEXT NOT NULL,
            termek_kod TEXT NOT NULL,
            mennyiseg INTEGER NOT NULL,
            keszlet_utana INTEGER NOT NULL,
            megjegyzes TEXT NOT NULL,
            vegrehajto TEXT NOT NULL
        );
        """;
        cmd.ExecuteNonQuery();

        using var countCmd = conn.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM dolgozok";
        var count = Convert.ToInt32(countCmd.ExecuteScalar());

        if (count == 0)
        {
            using var insert = conn.CreateCommand();
            insert.CommandText = """
                INSERT INTO dolgozok (felhasznalonev, jelszo_hash, jogosultsag, aktiv, letrehozva)
                VALUES ($u, $p, $r, 1, $d)
            """;
            insert.Parameters.AddWithValue("$u", "admin");
            insert.Parameters.AddWithValue("$p", HashPassword("admin123"));
            insert.Parameters.AddWithValue("$r", "admin");
            insert.Parameters.AddWithValue("$d", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            insert.ExecuteNonQuery();
        }
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public SessionUser? Login(string username, string password)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, felhasznalonev, jogosultsag, aktiv, jelszo_hash
            FROM dolgozok
            WHERE felhasznalonev = $u
        """;
        cmd.Parameters.AddWithValue("$u", username.Trim());

        using var r = cmd.ExecuteReader();
        if (!r.Read())
            return null;

        var active = r.GetInt32(3) == 1;
        var hash = r.GetString(4);

        if (!active || hash != HashPassword(password))
            return null;

        return new SessionUser(r.GetInt32(0), r.GetString(1), r.GetString(2));
    }

    public List<Product> GetProducts(string search = "")
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();

        if (string.IsNullOrWhiteSpace(search))
        {
            cmd.CommandText = """
                SELECT nev, kod, mennyiseg, egysegar, modositva
                FROM termekek
                ORDER BY nev
            """;
        }
        else
        {
            cmd.CommandText = """
                SELECT nev, kod, mennyiseg, egysegar, modositva
                FROM termekek
                WHERE lower(nev) LIKE lower($s) OR lower(kod) LIKE lower($s)
                ORDER BY nev
            """;
            cmd.Parameters.AddWithValue("$s", $"%{search.Trim()}%");
        }

        var list = new List<Product>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Product
            {
                Nev = r.GetString(0),
                Kod = r.GetString(1),
                Mennyiseg = r.GetInt32(2),
                Egysegar = r.GetInt32(3),
                Modositva = r.GetString(4)
            });
        }

        return list;
    }

    public void AddProduct(string nev, string kod, int mennyiseg, int egysegar, string vegrehajto)
    {
        using var conn = Open();
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO termekek (nev, kod, mennyiseg, egysegar, modositva)
            VALUES ($n, $k, $m, $e, $d)
        """;
        cmd.Parameters.AddWithValue("$n", nev.Trim());
        cmd.Parameters.AddWithValue("$k", kod.Trim());
        cmd.Parameters.AddWithValue("$m", mennyiseg);
        cmd.Parameters.AddWithValue("$e", egysegar);
        cmd.Parameters.AddWithValue("$d", now);
        cmd.ExecuteNonQuery();

        using var log = conn.CreateCommand();
        log.CommandText = """
            INSERT INTO mozgasnaplo
            (datum, tipus, termek_nev, termek_kod, mennyiseg, keszlet_utana, megjegyzes, vegrehajto)
            VALUES ($d, $t, $tn, $tk, $m, $ku, $megj, $v)
        """;
        log.Parameters.AddWithValue("$d", now);
        log.Parameters.AddWithValue("$t", "Új termék");
        log.Parameters.AddWithValue("$tn", nev.Trim());
        log.Parameters.AddWithValue("$tk", kod.Trim());
        log.Parameters.AddWithValue("$m", mennyiseg);
        log.Parameters.AddWithValue("$ku", mennyiseg);
        log.Parameters.AddWithValue("$megj", "Új termék rögzítése");
        log.Parameters.AddWithValue("$v", vegrehajto ?? "");
        log.ExecuteNonQuery();
    }

    public void UpdateProduct(string oldKod, string nev, string kod, int mennyiseg, int egysegar)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE termekek
            SET nev = $n, kod = $k, mennyiseg = $m, egysegar = $e, modositva = $d
            WHERE kod = $old
        """;
        cmd.Parameters.AddWithValue("$n", nev.Trim());
        cmd.Parameters.AddWithValue("$k", kod.Trim());
        cmd.Parameters.AddWithValue("$m", mennyiseg);
        cmd.Parameters.AddWithValue("$e", egysegar);
        cmd.Parameters.AddWithValue("$d", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("$old", oldKod.Trim());

        var affected = cmd.ExecuteNonQuery();
        if (affected == 0)
            throw new Exception("A termék nem található.");
    }

    public void DeleteProduct(string kod)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM termekek WHERE kod = $k";
        cmd.Parameters.AddWithValue("$k", kod.Trim());

        var affected = cmd.ExecuteNonQuery();
        if (affected == 0)
            throw new Exception("A termék nem található.");
    }

    public void MoveStock(string kod, string tipus, int mennyiseg, string megjegyzes, string vegrehajto)
    {
        if (mennyiseg <= 0)
            throw new Exception("A mennyiségnek nagyobbnak kell lennie 0-nál.");

        using var conn = Open();

        string termekNev;
        int currentQty;

        using (var get = conn.CreateCommand())
        {
            get.CommandText = "SELECT nev, mennyiseg FROM termekek WHERE kod = $k";
            get.Parameters.AddWithValue("$k", kod.Trim());

            using var r = get.ExecuteReader();
            if (!r.Read())
                throw new Exception("A megadott termékkód nem található.");

            termekNev = r.GetString(0);
            currentQty = r.GetInt32(1);
        }

        int newQty;
        if (tipus == "Bevételezés")
        {
            newQty = currentQty + mennyiseg;
        }
        else if (tipus == "Kiadás")
        {
            if (currentQty < mennyiseg)
                throw new Exception("Nincs elegendő készlet a kiadáshoz.");

            newQty = currentQty - mennyiseg;
        }
        else
        {
            throw new Exception("Ismeretlen művelettípus.");
        }

        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        using (var update = conn.CreateCommand())
        {
            update.CommandText = """
                UPDATE termekek
                SET mennyiseg = $m, modositva = $d
                WHERE kod = $k
            """;
            update.Parameters.AddWithValue("$m", newQty);
            update.Parameters.AddWithValue("$d", now);
            update.Parameters.AddWithValue("$k", kod.Trim());
            update.ExecuteNonQuery();
        }

        using (var log = conn.CreateCommand())
        {
            log.CommandText = """
                INSERT INTO mozgasnaplo
                (datum, tipus, termek_nev, termek_kod, mennyiseg, keszlet_utana, megjegyzes, vegrehajto)
                VALUES ($d, $t, $tn, $tk, $m, $ku, $megj, $v)
            """;
            log.Parameters.AddWithValue("$d", now);
            log.Parameters.AddWithValue("$t", tipus);
            log.Parameters.AddWithValue("$tn", termekNev);
            log.Parameters.AddWithValue("$tk", kod.Trim());
            log.Parameters.AddWithValue("$m", mennyiseg);
            log.Parameters.AddWithValue("$ku", newQty);
            log.Parameters.AddWithValue("$megj", megjegyzes ?? "");
            log.Parameters.AddWithValue("$v", vegrehajto ?? "");
            log.ExecuteNonQuery();
        }
    }

    public List<LogEntry> GetLogs(
        string search = "",
        string tipus = "",
        string vegrehajto = "",
        string datumTol = "",
        string datumIg = "")
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();

        var whereParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereParts.Add("(lower(termek_nev) LIKE lower($s) OR lower(termek_kod) LIKE lower($s))");
            cmd.Parameters.AddWithValue("$s", $"%{search.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(tipus))
        {
            whereParts.Add("tipus = $tipus");
            cmd.Parameters.AddWithValue("$tipus", tipus.Trim());
        }

        if (!string.IsNullOrWhiteSpace(vegrehajto))
        {
            whereParts.Add("lower(vegrehajto) LIKE lower($v)");
            cmd.Parameters.AddWithValue("$v", $"%{vegrehajto.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(datumTol))
        {
            whereParts.Add("substr(datum, 1, 10) >= $datumTol");
            cmd.Parameters.AddWithValue("$datumTol", datumTol.Trim());
        }

        if (!string.IsNullOrWhiteSpace(datumIg))
        {
            whereParts.Add("substr(datum, 1, 10) <= $datumIg");
            cmd.Parameters.AddWithValue("$datumIg", datumIg.Trim());
        }

        var whereSql = whereParts.Count > 0
            ? " WHERE " + string.Join(" AND ", whereParts)
            : "";

        cmd.CommandText = $@"
            SELECT datum, tipus, termek_nev, termek_kod, mennyiseg, keszlet_utana, megjegyzes, vegrehajto
            FROM mozgasnaplo
            {whereSql}
            ORDER BY id DESC";

        var list = new List<LogEntry>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new LogEntry
            {
                Datum = r.GetString(0),
                Tipus = r.GetString(1),
                TermekNev = r.GetString(2),
                TermekKod = r.GetString(3),
                Mennyiseg = r.GetInt32(4),
                KeszletUtana = r.GetInt32(5),
                Megjegyzes = r.IsDBNull(6) ? "" : r.GetString(6),
                Vegrehajto = r.GetString(7)
            });
        }

        return list;
    }

    public int GetTodayLogCount()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*)
            FROM mozgasnaplo
            WHERE substr(datum, 1, 10) = $today
        """;
        cmd.Parameters.AddWithValue("$today", DateTime.Now.ToString("yyyy-MM-dd"));

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public int GetTodayLogCountByType(string tipus)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*)
            FROM mozgasnaplo
            WHERE substr(datum, 1, 10) = $today
              AND tipus = $tipus
        """;
        cmd.Parameters.AddWithValue("$today", DateTime.Now.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$tipus", tipus.Trim());

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public int GetLowStockCount(int threshold = 5)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*)
            FROM termekek
            WHERE mennyiseg <= $threshold
        """;
        cmd.Parameters.AddWithValue("$threshold", threshold);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<UserItem> GetUsers()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, felhasznalonev, jogosultsag, aktiv, letrehozva
            FROM dolgozok
            ORDER BY felhasznalonev
        """;

        var list = new List<UserItem>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new UserItem
            {
                Id = r.GetInt32(0),
                Username = r.GetString(1),
                Role = r.GetString(2),
                Aktiv = r.GetInt32(3),
                Letrehozva = r.GetString(4)
            });
        }

        return list;
    }

    public void AddUser(string username, string password, string role)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO dolgozok (felhasznalonev, jelszo_hash, jogosultsag, aktiv, letrehozva)
            VALUES ($u, $p, $r, 1, $d)
        """;
        cmd.Parameters.AddWithValue("$u", username.Trim());
        cmd.Parameters.AddWithValue("$p", HashPassword(password));
        cmd.Parameters.AddWithValue("$r", role.Trim());
        cmd.Parameters.AddWithValue("$d", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.ExecuteNonQuery();
    }

    public void DeleteUser(int id)
    {
        using var conn = Open();

        using var check = conn.CreateCommand();
        check.CommandText = "SELECT felhasznalonev FROM dolgozok WHERE id = $id";
        check.Parameters.AddWithValue("$id", id);
        var username = check.ExecuteScalar()?.ToString();

        if (username == "admin")
            throw new Exception("Az alap admin felhasználó nem törölhető.");

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM dolgozok WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);

        var affected = cmd.ExecuteNonQuery();
        if (affected == 0)
            throw new Exception("A felhasználó nem található.");
    }
}
