using ContratosIA.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ContratosIA.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Contrato> Contratos { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Contrato>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Valor).HasColumnType("decimal(18,2)");
            e.Property(c => c.Tipo).HasConversion<string>();
            e.Property(c => c.Status).HasConversion<string>();
            e.HasOne(c => c.User)
             .WithMany(u => u.Contratos)
             .HasForeignKey(c => c.UserId);
        });
    }
}
