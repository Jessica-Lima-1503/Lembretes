using umfg.programacaoiii.minimalapi._2026.Entidades;

namespace umfg.programacaoiii.minimalapi._2026.DTO;

public class LembreteQueryDTO
{
    public class LembreteQueryRequestDTO
    {
        public int NumeroPagina { get; set; } = 1;
        public int TamanhoPagina { get; set; } = 10;
        public string? Descricao { get; set; }
        public DateTime? DataCadastroInicial { get; set; }
        public DateTime? DataCadastroFinal { get; set; }
        public DateTime? DataAtualizacaoInicial { get; set; }
        public DateTime? DataAtualizacaoFinal { get; set; }
        public string? Ordem { get; set; } = "DataCadastro";
        public string? Direcao { get; set; } = "DESC";
    }

    public class LembreteQueryResponseDTO
    {
        public long Total { get; set; }
        public int NumeroPagina { get; set; }
        public int TamanhoPagina { get; set; }
        public List<Lembrete> Lembretes { get; set; } = [];
    }
}
