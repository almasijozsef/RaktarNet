using System.Collections.Generic;

namespace RaktarNet.Web.Models
{
    public class DashboardViewModel
    {
        public SessionUser CurrentUser { get; set; } = default!;

        public List<Product> Products { get; set; } = new();
        public List<LogEntry> Logs { get; set; } = new();
        public List<UserItem> Users { get; set; } = new();

        public string Search { get; set; } = "";
        public string LogSearch { get; set; } = "";
        public string LogTipus { get; set; } = "";
        public string LogUser { get; set; } = "";
        public string LogDateFrom { get; set; } = "";
        public string LogDateTo { get; set; } = "";

        public int MaiMozgasokSzama { get; set; }
        public int MaiBevetelezesekSzama { get; set; }
        public int MaiKiadasokSzama { get; set; }
        public int AlacsonyKeszletuTermekekSzama { get; set; }
    }
}
