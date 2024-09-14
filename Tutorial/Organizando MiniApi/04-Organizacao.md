### Resumo Completo: Implementação de API com Autenticação JWT e Identity no ASP.NET Core

Neste tutorial, abordaremos a criação de uma API utilizando ASP.NET Core com autenticação JWT e Identity para gerenciar a autenticação e autorização de usuários. Também implementaremos endpoints de CRUD para gerenciamento de categorias e produtos, além de configurar serviços auxiliares como CORS e Swagger.

### Estrutura do Projeto

1. **Pasta `ApiEndpoints`**: Contém os endpoints da API:
   - **AutenticacaoEndpoints**: Endpoint para login e registro utilizando JWT e Identity.
   - **CategoriaEndpoints**: CRUD para gerenciamento de categorias.
   - **ProdutoEndpoints**: CRUD para gerenciamento de produtos.

2. **Pasta `AppServiceExtensions`**: Aqui configuraremos as extensões de serviços, como o tratamento de exceções, CORS, Swagger, autenticação JWT e Identity.

---

## 1. Implementando Endpoints na API

### 1.1. Endpoint de Autenticação (Login e Registro)

Utilizaremos o `UserManager` do Identity para gerenciar o registro de novos usuários e o login, com geração de tokens JWT.

```csharp
using ApiCatalogo.Models;
using ApiCatalogo.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace ApiCatalogo.Endpoints
{
    public static class AutenticacaoEndpoints
    {
        public static void MapAutenticaoEndpoints(this WebApplication app)
        {
            // Endpoint para registro de novos usuários
            app.MapPost("/register", [AllowAnonymous] async (UserModel userModel, UserManager<IdentityUser> userManager) =>
            {
                if (userModel == null) return Results.BadRequest("Dados inválidos");
                
                var user = new IdentityUser { UserName = userModel.UserName, Email = userModel.Email };
                var result = await userManager.CreateAsync(user, userModel.Password);
                
                if (result.Succeeded)
                {
                    return Results.Ok("Usuário registrado com sucesso");
                }
                
                return Results.BadRequest(result.Errors);
            })
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK)
            .WithName("Register")
            .WithTags("Autenticacao");

            // Endpoint para login e geração de JWT
            app.MapPost("/login", [AllowAnonymous] async (UserModel userModel, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ITokenService tokenService) =>
            {
                var result = await signInManager.PasswordSignInAsync(userModel.UserName, userModel.Password, false, false);

                if (result.Succeeded)
                {
                    var user = await userManager.FindByNameAsync(userModel.UserName);
                    var tokenString = tokenService.GerarToken(app.Configuration["Jwt:Key"], app.Configuration["Jwt:Issuer"], app.Configuration["Jwt:Audience"], userModel);
                    return Results.Ok(new { token = tokenString });
                }
                else
                {
                    return Results.BadRequest("Login Inválido");
                }
            })
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK)
            .WithName("Login")
            .WithTags("Autenticacao");
        }
    }
}
```

**Ajuste no `Program.cs`:**
```csharp
app.MapAutenticaoEndpoints();
```

### 1.2. Endpoints de Categoria

O código abaixo define as operações CRUD para o gerenciamento de categorias.

```csharp
using ApiCatalogo.Context;
using ApiCatalogo.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiCatalogo.Endpoints
{
    public static class CategoriaEndpoints
    {
        public static void MapCategoriasEndpoints(this WebApplication app)
        {
            // Criar nova categoria
            app.MapPost("/categorias", async (Categoria categoria, AppDbContext db) => {
                if (categoria is null)
                {
                    return Results.BadRequest();
                }
                db.Categorias.Add(categoria);
                await db.SaveChangesAsync();
                return Results.Created($"/categorias/{categoria.CategoriaId}", categoria);
            });

            // Retornar todas as categorias (requere autenticação)
            app.MapGet("/categorias", async (AppDbContext db) =>
               await db.Categorias.ToListAsync()).RequireAuthorization();

            // Retornar categoria por ID
            app.MapGet("/categorias/{id:int}", async (AppDbContext db, int id) =>
            {
                return await db.Categorias.FindAsync(id)
                     is Categoria categoria ? Results.Ok(categoria) : Results.NotFound();
            });

            // Atualizar categoria
            app.MapPut("/categorias/{id:int}", async (int id, Categoria categoria, AppDbContext db) => {
                if (categoria.CategoriaId != id)
                {
                    return Results.BadRequest();
                }
                var categoriaDb = await db.Categorias.FindAsync(id);
                if (categoriaDb == null) return Results.NotFound();
                categoriaDb.Nome = categoria.Nome;
                categoriaDb.Descricao = categoria.Descricao;
                await db.SaveChangesAsync();
                return Results.Ok(categoriaDb);
            });

            // Deletar categoria
            app.MapDelete("/categorias/{id:int}", async (int id, AppDbContext db) =>
            {
                var categoria = await db.Categorias.FindAsync(id);
                if (categoria == null) return Results.NotFound();
                db.Categorias.Remove(categoria);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });
        }
    }
}
```

**Ajuste no `Program.cs`:**
```csharp
app.MapCategoriasEndpoints();
```

### 1.3. Endpoints de Produto

Aqui definimos as operações CRUD para o gerenciamento de produtos.

```csharp
using ApiCatalogo.Context;
using ApiCatalogo.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiCatalogo.Endpoints
{
    public static class ProdutosEndpoints
    {
        public static void MapProdutosEndpoints(this WebApplication app) {

            // Criar novo produto
            app.MapPost("/produtos", async (Produto produto, AppDbContext db) => {
                db.Produtos.Add(produto);
                await db.SaveChangesAsync();
                return Results.Created($"/produtos/{produto.ProdutoId}", produto);
            });

            // Retornar todos os produtos
            app.MapGet("/produtos", async (AppDbContext db) => await db.Produtos.ToListAsync());

            // Retornar produto por ID
            app.MapGet("/produtos/{id:int}", async (int id, AppDbContext db) =>
            {
                return await db.Produtos.FindAsync(id)
                     is Produto produto ? Results.Ok(produto) : Results.NotFound();
            });

            // Atualizar produto
            app.MapPut("/produtos/{id:int}", async (int id, Produto produto, AppDbContext db) => {
                if (produto.ProdutoId != id)
                {
                    return Results.BadRequest();
                }
                var produtoDb = await db.Produtos.FindAsync(id);
                if (produtoDb == null) return Results.NotFound();
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

            // Deletar produto
            app.MapDelete("/produtos/{id:int}", async (int id, AppDbContext db) => {
                var produto = await db.Produtos.FindAsync(id);
                if (produto == null) return Results.NotFound();
                db.Produtos.Remove(produto);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });
        }
    }
}
```

**Ajuste no `Program.cs`:**
```csharp
app.MapProdutosEndpoints();
```

---

## 2. Configurando Serviços

### 2.1. Tratamento de Exceções e CORS

Configuramos o tratamento de exceções e CORS para a aplicação.

```csharp
namespace ApiCatalogo.AppServicesExtensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app, IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            return app;
        }

        public static IApplicationBuilder UseAppCors(this IApplicationBuilder app)
        { 
            app.UseCors(p => {
                p.AllowAnyOrigin();
                p.WithMethods("GET");
                p.AllowAnyHeader();
            });
            return app;
        }

        public static IApplicationBuilder UseSwaggerMiddleware(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            return app;
        }
    }
}
```

**Ajuste no `Program.cs`:**
```csharp
var environment = app.Environment;
app.UseExceptionHandling(environment)
    .UseSwaggerMiddleware()
    .UseAppCors();
```

### 2.2. Configurando Serviços (Swagger, Identity e JWT)

Adicionamos o Swagger, Identity, autenticação JWT e persistência de dados com o Entity Framework.

```csharp
using ApiCatalogo.Context;
using ApiCatalogo.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ApiCatalogo.AppServicesExtensions
{
    public static class ServiceCollectionExtensions
    {
        // Configura o Swagger
        public static Web

ApplicationBuilder AddApiSwagger(this WebApplicationBuilder builder)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            return builder;
        }

        // Configura a persistência de dados com Entity Framework e Identity
        public static WebApplicationBuilder AddPersistence(this WebApplicationBuilder builder)
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

            builder.Services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.AddScoped<ITokenService, TokenService>();
            return builder;
        }

        // Configura a autenticação JWT
        public static WebApplicationBuilder AddJwtAuthentication(this WebApplicationBuilder builder)
        {
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey
                    (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                });
            return builder;
        }
    }
}
```

**Ajuste no `Program.cs`:**
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddApiSwagger()
    .AddJwtAuthentication()
    .AddPersistence();
```

---

## Conclusão

Neste tutorial, implementamos uma API utilizando ASP.NET Core, organizamos o projeto em pastas, configuramos autenticação com JWT e Identity, criamos endpoints de CRUD para categorias e produtos, além de configurar serviços como Swagger, CORS e tratamento de exceções.