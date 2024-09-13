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

app.MapGet("/", () => "Catálogo de Produtos - 2024");

//-------------------------Endpoits Categoria ------------------------------------

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
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();





app.Run();

