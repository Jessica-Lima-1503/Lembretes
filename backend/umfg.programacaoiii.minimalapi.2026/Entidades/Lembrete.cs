namespace umfg.programacaoiii.minimalapi._2026.Entidades;

public class Lembrete
{
    public Guid Id { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public DateTime DataCadastro { get; private set; }
    public DateTime DataAtualizacao { get; private set; }
    public bool IsAtivo { get; private set; }

    // Construtor utilizado pelo Entity Framework Core.
    private Lembrete() { }

    public Lembrete(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("A descrição é obrigatória.", nameof(descricao));

        Id = Guid.NewGuid();
        Descricao = descricao.Trim();
        DataCadastro = DateTime.UtcNow;
        DataAtualizacao = DataCadastro;
        IsAtivo = true;
    }

    public void SetDescricao(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("A descrição é obrigatória.", nameof(descricao));

        Descricao = descricao.Trim();
    }

    public void SetAtivo(bool ativo) => IsAtivo = ativo;

    public void Update() => DataAtualizacao = DateTime.UtcNow;
}
