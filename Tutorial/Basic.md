

# Tutorial Completo de Minimal API com Atualização Parcial (PATCH) Usando .NET 8

## Índice

1. [Introdução](#introducao)
2. [Etapa 1: Criar o Projeto](#etapa-1-criar-o-projeto)
   - [Criar com Visual Studio](#criar-com-visual-studio)
   - [Criar com CLI](#criar-com-cli)
3. [Etapa 2: Instalar Pacotes Necessários](#etapa-2-instalar-pacotes-necessarios)
   - [Pacotes Requeridos](#pacotes-requeridos)
   - [Instalação dos Pacotes](#instalacao-dos-pacotes)
4. [Etapa 3: Limpeza do Projeto](#etapa-3-limpeza-do-projeto)
   - [Remover Conteúdos Desnecessários](#remover-conteudos-desnecessarios)
   - [Exemplos de Endpoints](#exemplos-de-endpoints)
5. [Etapa 4: Estrutura do Projeto](#etapa-4-estrutura-do-projeto)
   - [Definindo as Classes e o Contexto](#definindo-as-classes-e-o-contexto)
   - [Registrando o DbContext](#registrando-o-dbcontext)
6. [Etapa 5: Implementando os Endpoints](#etapa-5-implementando-os-endpoints)
   - [GET, POST, PUT, DELETE](#get-post-put-delete)
   - [Implementação da Atualização Parcial (PATCH)](#implementacao-da-atualizacao-parcial-patch)
7. [Conclusão](#conclusao)

---

## Introdução

Este guia detalhado mostra como criar e configurar uma **Minimal API** no .NET 8 com um banco de dados InMemory e suportar operações CRUD completas, incluindo atualização parcial (PATCH). O tutorial foi projetado para ser claro e fácil de seguir, cobrindo desde a criação do projeto até a configuração de todos os endpoints.

---

## Etapa 1: Criar o Projeto

### Criar com Visual Studio

1. Abra o Visual Studio e selecione **Criar um novo projeto**.
2. Escolha o template **ASP.NET Core Web API**.
3. No assistente de criação:
   - Nomeie o projeto.
   - Selecione **.NET 8** como framework.
   - Desmarque a opção para criar controladores.
   - Conclua a criação.

### Criar com CLI

Para criar o projeto usando a linha de comando:

```bash
dotnet new webapi -n MinimalApiExample
cd MinimalApiExample
```

Esse comando cria um novo projeto Web API com a estrutura básica, mas sem controladores e outros arquivos extras.

---

## Etapa 2: Instalar Pacotes Necessários

### Pacotes Requeridos

Para que o projeto funcione corretamente, você precisará adicionar alguns pacotes NuGet ao projeto:

1. **Microsoft.EntityFrameworkCore.InMemory**: Fornece suporte ao banco de dados InMemory para desenvolvimento e testes.
2. **Microsoft.AspNetCore.Mvc.NewtonsoftJson**: Necessário para habilitar suporte ao PATCH com `JsonPatchDocument`.

### Instalação dos Pacotes

Para instalar os pacotes necessários, utilize os comandos abaixo no terminal da CLI, dentro da pasta do projeto:

```bash
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Microsoft.AspNetCore.Mvc.NewtonsoftJson
```

Ou, se preferir usar o Visual Studio:

1. Clique com o botão direito no projeto > **Gerenciar Pacotes NuGet**.
2. Procure por `Microsoft.EntityFrameworkCore.InMemory` e `Microsoft.AspNetCore.Mvc.NewtonsoftJson`.
3. Instale ambos os pacotes.

Esses pacotes são essenciais para configurar o banco de dados em memória e para suportar operações de atualização parcial usando JSON Patch.

---

## Etapa 3: Limpeza do Projeto

### Remover Conteúdos Desnecessários

Remova arquivos de exemplo como `WeatherForecast` e controladores. Esses arquivos não são necessários para nossa Minimal API e só adicionam complexidade desnecessária.

### Exemplos de Endpoints

Adicione alguns endpoints simples para verificar o funcionamento básico da API:

```csharp
// Endpoint simples
app.MapGet("/", () => "Olá mundo");

// Endpoint consumindo outra API
app.MapGet("frases", async () => await new HttpClient().GetStringAsync("https://ron-swanson-quotes.herokuapp.com/v2/quotes"));
```

Esses exemplos são úteis para testar se a API está operando corretamente.

---

## Etapa 4: Estrutura do Projeto

### Definindo as Classes e o Contexto

Crie as classes `Tarefa` e `AppDbContext` abaixo do código do `Program.cs`.

#### Classe Tarefa

Esta classe define o modelo de dados de uma tarefa.

```csharp
class Tarefa
{
    public int Id { get; set; }
    public string? Nome { get; set; }
    public bool IsConcluida { get; set; }
}
```

#### Definindo o Contexto

O `AppDbContext` gerencia a comunicação com o banco de dados InMemory.

```csharp
class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Tarefa> Tarefas => Set<Tarefa>();   
}
```

### Registrando o DbContext

Registre o `AppDbContext` no `Program.cs` antes do `app.Run()`.

```csharp
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("TarefasDB"));
```

Isso configura o Entity Framework Core para usar um banco de dados em memória chamado "TarefasDB".

---

## Etapa 5: Implementando os Endpoints

### GET, POST, PUT, DELETE

Implemente os endpoints CRUD básicos:

```csharp
// GET todas as tarefas
app.MapGet("/tarefas", async (AppDbContext db) => await db.Tarefas.ToListAsync());

// GET tarefa por ID
app.MapGet("/tarefas/{id}", async (AppDbContext db, int id) =>
{
    var tarefa = await db.Tarefas.FindAsync(id);
    return tarefa is not null ? Results.Ok(tarefa) : Results.NotFound();
});

// POST criar nova tarefa
app.MapPost("/tarefas", async (Tarefa tarefa, AppDbContext db) =>
{
    db.Tarefas.Add(tarefa);
    await db.SaveChangesAsync();
    return Results.Created($"/tarefas/{tarefa.Id}", tarefa.Id);
});

// PUT atualizar tarefa
app.MapPut("/tarefas/{id}", async (int id, AppDbContext db, Tarefa inputTarefa) =>
{
    var tarefa = await db.Tarefas.FindAsync(id);
    if (tarefa is null) return Results.NotFound();

    tarefa.Nome = inputTarefa.Nome;
    tarefa.IsConcluida = inputTarefa.IsConcluida;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

// DELETE tarefa
app.MapDelete("/tarefas/{id}", async (int id, AppDbContext db) => 
{ 
    if (await db.Tarefas.FindAsync(id) is Tarefa tarefa)
    {
        db.Tarefas.Remove(tarefa);
        await db.SaveChangesAsync();
        return Results.Ok(tarefa);
    }
    return Results.NotFound();
});
```

### Implementação da Atualização Parcial (PATCH)

Implemente o endpoint de atualização parcial usando `JsonPatchDocument`.

#### Configuração Adicional

Adicione suporte ao `Newtonsoft.Json` no `Program.cs` para que o PATCH funcione corretamente:

```csharp
builder.Services.AddControllers().AddNewtonsoftJson();
```

#### Adicionando o Endpoint PATCH

```csharp
app.MapPatch("/tarefas/{id}", async (int id, JsonPatchDocument<Tarefa> patchDoc, AppDbContext db) =>
{
    var tarefa = await db.Tarefas.FindAsync(id);
    if (tarefa is null) return Results.NotFound();

    // Aplica as mudanças na tarefa
    patchDoc.ApplyTo(tarefa);

    if (!db.Entry(tarefa).IsModified)
        return Results.BadRequest("Nenhuma modificação aplicada.");

    await db.SaveChangesAsync();
    return Results.NoContent();
});
```

#### Exemplo de Requisição PATCH:

```json
[
  { "op": "replace", "path": "/nome", "value": "Nova Tarefa" },
  { "op": "replace", "path": "/isConcluida", "value": true }
]
```

---

## Conclusão

Com este tutorial, você aprendeu como criar uma Minimal API no .NET 8 com suporte completo a operações CRUD, incluindo a atualização parcial (PATCH) usando `JsonPatchDocument`. Este exemplo é ideal para aprender e adaptar para projetos mais complexos.

