using Microsoft.EntityFrameworkCore;
using ConsultaCnpjReceita.Model;
using WebConsultaCnpjReceita.Models;

namespace Data.AppDbContext;

public class AppDbContext : DbContext
{
    public DbSet<tb_stg_estoque_full> tb_stg_estoque_full { get; set; }
    public DbSet<tb_stg_estoque_full_hemera> tb_stg_estoque_full_hemera { get; set; }
    public DbSet<Root> tb_aux_Retorno_Receita { get; set; }
    public DbSet<XmlAmbimaModel> tb_aux_Xml_Anbima { get; set; }
    public DbSet<FinaxisEstoque> tb_stg_estoque_finaxis_full { get; set; }
    public DbSet<HemeraEstoque> tb_stg_estoque_hemera_full { get; set; }
    public DbSet<EstoqueModel> tb_stg_estoque_singulare_full { get; set; }
    public DbSet<HemeraLiquidados> tb_stg_liquidados_bancario_hemera_full { get; set; }
    public DbSet<FinaxisLiquidados> tb_stg_liquidados_finaxis_full { get; set; }
    public DbSet<HemeraLiquidadosRecompra> tb_stg_liquidados_recompra_hemera_full { get; set; }
    public DbSet<SingulareLiquidados> tb_stg_liquidados_singulare_full { get; set; }
    public DbSet<WebhookModel> tb_aux_callback_estoque_singulare { get; set; }
    public DbSet<NovoXmlAnbima> tb_aux_retorno_xml_anbima { get; set; }
    public DbSet<TituloPrivadoCarteira> tb_ods_titulo_privado_carteira { get; set; }
    public DbSet<FundoDetalhes> tb_fato_carteira { get; set; }
    public DbSet<RelatoriosProcessados> tb_aux_relatorios_processados { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NovoXmlAnbima>()
            .Property(e => e.Xml)
            .HasColumnType("xml");
    }
}