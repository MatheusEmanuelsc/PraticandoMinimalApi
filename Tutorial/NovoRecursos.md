### Resumo: Novos Recursos de *Minimal APIs* no .NET 7 e .NET 8

Neste resumo, vamos abordar os novos recursos disponíveis nas *Minimal APIs* introduzidos no .NET 7 e as melhorias no .NET 8. Veremos o uso de **filtros**, o suporte para **upload de arquivos**, o recurso de **array binding**, e discutiremos o uso de **atributos**.

### Índice

1. [Introdução às Minimal APIs](#introdução)
2. [Uso de Filtros](#uso-de-filtros)
3. [Upload de Arquivos](#upload-de-arquivos)
4. [Array Binding](#array-binding)
5. [Atributos](#atributos)
6. [Conclusão](#conclusão)

---

### 1. Introdução às Minimal APIs

As *Minimal APIs* foram introduzidas no .NET 6 para criar endpoints HTTP de forma mais simples e concisa, eliminando a necessidade de controladores e outras camadas adicionais. Essas APIs são ideais para cenários como **microserviços**, **APIs simples** ou **projetos menores** que não precisam da complexidade do ASP.NET Core MVC.

Com o lançamento do .NET 7 e .NET 8, novas funcionalidades e melhorias foram adicionadas para facilitar ainda mais o desenvolvimento de aplicações, incluindo novos recursos para trabalhar com filtros, manipulação de arquivos, vinculação de arrays e uso de atributos.

---

### 2. Uso de Filtros

No .NET 7, foi introduzido o suporte para **filtros de endpoint** em *Minimal APIs*. Filtros permitem adicionar **lógica reutilizável** antes ou depois da execução dos endpoints, facilitando a implementação de funcionalidades como validação, autenticação ou manipulação de respostas.

#### Exemplo de Filtro em Minimal API (.NET 7):

```csharp
app.MapPost("/produtos", (Produto produto) => 
{
    // Lógica para criar produto
})
.AddEndpointFilter(async (context, next) => 
{
    var produto = context.GetArgument<Produto>(0);
    
    if (string.IsNullOrEmpty(produto.Nome))
    {
        return Results.BadRequest("Nome do produto é obrigatório.");
    }

    return await next(context); // Próximo filtro ou endpoint
});
```

No exemplo acima, o filtro é usado para verificar se o nome do produto foi fornecido antes de processar a requisição.

**Novidade no .NET 8:** O .NET 8 aprimorou a **ordenação de filtros**, permitindo a criação de uma cadeia de execução mais clara entre filtros e middlewares.

---

### 3. Upload de Arquivos

Outro recurso significativo introduzido no .NET 7 foi o suporte para **upload de arquivos** diretamente nas *Minimal APIs*. A nova funcionalidade permite aceitar arquivos como parte da requisição, tornando o processo de manipulação de arquivos mais fácil.

#### Exemplo de Upload de Arquivo em Minimal API:

```csharp
app.MapPost("/upload", async (IFormFile arquivo) =>
{
    if (arquivo.Length > 0)
    {
        var filePath = Path.Combine("uploads", arquivo.FileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await arquivo.CopyToAsync(stream);
        }

        return Results.Ok("Arquivo carregado com sucesso.");
    }
    return Results.BadRequest("Nenhum arquivo foi enviado.");
});
```

Neste exemplo, a API recebe um arquivo de um formulário e o salva em um diretório local. O uso de `IFormFile` facilita a manipulação de arquivos enviados via requisições HTTP POST.

---

### 4. Array Binding

O **array binding** foi aprimorado no .NET 7 e 8, permitindo o mapeamento direto de arrays passados como parâmetros em uma URL de forma mais natural.

#### Exemplo de Array Binding:

```csharp
app.MapGet("/produtos", (int[] ids) =>
{
    // Suponha que recuperamos produtos a partir dos IDs
    var produtos = RecuperarProdutos(ids);
    return Results.Ok(produtos);
});
```

No exemplo acima, ao chamar a API com a URL `/produtos?ids=1&ids=2&ids=3`, o array de `ids` será automaticamente vinculado ao parâmetro `int[] ids`. Isso simplifica o recebimento de múltiplos parâmetros de consulta, como uma lista de IDs.

---

### 5. Atributos

O suporte a atributos foi melhorado no .NET 8 para *Minimal APIs*, facilitando o uso de **validações**, **autenticação** e **autorização** diretamente nos endpoints.

#### Exemplo de Atributo com Validação:

```csharp
app.MapPost("/usuario", ([Required] Usuario usuario) =>
{
    if (!ModelState.IsValid)
    {
        return Results.BadRequest(ModelState);
    }
    
    // Lógica para criar usuário
    return Results.Ok("Usuário criado com sucesso.");
});
```

O uso de atributos como `[Required]` permite adicionar validações diretamente no modelo de dados, garantindo que os parâmetros recebidos estejam de acordo com as regras definidas.

**Novidade no .NET 8:** Os atributos agora podem ser utilizados em conjunto com **filtros** e **validações personalizadas**, proporcionando uma maior flexibilidade para adicionar lógica de controle e validação nos endpoints de forma declarativa.

---

### 6. Conclusão

As *Minimal APIs* no .NET 7 e .NET 8 oferecem um conjunto de funcionalidades robusto para o desenvolvimento de APIs de forma mais ágil e simplificada. Com o suporte para **filtros**, **upload de arquivos**, **array binding** e melhorias no uso de **atributos**, os desenvolvedores têm à disposição ferramentas poderosas para criar APIs eficientes, reutilizáveis e seguras.

Esses novos recursos tornam as *Minimal APIs* uma excelente escolha para projetos de diversos tamanhos, oferecendo flexibilidade e simplicidade sem sacrificar funcionalidades avançadas.