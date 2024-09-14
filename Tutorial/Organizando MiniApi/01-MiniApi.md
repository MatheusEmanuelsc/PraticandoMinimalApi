### Organização e Projeto Mini API com .NET 8

Este guia aborda a criação de uma mini API usando .NET 8 com Entity Framework Core e MySQL, sem o uso de controladores tradicionais. A API gerencia um catálogo de produtos e categorias, com os endpoints definidos diretamente na classe `Program`.

### Índice

1. [Etapa 1: Criação do Projeto](#etapa-1-criação-do-projeto)
2. [Etapa 2: Limpeza do Código Inicial](#etapa-2-limpeza-do-código-inicial)
3. [Etapa 3: Instalação de Pacotes](#etapa-3-instalação-de-pacotes)
4. [Etapa 4: Criação dos Modelos de Domínio](#etapa-4-criação-dos-modelos-de-domínio)
5. [Etapa 5: Criação da Classe de Contexto](#etapa-5-criação-da-classe-de-contexto)
6. [Etapa 6: Definição da Connection String](#etapa-6-definição-da-connection-string)
7. [Etapa 7: Configuração do Program.cs](#etapa-7-configuração-do-programcs)
8. [Etapa 8: Definição dos Endpoints](#etapa-8-definição-dos-endpoints)

### Etapa 1: Criação do Projeto

Crie um novo projeto utilizando o Visual Studio ou VS Code. Ao criar o projeto, desmarque a opção que gera os controladores por padrão, pois os endpoints serão definidos manualmente.

### Etapa 2: Limpeza do Código Inicial

Após a criação, remova o código desnecessário, como a classe `WeatherForecast`, que não será utilizada neste projeto.

### Etapa 3: Instalação de Pacotes

Instale os pacotes necessários para utilizar o Entity Framework Core com MySQL:

- **Pomelo.EntityFrameworkCore.MySql**: Provê suporte ao MySQL.
- **Microsoft.EntityFrameworkCore.Design**: Necessário para as ferramentas do EF Core.
- **EF Core Tools**: Instale globalmente usando o comando:

  ```bash
  dotnet tool install --global dotnet-ef
  ```

Atualize os pacotes, se necessário, para garantir a compatibilidade.

### Etapa 4: Criação dos Modelos de Domínio

Crie uma pasta `Models` e adicione os modelos de domínio. Abaixo estão as classes `Categoria` e `Produto`, com a anotação `[JsonIgnore]` na propriedade de navegação `Categoria` para evitar problemas de serialização.

#### Categoria.cs

```csharp
namespace ApiCatalogo.Models
{
    public class Categoria
    {
        public int CategoriaId { get; set; }
        public string? Nome { get; set; }
        public string? Descricao { get; set; }

        [JsonIgnore]
        public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
    }
}
```

#### Produto.cs

```csharp
namespace ApiCatalogo.Models
{
    public class Produto
    {
        public int ProdutoId { get; set; }
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public decimal Preco  { get; set; }
        public string? Imagem { get; set; }
        public DateTime DataCompra { get; set; }
        public int Estoque { get; set; }

        public int CategoriaId { get; set; }

        // Propriedade de navegação com JsonIgnore
        [JsonIgnore]
        public Categoria? Categoria { get; set; }
    }
}
```

### Etapa 5: Criação da Classe de Contexto

Crie uma pasta `Context` e adicione a classe `AppDbContext`, que define o contexto do EF Core e mapeia os modelos para o banco de dados usando a Fluent API.

#### AppDbContext.cs

```csharp
using ApiCatalogo.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiCatalogo.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Categoria>? Categorias { get; set; }
        public DbSet<Produto>? Produtos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configurações da Categoria
            modelBuilder.Entity<Categoria>().HasKey(c => c.CategoriaId);
            modelBuilder.Entity<Categoria>().Property(c => c.Nome)
                .HasMaxLength(100)
                .IsRequired();
            modelBuilder.Entity<Categoria>().Property(c => c.Descricao)
                .HasMaxLength(150)
                .IsRequired();

            // Configurações do Produto
            modelBuilder.Entity<Produto>().HasKey(p => p.ProdutoId);
            modelBuilder.Entity<Produto>().Property(p => p.Nome)
                .HasMaxLength(100)
                .IsRequired();
            modelBuilder.Entity<Produto>().Property(p => p.Descricao)
                .HasMaxLength(150);
            modelBuilder.Entity<Produto>().Property(p => p.Imagem)
                .HasMaxLength(100);
            modelBuilder.Entity<Produto>().Property(p => p.Preco)
                .HasPrecision(14, 2);

            // Configuração de Relacionamento
            modelBuilder.Entity<Produto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Produtos)
                .HasForeignKey(p => p.CategoriaId);
        }
    }
}
```

### Etapa 6: Definição da Connection String

Defina a connection string no arquivo `appsettings.json` para conectar ao banco de dados MySQL:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=DbMiniApiCatalogo;Uid=root;Pwd=b1b2b3b4;"
}
```

### Etapa 7: Configuração do Program.cs

Configure o contexto do banco de dados no `Program.cs` para utilizar o MySQL com a connection string definida:

```csharp
var mysqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(mysqlConnection, ServerVersion.AutoDetect(mysqlConnection));
});
```

### Etapa 8: Definição dos Endpoints

Os endpoints da API são definidos diretamente na classe `Program.cs`. Abaixo estão alguns exemplos de endpoints para `Categorias` e `Produtos`.

#### Endpoints de Categoria

```csharp
app.MapPost("/categorias", async (Categoria categoria, AppDbContext db) => {
    if (categoria is null)
    {
        return Results.BadRequest();
    }
    db.Categorias.Add(categoria);
    await db.SaveChangesAsync();

    return Results.Created($"/categorias/{categoria.CategoriaId}", categoria);
});

app.MapGet("/categorias", async (AppDbContext db) => await db.Categorias.ToListAsync());

app.MapGet("/categorias/{id:int}", async (AppDbContext db, int id) =>
{
    return await db.Categorias.FindAsync(id)
        is Categoria categoria ? Results.Ok(categoria) : Results.NotFound();
});

app.MapPut("/categorias/{id:int}", async (int id, Categoria categoria, AppDbContext db) => {
    if (categoria.CategoriaId != id) return Results.BadRequest();

    var categoriaDb = await db.Categorias.FindAsync(id);
    if (categoriaDb is null) return Results.NotFound();

    categoriaDb.Nome = categoria.Nome;
    categoriaDb.Descricao = categoria.Descricao;

    await db.SaveChangesAsync();
    return Results.Ok(categoriaDb);
});

app.MapDelete("/categorias/{id:int}", async (int id, AppDbContext db) =>
{
    var categoria = await db.Categorias.FindAsync(id);
    if (categoria is null) return Results.NotFound();

    db.Categorias.Remove(categoria);
    await db.SaveChangesAsync();

    return Results.NoContent();
});
```

#### Endpoints de Produto

```csharp
app.MapPost("/produtos", async (Produto produto, AppDbContext db) => {
    db.Produtos.Add(produto);
    await db.SaveChangesAsync();

    return Results.Created($"/produtos/{produto.ProdutoId}", produto);
});

app.MapGet("/produtos", async (AppDbContext db) => await db.Produtos.ToListAsync());

app.MapGet("/produtos/{id:int}", async (int id, AppDbContext db) => {
    return await db.Produtos.FindAsync(id)
        is Produto produto ? Results.Ok(produto) : Results.NotFound();
});

app.MapPut("/produtos/{id:int}", async (int id, Produto produto, AppDbContext db) => {
    if (produto.ProdutoId != id) return Results.BadRequest();

    var produtoDb = await db.Produtos.FindAsync(id);
    if (produtoDb is null) return Results.NotFound();

    produtoDb.Nome = produto.Nome;
    produtoDb.Descricao = produto.Descricao;
    produtoDb.Preco = produto.Preco;
    produtoDb.Imagem = produto.Imagem;
    produtoDb.DataCompra = produto.DataCompra;
    produtoDb.Estoque = produto.Estoque;
    produtoDb.CategoriaId = produto.CategoriaId;

    await db.SaveChangesAsync();
    return Results.Ok(produtoDb);
});

app.MapDelete("/produtos/{id:int}", async (int id, AppDbContext db) =>
{
    var produto = await db.Produtos.Find

Async(id);
    if (produto is null) return Results.NotFound();

    db.Produtos.Remove(produto);
    await db.SaveChangesAsync();

    return Results.NoContent();
});
```

### Conclusão

Este tutorial forneceu uma visão detalhada sobre como criar uma mini API com .NET 8 utilizando EF Core e MySQL. Ao definir os endpoints diretamente na classe `Program`, simplificamos o projeto, mantendo um controle claro e direto sobre cada operação CRUD. As propriedades de navegação foram configuradas para evitar problemas de serialização com a anotação `[JsonIgnore]`.