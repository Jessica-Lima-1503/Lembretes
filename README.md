# Meus Lembretes — versão corrigida

Projeto com frontend HTML/CSS/JavaScript e API Minimal em .NET 8, Entity Framework Core, MariaDB/MySQL e QuestPDF.

## O que foi ajustado

- Mantido o frontend original em `frontend/index.html`, sem reformulação visual.
- Criadas as classes de entidade, DTO e contexto que estavam ausentes no conjunto de arquivos enviado.
- Incluída migration inicial para criar a tabela `Lembrete`.
- Corrigidos os endpoints CRUD, filtros, paginação e geração do PDF.
- Adicionado perfil HTTPS na porta `7063`, que corresponde à URL usada pelo frontend.
- Incluída validação de conexão e de intervalos de datas.

## Requisitos

- .NET SDK 8
- MariaDB 10.4 ou compatível
- Banco `umfg_lembrete_develop` criado no servidor

## Configurar o banco

Edite `backend/umfg.programacaoiii.minimalapi.2026/appsettings.Development.json` e ajuste usuário, senha, porta e nome do banco conforme sua instalação.

A configuração enviada originalmente usava MariaDB 10.4.32. Por isso, o projeto mantém essa versão no provedor Pomelo.

Exemplo para MariaDB na porta 3306, usuário root sem senha:

`Server=127.0.0.1;Port=3306;Database=umfg_lembrete_develop;User ID=root;Password=;`

Se sua instalação usa a porta 3308, ajuste a porta para `3308` e informe a senha correta.

> O banco precisa existir antes da primeira execução. A migration cria a tabela `Lembrete`, não o banco de dados.

## Executar no Windows

1. Abra o terminal na pasta `backend/umfg.programacaoiii.minimalapi.2026`.
2. Restaure os pacotes:

   `dotnet restore`

3. Confie no certificado HTTPS (uma vez):

   `dotnet dev-certs https --trust`

4. Inicie a API:

   `dotnet run --launch-profile https`

5. Abra `frontend/index.html` no navegador.

A API ficará disponível em `https://localhost:7063`.

## Teste rápido

- `GET https://localhost:7063/lembretes`
- `POST https://localhost:7063/lembretes` com JSON `{"descricao":"Meu primeiro lembrete"}`
- `GET https://localhost:7063/lembretes/filtro?NumeroPagina=1&TamanhoPagina=10`
- `GET https://localhost:7063/lembretes/imprimir?NumeroPagina=1&TamanhoPagina=100`

## Observações

- A exclusão é lógica: o lembrete passa para `IsAtivo = false`.
- Os endpoints de listagem e filtro retornam somente lembretes ativos.
- O relatório respeita os filtros e gera até 1000 registros.
- O frontend foi mantido. Se a porta HTTPS da API for alterada, atualize `API_URL` no início do script de `frontend/index.html`.
