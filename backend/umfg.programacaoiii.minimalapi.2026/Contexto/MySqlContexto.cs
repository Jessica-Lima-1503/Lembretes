using Microsoft.EntityFrameworkCore;
using umfg.programacaoiii.minimalapi._2026.Entidades;

namespace umfg.programacaoiii.minimalapi._2026.Contexto;

public class MySqlContexto(DbContextOptions<MySqlContexto> options) : DbContext(options)
{
    public DbSet<Lembrete> Lembrete => Set<Lembrete>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Lembrete>(entity =>
        {
            entity.ToTable("Lembrete");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnType("char(36)").IsRequired();
            entity.Property(x => x.Descricao).HasMaxLength(500).IsRequired();
            entity.Property(x => x.DataCadastro).HasColumnType("datetime(6)").IsRequired();
            entity.Property(x => x.DataAtualizacao).HasColumnType("datetime(6)").IsRequired();
            entity.Property(x => x.IsAtivo).IsRequired();
        });
    }
}
