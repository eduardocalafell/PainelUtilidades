using Microsoft.EntityFrameworkCore;
using ConsultaCnpjReceita.Model;

namespace Data.AppDbContext;

public class AppDbContext : DbContext
{
    public DbSet<Estoque> Estoque { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }
}