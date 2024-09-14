### Resumo: Autenticação JWT com Identity - Parte 2

Neste tutorial, seguiremos as bases estabelecidas anteriormente, onde implementamos a autenticação JWT. Agora, vamos adicionar **ASP.NET Core Identity** para gerenciar a autenticação de usuários de maneira mais completa. ASP.NET Core Identity oferece uma estrutura poderosa para autenticação, gerenciamento de usuários e funções.

#### **Etapa 1: Geração de Token com Identity**

##### 1.1 Criar a classe `UserModel`
Vamos usar uma classe simples para representar as credenciais de login do usuário. Ela será usada no processo de autenticação.

```csharp
namespace ApiCatalogo.Models
{
    public class UserModel
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }
}
```

##### 1.2 Definir Seção JWT no `appsettings.json`
Aqui vamos definir a chave secreta usada para gerar o token JWT. Essa chave deve ser forte e mantida em segredo.

```json
"Jwt": {
    "Key": "chave secreta aqui",
    "Issuer": "issuer aqui",
    "Audience": "audience aqui"
}
```

##### 1.3 Adicionar o pacote `Microsoft.AspNetCore.Authentication.JwtBearer`
Para trabalhar com JWT, instale o pacote de autenticação JWT:

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

##### 1.4 Criar Serviço para Geração de Token
Este serviço usará a chave JWT definida no `appsettings.json` para gerar o token JWT com base nas credenciais do usuário.

```csharp
using ApiCatalogo.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiCatalogo.Services
{
    public class TokenService : ITokenService
    {
        public string GerarToken(string key, string issuer, string audience, UserModel user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier,Guid.NewGuid().ToString())
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(issuer: issuer,
                                             audience: audience,
                                             claims: claims,
                                             expires: DateTime.Now.AddMinutes(10),
                                             signingCredentials: credentials);

            var tokenHandler = new JwtSecurityTokenHandler();
            var stringToken = tokenHandler.WriteToken(token);
            return stringToken;
        }
    }
}
```

#### **Etapa 2: Integração com ASP.NET Core Identity**

Agora, vamos adicionar suporte ao **Identity** para gerenciar usuários e autenticação.

##### 2.1 Adicionar Pacotes Necessários
Instale os pacotes necessários para Identity e Entity Framework Core:

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

##### 2.2 Configurar Identity no `Program.cs`
Agora, configuramos o Identity no pipeline de serviços da aplicação e adicionamos a autenticação JWT.

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<ITokenService, TokenService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddAuthorization();
```

##### 2.3 Configurar `AppDbContext`
No `AppDbContext`, devemos adicionar o suporte ao Identity.

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ApiCatalogo.Context
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        
        public DbSet<Categoria> Categorias { get; set; }
    }
}
```

#### **Etapa 3: Criação do Endpoint para Login**

Agora vamos criar o endpoint para login. Ele validará o usuário usando o **Identity**, e se as credenciais estiverem corretas, será gerado um token JWT.

```csharp
app.MapPost("/login", [AllowAnonymous] async (UserModel userModel, ITokenService tokenService, UserManager<IdentityUser> userManager) =>
{
    if (userModel == null)
    {
        return Results.BadRequest("Login inválido");
    }

    var user = await userManager.FindByNameAsync(userModel.UserName);
    if (user != null && await userManager.CheckPasswordAsync(user, userModel.Password))
    {
        var tokenString = tokenService.GerarToken(app.Configuration["Jwt:Key"],
            app.Configuration["Jwt:Issuer"],
            app.Configuration["Jwt:Audience"],
            userModel);
        return Results.Ok(new { token = tokenString });
    }
    else
    {
        return Results.BadRequest("Login inválido");
    }
}).Produces(StatusCodes.Status400BadRequest)
  .Produces(StatusCodes.Status200OK)
  .WithName("Login")
  .WithTags("Autenticacao");
```

#### **Etapa 4: Proteger Endpoints**

Agora que o JWT e o Identity estão configurados, podemos proteger nossos endpoints usando o método `RequireAuthorization()`.

```csharp
app.MapGet("/categorias", async (AppDbContext db) => 
    await db.Categorias.ToListAsync())
    .RequireAuthorization();
```

### Conclusão
Com essas etapas, você adicionou autenticação JWT e integração com **ASP.NET Core Identity** em sua aplicação. Agora, o sistema pode gerenciar usuários de maneira mais robusta, proteger endpoints e gerar tokens JWT para autenticação.