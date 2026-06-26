using AutoAudit;
using Microsoft.EntityFrameworkCore;

namespace AutoAudit.Tests;

/// <summary>Integration tests — the generator runs on this project so generated code is live.</summary>
public class GeneratedCodeTests
{
    // ── Property existence ─────────────────────────────────────────────────

    [Fact]
    public void Order_HasCreatedAtProperty()
    {
        var order = new Order();
        order.CreatedAt.Should().Be(default);
    }

    [Fact]
    public void Order_HasUpdatedAtProperty()
    {
        var order = new Order();
        order.UpdatedAt.Should().Be(default);
    }

    [Fact]
    public void Order_HasCreatedByProperty()
    {
        var order = new Order();
        order.CreatedBy.Should().BeNull();
    }

    [Fact]
    public void Order_HasUpdatedByProperty()
    {
        var order = new Order();
        order.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public void Customer_HasCreatedAtProperty()
    {
        var customer = new Customer();
        customer.CreatedAt.Should().Be(default);
    }

    // ── IAuditableEntity interface ─────────────────────────────────────────

    [Fact]
    public void Order_ImplementsIAuditableEntity()
    {
        var order = new Order();
        order.Should().BeAssignableTo<IAuditableEntity>();
    }

    [Fact]
    public void Customer_ImplementsIAuditableEntity()
    {
        var customer = new Customer();
        customer.Should().BeAssignableTo<IAuditableEntity>();
    }

    [Fact]
    public void AuditProperties_AreSettableThroughInterface()
    {
        IAuditableEntity entity = new Order();
        var now = DateTimeOffset.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        entity.CreatedBy = "alice";
        entity.UpdatedBy = "alice";

        entity.CreatedAt.Should().Be(now);
        entity.UpdatedAt.Should().Be(now);
        entity.CreatedBy.Should().Be("alice");
        entity.UpdatedBy.Should().Be("alice");
    }

    // ── AuditInterceptor — insert ──────────────────────────────────────────

    private static TestDbContext BuildContext(string dbName, Func<string?>? getUser = null)
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(dbName);
        builder.AddAuditInterceptor(getUser);
        return new TestDbContext(builder.Options);
    }

    [Fact]
    public async Task AuditInterceptor_Insert_SetsCreatedAt()
    {
        await using var ctx = BuildContext(nameof(AuditInterceptor_Insert_SetsCreatedAt));
        var before = DateTimeOffset.UtcNow;

        ctx.Orders.Add(new Order { Id = 1 });
        await ctx.SaveChangesAsync();

        var order = await ctx.Orders.FindAsync(1);
        order!.CreatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task AuditInterceptor_Insert_SetsUpdatedAt()
    {
        await using var ctx = BuildContext(nameof(AuditInterceptor_Insert_SetsUpdatedAt));

        ctx.Orders.Add(new Order { Id = 1 });
        await ctx.SaveChangesAsync();

        var order = await ctx.Orders.FindAsync(1);
        order!.UpdatedAt.Should().Be(order.CreatedAt);
    }

    [Fact]
    public async Task AuditInterceptor_Insert_SetsCreatedBy()
    {
        await using var ctx = BuildContext(nameof(AuditInterceptor_Insert_SetsCreatedBy), () => "alice");

        ctx.Orders.Add(new Order { Id = 1 });
        await ctx.SaveChangesAsync();

        var order = await ctx.Orders.FindAsync(1);
        order!.CreatedBy.Should().Be("alice");
    }

    [Fact]
    public async Task AuditInterceptor_Insert_SetsUpdatedBy()
    {
        await using var ctx = BuildContext(nameof(AuditInterceptor_Insert_SetsUpdatedBy), () => "alice");

        ctx.Orders.Add(new Order { Id = 1 });
        await ctx.SaveChangesAsync();

        var order = await ctx.Orders.FindAsync(1);
        order!.UpdatedBy.Should().Be("alice");
    }

    [Fact]
    public async Task AuditInterceptor_Insert_NoUserProvider_UserFieldsAreNull()
    {
        await using var ctx = BuildContext(nameof(AuditInterceptor_Insert_NoUserProvider_UserFieldsAreNull));

        ctx.Orders.Add(new Order { Id = 1 });
        await ctx.SaveChangesAsync();

        var order = await ctx.Orders.FindAsync(1);
        order!.CreatedBy.Should().BeNull();
        order.UpdatedBy.Should().BeNull();
    }

    // ── AuditInterceptor — update ──────────────────────────────────────────

    [Fact]
    public async Task AuditInterceptor_Update_UpdatesUpdatedAt()
    {
        await using var ctx = BuildContext(nameof(AuditInterceptor_Update_UpdatesUpdatedAt));

        ctx.Orders.Add(new Order { Id = 1 });
        await ctx.SaveChangesAsync();

        var order = (await ctx.Orders.FindAsync(1))!;
        var originalUpdatedAt = order.UpdatedAt;

        await Task.Delay(5); // ensure time advances
        order.Description = "changed";
        await ctx.SaveChangesAsync();

        order.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task AuditInterceptor_Update_UpdatesUpdatedBy()
    {
        await using var ctx = BuildContext(nameof(AuditInterceptor_Update_UpdatesUpdatedBy), () => "bob");

        ctx.Orders.Add(new Order { Id = 1 });
        await ctx.SaveChangesAsync();

        var order = (await ctx.Orders.FindAsync(1))!;
        order.Description = "changed";
        await ctx.SaveChangesAsync();

        order.UpdatedBy.Should().Be("bob");
    }

    [Fact]
    public async Task AuditInterceptor_Update_DoesNotChangeCreatedAt()
    {
        await using var ctx = BuildContext(nameof(AuditInterceptor_Update_DoesNotChangeCreatedAt));

        ctx.Orders.Add(new Order { Id = 1 });
        await ctx.SaveChangesAsync();

        var order = (await ctx.Orders.FindAsync(1))!;
        var originalCreatedAt = order.CreatedAt;

        order.Description = "changed";
        await ctx.SaveChangesAsync();

        order.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public async Task AuditInterceptor_Update_DoesNotChangeCreatedBy()
    {
        await using var ctx = BuildContext(nameof(AuditInterceptor_Update_DoesNotChangeCreatedBy), () => "alice");

        ctx.Orders.Add(new Order { Id = 1 });
        await ctx.SaveChangesAsync();

        var order = (await ctx.Orders.FindAsync(1))!;
        order.Description = "changed";
        await ctx.SaveChangesAsync();

        order.CreatedBy.Should().Be("alice");
    }

    // ── Multiple entities in one SaveChanges ──────────────────────────────

    [Fact]
    public async Task AuditInterceptor_MultipleEntities_AllAudited()
    {
        await using var ctx = BuildContext(nameof(AuditInterceptor_MultipleEntities_AllAudited), () => "sys");

        ctx.Orders.Add(new Order { Id = 1 });
        ctx.Customers.Add(new Customer { Id = 1, Name = "Acme" });
        await ctx.SaveChangesAsync();

        var order = await ctx.Orders.FindAsync(1);
        var customer = await ctx.Customers.FindAsync(1);

        order!.CreatedBy.Should().Be("sys");
        customer!.CreatedBy.Should().Be("sys");
    }
}
