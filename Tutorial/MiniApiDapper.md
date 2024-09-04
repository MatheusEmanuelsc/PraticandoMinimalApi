### Tutorial: Criando uma API Minimal com Atualização Parcial Usando Dapper e Dapper.Contrib no .NET 8

Este tutorial guiará você na criação de uma API Minimal no .NET 8 utilizando Dapper para acesso ao banco de dados. A aplicação contará com operações CRUD (Create, Read, Update, Delete) e uma atualização parcial (`PATCH`) utilizando Dapper.Contrib. Abaixo, seguimos um passo a passo detalhado, com explicações sobre cada etapa e um resumo das funcionalidades implementadas.

## Índice

1. [Etapa 0: Criar o Projeto Vazio](#etapa-0-criar-o-projeto-vazio)
2. [Etapa 1: Instalar os Pacotes Necessários](#etapa-1-instalar-os-pacotes-necessários)
3. [Etapa 2: Ajuste no `appsettings.json`](#etapa-2-ajuste-no-appsettingsjson)
4. [Estrutura do Projeto](#estrutura-do-projeto)
5. [Parte 1: Criação das Classes na Pasta `Data`](#parte-1-criação-das-classes-na-pasta-data)
    - [Record `Tarefa`](#record-tarefa)
    - [Contexto `TarefaContext`](#contexto-tarefacontext)
6. [Parte 2: Configuração de Persistência na Pasta `Extensions`](#parte-2-configuração-de-persistência-na-pasta-extensions)
7. [Parte 3: Configuração do `Program.cs`](#parte-3-configuração-do-programcs)
8. [Parte 4: Definição dos Endpoints na Pasta `Endpoints`](#parte-4-definição-dos-endpoints-na-pasta-endpoints)
9. [Resumo dos Pacotes Utilizados](#resumo-dos-pacotes-utilizados)

## Etapa 0: Criar o Projeto Vazio

Crie um novo projeto vazio utilizando o comando:

```bash
dotnet new web -n TarefasApi
cd TarefasApi
```

## Etapa 1: Instalar os Pacotes Necessários

Instale os pacotes necessários para o projeto. Utilize os comandos abaixo:

```bash
dotnet add package Dapper
dotnet add package Dapper.Contrib
dotnet add package System.Data.SqlClient
```

Esses pacotes são essenciais para a comunicação com o banco de dados SQL Server utilizando Dapper e suas extensões para facilitar operações CRUD.

## Etapa 2: Ajuste no `appsettings.json`

Ajuste o arquivo `appsettings.json` para adicionar a string de conexão com o banco de dados. Adicione a seção `"ConnectionStrings"` conforme o exemplo:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS; Initial Catalog=TarefasDb; Integrated Security=True; Encrypt=True; TrustServerCertificate=True" // Ajuste feito aqui
  },
  "AllowedHosts": "*"
}
```

**Destaque:** A linha `"DefaultConnection": ...` foi adicionada para definir a string de conexão ao banco de dados `TarefasDb`.

## Estrutura do Projeto

Para manter o projeto organizado, crie as seguintes pastas:

- `Data`: para as classes e contextos relacionados ao banco de dados.
- `Endpoints`: para definir os endpoints da API.
- `Extensions`: para a configuração da Service Collection.

## Parte 1: Criação das Classes na Pasta `Data`

### Record `Tarefa`

Crie o arquivo `Tarefa.cs` na pasta `Data`:

```csharp
namespace TarefasApi.Data;

[Table("Tarefas")]
public record Tarefa(int Id, string Atividade, string Status);
```

**Por que usar `record` e não `class`?**

- O `record` é utilizado aqui por sua imutabilidade e simplicidade na definição de modelos de dados. Ele é ideal para cenários onde as instâncias são principalmente usadas para transportar dados.
- A principal diferença entre `record` e `class` é que `record` implementa igualdade estrutural por padrão, enquanto `class` utiliza igualdade por referência.

### Contexto `TarefaContext`

Crie o arquivo `TarefaContext.cs` na pasta `Data`:

```csharp
using System.Data;

namespace TarefasApi.Data
{
    public class TarefaContext
    {
        public delegate Task<IDbConnection> GetConnection();
    }
}
```

Esse contexto define um `delegate` para obter conexões de banco de dados de forma assíncrona, simplificando a injeção de dependência.

## Parte 2: Configuração de Persistência na Pasta `Extensions`

Crie o arquivo `ServiceCollectionsExtensions.cs` na pasta `Extensions`:

```csharp
using System.Data.SqlClient;
using static TarefasApi.Data.TarefaContext;

namespace TarefasApi.Extensions;

public static class ServiceCollectionsExtensions
{
    public static WebApplicationBuilder AddPersistence(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        builder.Services.AddScoped<GetConnection>(sp =>
        async () =>
        {
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        });
        return builder;
    }
}
```

Este método de extensão configura a injeção de dependência para gerenciar a conexão com o banco de dados.

## Parte 3: Configuração do `Program.cs`

Modifique o `Program.cs` para configurar o uso da persistência e mapeamento de endpoints.

```csharp
using TarefasApi.Extensions;
using TarefasApi.EndPoints;

var builder = WebApplication.CreateBuilder(args);

// Adiciona a configuração de persistência ao projeto
builder.AddPersistence();

var app = builder.Build();

// Mapeia os endpoints definidos
app.MapTarefasEndpoints();

app.Run();
```

## Parte 4: Definição dos Endpoints na Pasta `Endpoints`

Crie o arquivo `TarefasEndpoints.cs` na pasta `Endpoints` e defina os endpoints para as operações CRUD e atualização parcial:

```csharp
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.AspNetCore.JsonPatch;
using TarefasApi.Data;
using static TarefasApi.Data.TarefaContext;

namespace TarefasApi.EndPoints
{
    public static class TarefasEndpoints
    {
        public static void MapTarefasEndpoints(this WebApplication app)
        {
            app.MapGet("/", () => $"Bem-Vindo a Api Tarefas - {DateTime.Now}");

            app.MapGet("/tarefas", async (GetConnection connectionGetter) =>
            {
                using var con = await connectionGetter();
                var tarefas = con.GetAll<Tarefa>().ToList();
                if (tarefas is null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(tarefas);
            });

            app.MapGet("/tarefas/{id}", async (GetConnection connectionGetter, int id) =>
            {
                using var con = await connectionGetter();
                var tarefa = con.Get<Tarefa>(id);
                if (tarefa is null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(tarefa);
            });

            app.MapPost("/tarefas", async (GetConnection connectionGetter, Tarefa tarefa) =>
            {
                using var con = await connectionGetter();
                var id = con.Insert(tarefa);
                return Results.Created($"/tarefas/{id}", tarefa);
            });

            app.MapPut("/tarefas", async (GetConnection connectionGetter, Tarefa tarefa) =>
            {
                using var con = await connectionGetter();
                var updated = con.Update(tarefa);
                if (!updated)
                {
                    return Results.BadRequest("Falha ao atualizar a tarefa.");
                }
                return Results.Ok();
            });

            // Atualização Parcial com PATCH
            app.MapPatch("/tarefas/{id}", async (GetConnection connectionGetter, int id, JsonPatchDocument<Tarefa> patchDoc) =>
            {
                if (patchDoc == null)
                {
                    return Results.BadRequest();
                }

                using var con = await connectionGetter();
                var tarefa = con.Get<Tarefa>(id);
                if (tarefa is null)
                {
                    return Results.NotFound();
                }

                patchDoc.ApplyTo(tarefa);

                // Atualiza a tarefa no banco de dados
                var updated = con.Update(tarefa);
                if (!updated)
                {
                    return Results.BadRequest("Erro ao aplicar atualizações parciais.");
                }

                return Results.Ok(tarefa);
            });

            app.MapDelete("/tarefas/{id}", async (GetConnection connectionGetter, int id) =>
            {
                using var con = await connectionGetter();
                var tarefa = con.Get<Tarefa>(id);
                if (tarefa is null)
                {
                    return Results.NotFound();
                }
                con.Delete(tarefa);
                return Results.Ok(tarefa);
            });
        }
    }
}
```

## Resumo dos Pacotes Utilizados

- **Dapper**: Biblioteca para mapeamento de objetos-relacionais (ORM) que permite consultas SQL dinâmicas e simples.
- **Dapper.Contrib**: Extensão do Dapper que facilita operações CRUD (Create, Read, Update, Delete).
- **System.Data.SqlClient**: Provedor de dados para SQL Server, necessário para conectar o projeto ao banco de dados SQL Server.

Este tutorial fornece um passo a passo completo para criar uma API Minimal no .NET 8 utilizando Dapper, com todas as operações CRUD necessárias, além de permitir atualizações parciais com o método `PATCH`.