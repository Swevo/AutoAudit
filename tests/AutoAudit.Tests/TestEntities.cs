using AutoAudit;
using Microsoft.EntityFrameworkCore;

namespace AutoAudit.Tests;

// ── Test entities ──────────────────────────────────────────────────────────

[Auditable]
public partial class Order
{
    public int Id { get; set; }
    public string? Description { get; set; }
}

[Auditable]
public partial class Customer
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

// ── Test DbContext ─────────────────────────────────────────────────────────

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();
}
