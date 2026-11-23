using Microsoft.EntityFrameworkCore;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using umfg.programacaoiii.minimalapi._2026.Contexto;
using umfg.programacaoiii.minimalapi._2026.DTO;
using umfg.programacaoiii.minimalapi._2026.Entidades;

namespace umfg.programacaoiii.minimalapi._2026;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString =
            builder.Configuration.GetConnectionString("DefaultConnection")
            ?? builder.Configuration["ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A ConnectionString não foi configurada em appsettings.json.");
        }

        builder.Services.AddDbContext<MySqlContexto>(options =>
            options.UseMySql(
                connectionString,
                new MariaDbServerVersion(new Version(10, 4, 32))));

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        var app = builder.Build();

        // Aplica as migrations automaticamente. O banco informado na ConnectionString
        // precisa existir no servidor MySQL/MariaDB.
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var contexto = scope.ServiceProvider.GetRequiredService<MySqlContexto>();
            await contexto.Database.MigrateAsync();
        }

        Settings.License = LicenseType.Community;

        app.UseCors("Frontend");
        app.UseHttpsRedirection();

        app.MapPost("/lembretes", async (LembreteDTO dto, MySqlContexto contexto) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Descricao) || dto.Descricao.Trim().Length > 500)
                return Results.BadRequest("Descrição obrigatória (até 500 caracteres).");

            var lembrete = new Lembrete(dto.Descricao.Trim());
            contexto.Lembrete.Add(lembrete);
            await contexto.SaveChangesAsync();

            return Results.Created($"/lembretes/{lembrete.Id}", lembrete);
        });

        app.MapGet("/lembretes/{id:guid}", async (Guid id, MySqlContexto contexto) =>
        {
            var lembrete = await contexto.Lembrete
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsAtivo);

            return lembrete is null ? Results.NotFound() : Results.Ok(lembrete);
        });

        app.MapGet("/lembretes", async (MySqlContexto contexto) =>
            Results.Ok(await contexto.Lembrete
                .AsNoTracking()
                .Where(x => x.IsAtivo)
                .OrderByDescending(x => x.DataCadastro)
                .ToListAsync()));

        app.MapGet("/lembretes/filtro", async (
            [AsParameters] LembreteQueryDTO.LembreteQueryRequestDTO dto,
            MySqlContexto contexto) =>
        {
            if (dto.NumeroPagina <= 0 || dto.TamanhoPagina <= 0)
                return Results.BadRequest("Página e tamanho da página devem ser maiores que zero.");

            if (IntervaloInvalido(dto.DataCadastroInicial, dto.DataCadastroFinal) ||
                IntervaloInvalido(dto.DataAtualizacaoInicial, dto.DataAtualizacaoFinal))
                return Results.BadRequest("A data inicial não pode ser maior que a data final.");

            var (lembretes, total) = await FiltrarLembretes(dto, contexto);

            return Results.Ok(new LembreteQueryDTO.LembreteQueryResponseDTO
            {
                Total = total,
                NumeroPagina = dto.NumeroPagina,
                TamanhoPagina = Math.Min(dto.TamanhoPagina, 1000),
                Lembretes = lembretes
            });
        });

        app.MapGet("/lembretes/imprimir", async (
            [AsParameters] LembreteQueryDTO.LembreteQueryRequestDTO dto,
            MySqlContexto contexto) =>
        {
            if (dto.NumeroPagina <= 0 || dto.TamanhoPagina <= 0)
                return Results.BadRequest("Página e tamanho da página devem ser maiores que zero.");

            if (IntervaloInvalido(dto.DataCadastroInicial, dto.DataCadastroFinal) ||
                IntervaloInvalido(dto.DataAtualizacaoInicial, dto.DataAtualizacaoFinal))
                return Results.BadRequest("A data inicial não pode ser maior que a data final.");

            // O relatório respeita os filtros recebidos e pode conter até 1000 registros.
            dto.NumeroPagina = 1;
            dto.TamanhoPagina = 1000;
            var (lembretes, _) = await FiltrarLembretes(dto, contexto);

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);

                    page.Header()
                        .Text("Relatório de Lembretes")
                        .SemiBold()
                        .FontSize(20)
                        .FontColor(Colors.Brown.Medium);

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        column.Spacing(10);
                        column.Item().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}");

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1.4f);
                                columns.RelativeColumn(1.4f);
                                columns.ConstantColumn(55);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CelulaCabecalho).Text("Descrição");
                                header.Cell().Element(CelulaCabecalho).Text("Cadastro");
                                header.Cell().Element(CelulaCabecalho).Text("Atualização");
                                header.Cell().Element(CelulaCabecalho).Text("Status");
                            });

                            foreach (var lembrete in lembretes)
                            {
                                table.Cell().Element(CelulaCorpo).Text(lembrete.Descricao).FontSize(9);
                                table.Cell().Element(CelulaCorpo).Text(lembrete.DataCadastro.ToLocalTime().ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                                table.Cell().Element(CelulaCorpo).Text(lembrete.DataAtualizacao.ToLocalTime().ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                                table.Cell().Element(CelulaCorpo)
                                    .Text(lembrete.IsAtivo ? "Ativo" : "Inativo")
                                    .FontSize(8);
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Página ");
                        text.CurrentPageNumber();
                    });
                });
            });

            return Results.File(documento.GeneratePdf(), "application/pdf", "relatorio-lembretes.pdf");
        });

        app.MapPut("/lembretes/{id:guid}", async (
            Guid id, LembreteDTO dto, MySqlContexto contexto) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Descricao) || dto.Descricao.Trim().Length > 500)
                return Results.BadRequest("Descrição obrigatória (até 500 caracteres).");

            var lembrete = await contexto.Lembrete
                .FirstOrDefaultAsync(x => x.Id == id && x.IsAtivo);

            if (lembrete is null)
                return Results.NotFound();

            lembrete.SetDescricao(dto.Descricao.Trim());
            lembrete.Update();
            await contexto.SaveChangesAsync();

            return Results.Ok(lembrete);
        });

        app.MapDelete("/lembretes/{id:guid}", async (Guid id, MySqlContexto contexto) =>
        {
            var lembrete = await contexto.Lembrete
                .FirstOrDefaultAsync(x => x.Id == id && x.IsAtivo);

            if (lembrete is null)
                return Results.NotFound();

            lembrete.SetAtivo(false);
            lembrete.Update();
            await contexto.SaveChangesAsync();

            return Results.NoContent();
        });

        app.Run();
    }

    private static bool IntervaloInvalido(DateTime? inicial, DateTime? final) =>
        inicial.HasValue && final.HasValue && inicial.Value.Date > final.Value.Date;

    private static async Task<(List<Lembrete> Lembretes, long Total)> FiltrarLembretes(
        LembreteQueryDTO.LembreteQueryRequestDTO dto,
        MySqlContexto contexto)
    {
        var tamanhoPagina = Math.Clamp(dto.TamanhoPagina, 1, 1000);

        IQueryable<Lembrete> query = contexto.Lembrete
            .AsNoTracking()
            .Where(x => x.IsAtivo);

        if (!string.IsNullOrWhiteSpace(dto.Descricao))
        {
            var termo = dto.Descricao.Trim();
            query = query.Where(x => EF.Functions.Like(x.Descricao, $"%{termo}%"));
        }

        if (dto.DataCadastroInicial.HasValue)
        {
            var inicio = dto.DataCadastroInicial.Value.Date;
            query = query.Where(x => x.DataCadastro >= inicio);
        }

        if (dto.DataCadastroFinal.HasValue)
        {
            var fimExclusivo = dto.DataCadastroFinal.Value.Date.AddDays(1);
            query = query.Where(x => x.DataCadastro < fimExclusivo);
        }

        if (dto.DataAtualizacaoInicial.HasValue)
        {
            var inicio = dto.DataAtualizacaoInicial.Value.Date;
            query = query.Where(x => x.DataAtualizacao >= inicio);
        }

        if (dto.DataAtualizacaoFinal.HasValue)
        {
            var fimExclusivo = dto.DataAtualizacaoFinal.Value.Date.AddDays(1);
            query = query.Where(x => x.DataAtualizacao < fimExclusivo);
        }

        var asc = string.Equals(dto.Direcao, "ASC", StringComparison.OrdinalIgnoreCase);

        IOrderedQueryable<Lembrete> ordenada = (dto.Ordem ?? "").ToLowerInvariant() switch
        {
            "descricao" => asc ? query.OrderBy(x => x.Descricao) : query.OrderByDescending(x => x.Descricao),
            "dataatualizacao" => asc ? query.OrderBy(x => x.DataAtualizacao) : query.OrderByDescending(x => x.DataAtualizacao),
            _ => asc ? query.OrderBy(x => x.DataCadastro) : query.OrderByDescending(x => x.DataCadastro)
        };

        ordenada = ordenada.ThenBy(x => x.Id);

        var total = await query.LongCountAsync();
        var pular = (long)(dto.NumeroPagina - 1) * tamanhoPagina;
        var lembretes = await ordenada.Skip((int)Math.Min(pular, int.MaxValue))
            .Take(tamanhoPagina)
            .ToListAsync();

        return (lembretes, total);
    }

    private static IContainer CelulaCabecalho(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).Padding(5);

    private static IContainer CelulaCorpo(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
}
