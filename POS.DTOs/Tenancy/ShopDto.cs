using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.DTOs.Tenancy
{
    public class ShopDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? ContactNo { get; set; }
        public bool IsMainBranch { get; set; }
        public string? InvoicePrefix { get; set; }
    }
    public class CreateShopDto
    {
        public string Name { get; set; } = default!;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? ContactNo { get; set; }
        public string? InvoicePrefix { get; set; }
    }

    public class UpdateShopDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? ContactNo { get; set; }
        public string? InvoicePrefix { get; set; }
    }

    // ---------- User <-> Shop assignment (UserShop join table) ----------
    public class AssignUserToShopDto
    {
        public int UserId { get; set; }
        public int ShopId { get; set; }
        public bool IsDefault { get; set; }
    }
}
