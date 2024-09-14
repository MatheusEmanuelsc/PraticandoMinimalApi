using ApiCatalogo.Context;
using ApiCatalogo.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiCatalogo.Endpoints
{
    public static class CategoriaEndpoints
    {
        public static void MapCategoriasEndpoints(this WebApplication app)
        {
            //-------------------------Endpoints Categoria ------------------------------------

            app.MapPost("/categorias", async (Categoria categoria, AppDbContext db) => {
                if (categoria is null)
                {
                    return Results.BadRequest();
                }
                db.Categorias.Add(categoria);
                await db.SaveChangesAsync();

                return Results.Created($"/categorias/{categoria.CategoriaId}", categoria);
            });

            app.MapGet("/categorias", async (AppDbContext db) =>
               await db.Categorias.ToListAsync()).RequireAuthorization();


            app.MapGet("/categorias/{id:int}", async (AppDbContext db, int id) =>
            {
                return await db.Categorias.FindAsync(id)
                     is Categoria categoria ? Results.Ok(categoria) : Results.NotFound();
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
        }
    }
}
