

# Parte 2: Implementando JWT com Identity e Configuração de Swagger

## Índice
1. [Introdução](#introdução)
2. [Configuração do ASP.NET Core Identity](#configuração-do-aspnet-core-identity)
3. [Configuração do JWT](#configuração-do-jwt)
4. [Configurando Swagger com Suporte a JWT](#configurando-swagger-com-suporte-a-jwt)
5. [Conclusão](#conclusão)

---

## 1. Introdução

Nesta parte do resumo, vamos implementar o **JWT** (JSON Web Token) em uma **Web API** usando **ASP.NET Core Identity** e configurar o **Swagger** para permitir a autenticação de endpoints protegidos com tokens JWT. Isso permitirá documentar e testar facilmente os endpoints da API que exigem autenticação.

## 2. Configuração do ASP.NET Core Identity

Primeiro, vamos configurar o **ASP.NET Core Identity**, que é um sistema completo de gerenciamento de usuários. Ele facilita a criação, autenticação e gerenciamento de usuários, senhas, roles e claims. A configuração do Identity será usada para gerar e validar tokens JWT.

### Passos:

1. **Instalar os Pacotes Necessários**
   Se ainda não tiver instalado, adicione os pacotes necessários:
   ```bash
   dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
   dotnet add package Microsoft.EntityFrameworkCore
   ```

2. **Adicionar o Identity no `Program.cs`**
   No arquivo `Program.cs`, configure o Identity para trabalhar com o **Entity Framework** e o banco de dados:

   ```csharp
   builder.Services.AddDbContext<ApplicationDbContext>(options =>
       options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
           new MySqlServerVersion(new Version(8, 0, 28))));

   builder.Services.AddIdentity<IdentityUser, IdentityRole>()
       .AddEntityFrameworkStores<ApplicationDbContext>()
       .AddDefaultTokenProviders();
   ```

   - **AddIdentity**: Adiciona o Identity ao pipeline, configurando-o para usar o Entity Framework e fornecer os provedores padrão de autenticação.

---

## 3. Configuração do JWT

Agora, vamos configurar o JWT para autenticar os usuários em nossa API.

1. **Configurar JWT no `Program.cs`**:
   Adicione a configuração para gerar e validar tokens JWT:

   ```csharp
   using Microsoft.AspNetCore.Authentication.JwtBearer;
   using Microsoft.IdentityModel.Tokens;
   using System.Text;

   var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtConfig:Secret"]);

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
           ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
           ValidAudience = builder.Configuration["JwtConfig:Audience"],
           IssuerSigningKey = new SymmetricSecurityKey(key)
       };
   });
   ```

   - **AddAuthentication**: Configura o esquema de autenticação padrão como `JwtBearer`.
   - **TokenValidationParameters**: Define os parâmetros de validação do token, incluindo a chave de assinatura e as verificações de emissor e público.

2. **Configurar JWT no `appsettings.json`**:
   No arquivo `appsettings.json`, adicione as configurações para o JWT:
   ```json
   "JwtConfig": {
     "Secret": "SuaChaveSuperSecreta",
     "Issuer": "sua-aplicacao",
     "Audience": "seus-usuarios"
   }
   ```

---

## 4. Configurando Swagger com Suporte a JWT

Agora, vamos configurar o **Swagger** para documentar nossa API e incluir o suporte à autenticação JWT nos testes dos endpoints.

### Passos:

1. **Instalar o Swagger**:
   Se ainda não estiver instalado, adicione o pacote Swagger:
   ```bash
   dotnet add package Swashbuckle.AspNetCore
   ```

2. **Configurar Swagger no `Program.cs`**:
   Adicione as seguintes configurações no `Program.cs` para configurar o Swagger com suporte à autenticação JWT:

   ```csharp
   builder.Services.AddSwaggerGen(c =>
   {
       c.SwaggerDoc("v1", new OpenApiInfo { Title = "ApiCatalogo", Version = "v1" });

       c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
       {
           Name = "Authorization",
           Type = SecuritySchemeType.ApiKey,
           Scheme = "Bearer",
           BearerFormat = "JWT",
           In = ParameterLocation.Header,
           Description = @"JWT Authorization header using the Bearer scheme.
           Enter 'Bearer' [space] and then your token. Example: 'Bearer 12345abcdef'",
       });

       c.AddSecurityRequirement(new OpenApiSecurityRequirement
       {
           {
               new OpenApiSecurityScheme
               {
                   Reference = new OpenApiReference
                   {
                       Type = ReferenceType.SecurityScheme,
                       Id = "Bearer"
                   }
               },
               new string[] { }
           }
       });
   });

   builder.Services.AddEndpointsApiExplorer();
   ```

   - **AddSecurityDefinition**: Define como o Swagger deve lidar com a autenticação JWT, permitindo que você insira o token diretamente no Swagger UI.
   - **AddSecurityRequirement**: Exige que todos os endpoints protegidos pela autenticação JWT tenham o token no cabeçalho `Authorization`.

3. **Ativar o Middleware do Swagger**:
   Adicione os middlewares para o Swagger no pipeline de requisições:

   ```csharp
   app.UseSwagger();
   app.UseSwaggerUI(c =>
   {
       c.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiCatalogo v1");
   });
   ```

   Isso disponibiliza a interface do Swagger na URL `/swagger/index.html`.

---

## 5. Conclusão

Nesta parte, você aprendeu a configurar o **JWT** e o **ASP.NET Core Identity** para autenticar usuários em uma Web API. Também configuramos o **Swagger** para documentar e testar os endpoints protegidos por JWT. Agora, você pode autenticar e autorizar usuários de forma segura e visualizar/testar seus endpoints protegidos diretamente no Swagger UI.

Com essas configurações, você possui uma API segura e documentada, pronta para ser expandida com mais funcionalidades, como autorização baseada em roles e claims.

---

Essa versão inclui todas as etapas que você mencionou e configura o Swagger adequadamente para o uso com JWT, garantindo que a API esteja bem documentada e que os endpoints protegidos possam ser testados de forma eficiente.