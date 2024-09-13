using ApiCatalogo.Context;
using ApiCatalogo.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var mysqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(mysqlConnection, ServerVersion.AutoDetect(mysqlConnection));
});
var app = builder.Build();

//definir os endpoints

app.MapGet("/", () => "Cat�logo de Produtos - 2024");

//-------------------------Endpoints Categoria ------------------------------------

app.MapPost("/categorias",async(Categoria categoria, AppDbContext db) => {
    if (categoria is null)
    {
       return Results.BadRequest();  
    }
    db.Categorias.Add(categoria);
    await db.SaveChangesAsync();

    return Results.Created($"/categorias/{categoria.CategoriaId}",categoria);
});

app.MapGet("/categorias", async (AppDbContext db) => await db.Categorias.ToListAsync());

app.MapGet("/categorias/{id:int}", async(AppDbContext db, int id) =>
{
   return await db.Categorias.FindAsync( id)
        is Categoria categoria ? Results.Ok(categoria): Results.NotFound();
});

app.MapPut("/categoria/{id:int}", async (int id, Categoria categoria, AppDbContext db) => {

    if (categoria.CategoriaId != id)
    {
        return Results.BadRequest();
    }

    var categoriaDb = await db.Categorias.FindAsync(id);
    if (categoriaDb != null) return Results.NotFound();

    categoriaDb.Nome = categoria.Nome;
    categoriaDb.Descricao = categoria.Descricao;

    await db.SaveChangesAsync();
    return Results.Ok(categoriaDb);
});

app.MapDelete("/categorias/{id:int}", async (int id, AppDbContext db) =>
{
    var categoria = await db.Categorias.FindAsync(id);
    if (categoria != null) return Results.NotFound();
    db.Categorias.Remove(categoria);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

//-------------------------Endpoints Produto ------------------------------------

app.MapPost("/produtos", async (Produto produto, AppDbContext db)
 => {
     db.Produtos.Add(produto);
     await db.SaveChangesAsync();

     return Results.Created($"/produtos/{produto.ProdutoId}", produto);
 });

app.MapGet("/produtos", async (AppDbContext db) => await db.Produtos.ToListAsync());

app.MapGet("/produtos/{id:int}", async (int id, AppDbContext db)
    => {
        return await db.Produtos.FindAsync(id)
                     is Produto produto
                     ? Results.Ok(produto)
                     : Results.NotFound();
    });

app.MapPut("/produtos/{id:int}", async (int id, Produto produto, AppDbContext db) =>
{

    if (produto.ProdutoId != id)
    {
        return Results.BadRequest();
    }

    var produtoDB = await db.Produtos.FindAsync(id);

    if (produtoDB is null) return Results.NotFound();

    produtoDB.Nome = produto.Nome;
    produtoDB.Descricao = produto.Descricao;
    produtoDB.Preco = produto.Preco;
    produtoDB.Imagem = produto.Imagem;
    produtoDB.DataCompra = produto.DataCompra;
    produtoDB.Estoque = produto.Estoque;
    produtoDB.CategoriaId = produto.CategoriaId;

    await db.SaveChangesAsync();

    return Results.Ok(produtoDB);
});

app.MapDelete("/produtos/{id:int}", async (int id, AppDbContext db) =>
{
    var produto = await db.Produtos.FindAsync(id);

    if (produto is null)
    {
        return Results.NotFound();
    }

    db.Produtos.Remove(produto);
    await db.SaveChangesAsync();

    return Results.NoContent();
});


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();





app.Run();

