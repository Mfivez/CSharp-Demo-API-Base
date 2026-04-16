---

# Cours — La gestion des erreurs dans ton API ASP.NET Core (.NET 9)

Jusqu’ici, ton API :

* reçoit des requêtes
* valide les données
* appelle le service
* accède à la base

Il est maintenant temps d’ajouter quelque chose d’essentiel :

**la gestion globale des erreurs**

---

# 1. Le problème actuel

Dans une API, une erreur peut arriver à plusieurs endroits.

Exemple dans le repository :

```csharp
await using SqlConnection connection = new SqlConnection(_connectionString);
await connection.OpenAsync(); // erreur possible
```

Ou dans le service :

```csharp
if (product == null)
{
    throw new ArgumentException("Produit introuvable");
}
```

Sans gestion d’erreur centralisée, l’exception remonte telle quelle.

Résultat possible :

* réponse incohérente
* erreur technique brute
* comportement différent selon l’environnement

ASP.NET Core distingue justement le développement et la production pour la gestion des erreurs. En développement, on veut surtout voir le détail technique. En production, on veut une réponse propre et contrôlée. ([Microsoft Learn][1])

---

# 2. Objectif de la gestion des erreurs

Le but est simple :

**toujours renvoyer une réponse claire, cohérente et maîtrisée**

Aujourd’hui, le format standard recommandé pour une API HTTP est **Problem Details**.

ASP.NET Core sait produire ce format avec `AddProblemDetails()`. ([Microsoft Learn][2])

Exemple :

```json
{
  "type": "about:blank",
  "title": "Une erreur est survenue",
  "status": 500
}
```

---

# 3. Première idée : mettre des try/catch dans les controllers

On pourrait faire ça :

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<ProductResponse>> GetById(int id)
{
    try
    {
        var product = await _productService.GetProductByIdAsync(id);

        return Ok(new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price
        });
    }
    catch (Exception)
    {
        return StatusCode(500, "Erreur serveur");
    }
}
```

---

## Pourquoi ce n’est pas une bonne solution

Le problème, c’est que cette approche devient vite :

* répétitive
* lourde
* difficile à maintenir

Chaque action finit par gérer les erreurs de son côté.

Le controller devient plus chargé alors que son rôle devrait rester simple :

* recevoir la requête
* appeler le service
* renvoyer la réponse

---

# 4. L’approche moderne en ASP.NET Core

Aujourd’hui, l’approche recommandée repose sur 3 éléments :

* `UseExceptionHandler()`
* `IExceptionHandler`
* `AddProblemDetails()`

Le principe est le suivant :

* `UseExceptionHandler()` active le middleware global d’exception
* `IExceptionHandler` contient la logique personnalisée de traitement
* `AddProblemDetails()` permet de renvoyer un format d’erreur standardisé

Microsoft documente précisément cette approche pour les APIs ASP.NET Core. ([Microsoft Learn][1])

---

# 5. Schéma mental

```text
Client → Middleware → Controller → Service → Repository
                 ↑
        UseExceptionHandler()
                 ↑
        GlobalExceptionHandler
                 ↑
       Réponse ProblemDetails
```

Si une exception non gérée remonte depuis le repository, le service ou le controller :

* le middleware `UseExceptionHandler()` l’intercepte
* il appelle ton `IExceptionHandler`
* ton handler fabrique une réponse HTTP propre

Si `TryHandleAsync()` retourne `true`, cela signifie que l’exception a été gérée. ([Microsoft Learn][1])

---

# 6. Ce qu’on va faire dans ton projet

Dans ton projet, on va mettre en place :

* un handler global d’exception
* une configuration propre dans `Program.cs`
* des exceptions métier dans la couche service
* des controllers sans `try/catch` inutiles
* des réponses d’erreur cohérentes en JSON

---

# 7. Enregistrer les services

## Program.cs

Dans ton projet, il faut enregistrer :

* les controllers
* `ProblemDetails`
* le handler global


```csharp
using DAL.Interfaces;
using DAL.Repositories;
using BLL.Interfaces;
using BLL.Services;
using Démo_simple_API.MiddleWares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler();
}

app.UseStatusCodePages();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Pourquoi comme ça ?

`AddExceptionHandler<GlobalExceptionHandler>()` enregistre ton handler.

`UseExceptionHandler()` active le middleware officiel de gestion des exceptions.

`AddProblemDetails()` prépare la production de réponses d’erreur au format Problem Details. ([Microsoft Learn][1])

### Point important

La doc Microsoft montre généralement `UseExceptionHandler()` **hors développement**.

Pourquoi ?

Parce qu’en développement, on préfère souvent la page d’erreur de développement pour voir le détail technique. En production, on remplace ça par une réponse maîtrisée. ([Microsoft Learn][1])

### Variante pratique

Si tu veux **tester directement ton JSON d’erreur même en développement**, tu peux aussi écrire :

```csharp
app.UseExceptionHandler();
```

en dehors du `if`.

Ce n’est pas absurde pour une API pendant les tests, mais la version la plus “académique” reste celle de la doc : **handler surtout hors développement**.

---

# 8. Créer le handler global

## GlobalExceptionHandler.cs

Voici une version propre pour ton projet :

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Démo_simple_API.MiddleWares
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Une exception non gérée est survenue.");

            var problemDetails = new ProblemDetails();

            switch (exception)
            {
                case ArgumentException:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = exception.Message;
                    break;

                case KeyNotFoundException:
                    problemDetails.Status = StatusCodes.Status404NotFound;
                    problemDetails.Title = exception.Message;
                    break;

                default:
                    problemDetails.Status = StatusCodes.Status500InternalServerError;
                    problemDetails.Title = "Une erreur interne est survenue";
                    break;
            }

            httpContext.Response.StatusCode = problemDetails.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
```

---

## Explication

`IExceptionHandler` expose une seule méthode :

```csharp
TryHandleAsync(HttpContext, Exception, CancellationToken)
```

Cette méthode reçoit :

* le contexte HTTP
* l’exception interceptée
* le token d’annulation

Si elle retourne `true`, l’exception est considérée comme traitée. ([Microsoft Learn][1])

---

# 9. Où lancer les exceptions métier ?

Dans le **service**.

C’est là qu’on exprime les règles métier.

Exemple dans ton `ProductService` :

```csharp
public async Task<Product> GetProductByIdAsync(int id)
{
    Product? product = await _productRepository.GetByIdAsync(id);

    if (product == null)
    {
        throw new KeyNotFoundException("Produit introuvable");
    }

    return product;
}
```

Pourquoi `KeyNotFoundException` ici ?

Parce que si un produit n’existe pas, le code HTTP logique est plutôt :

* **404 Not Found**

que :

* **400 Bad Request**

Le handler global transforme ensuite cette exception en réponse HTTP propre.

---

# 10. Ce qui change dans le controller

Presque rien.

Le controller reste simple :

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<ProductResponse>> GetById(int id)
{
    var product = await _productService.GetProductByIdAsync(id);

    var response = new ProductResponse
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price
    };

    return Ok(response);
}
```

Pas besoin de `try/catch` partout.

La gestion des erreurs est centralisée.

---

# 12. Gérer aussi les opérations UPDATE et DELETE sur des éléments absents

Dans ton code d’origine :

* `UpdateAsync` exécute une requête SQL
* `DeleteAsync` exécute une requête SQL

Mais si aucun produit n’existe avec cet `Id`, SQL ne lève pas forcément d’exception.

La commande s’exécute simplement avec **0 ligne affectée**.

Donc il faut le gérer.

---

## Repository corrigé

```csharp
using DAL.Interfaces;
using Microsoft.Data.SqlClient;
using Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace DAL.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<List<Product>> GetAllAsync()
        {
            var products = new List<Product>();

            await using SqlConnection connection = new SqlConnection(_connectionString);
            const string query = "SELECT Id, Name, Price FROM Products";

            await using SqlCommand command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(new Product
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString() ?? "",
                    Price = Convert.ToDecimal(reader["Price"])
                });
            }

            return products;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            Product? product = null;

            await using SqlConnection connection = new SqlConnection(_connectionString);
            const string query = "SELECT Id, Name, Price FROM Products WHERE Id = @Id";

            await using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                product = new Product
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString() ?? "",
                    Price = Convert.ToDecimal(reader["Price"])
                };
            }

            return product;
        }

        public async Task<int> AddAsync(Product product)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);

            const string query = """
                INSERT INTO Products (Name, Price)
                OUTPUT INSERTED.Id
                VALUES (@Name, @Price)
                """;

            await using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@Price", product.Price);

            await connection.OpenAsync();
            return (int)await command.ExecuteScalarAsync();
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);

            const string query = "UPDATE Products SET Name = @Name, Price = @Price WHERE Id = @Id";

            await using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@Id", product.Id);
            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@Price", product.Price);

            await connection.OpenAsync();
            int rowsAffected = await command.ExecuteNonQueryAsync();

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);

            const string query = "DELETE FROM Products WHERE Id = @Id";

            await using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            int rowsAffected = await command.ExecuteNonQueryAsync();

            return rowsAffected > 0;
        }
    }
}
```

---

# 13. Il faut aussi adapter l’interface du repository

## IProductRepository.cs

```csharp
using Domain.Entities;

namespace DAL.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<int> AddAsync(Product product);
        Task<bool> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
    }
}
```

---

# 14. Adapter le service pour lancer les bonnes exceptions

## ProductService.cs

```csharp
using BLL.Interfaces;
using DAL.Interfaces;
using Domain.Entities;

namespace BLL.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _productRepository.GetAllAsync();
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            Product? product = await _productRepository.GetByIdAsync(id);

            if (product == null)
            {
                throw new KeyNotFoundException("Produit introuvable");
            }

            return product;
        }

        public async Task<int> CreateProductAsync(Product product)
        {
            return await _productRepository.AddAsync(product);
        }

        public async Task UpdateProductAsync(Product product)
        {
            bool updated = await _productRepository.UpdateAsync(product);

            if (!updated)
            {
                throw new KeyNotFoundException("Produit introuvable");
            }
        }

        public async Task DeleteProductAsync(int id)
        {
            bool deleted = await _productRepository.DeleteAsync(id);

            if (!deleted)
            {
                throw new KeyNotFoundException("Produit introuvable");
            }
        }
    }
}
```

---

# 15. Adapter aussi l’interface du service

## IProductService.cs

```csharp
using Domain.Entities;

namespace BLL.Interfaces
{
    public interface IProductService
    {
        Task<List<Product>> GetAllProductsAsync();
        Task<Product> GetProductByIdAsync(int id);
        Task<int> CreateProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);
    }
}
```

---

# 16. Corriger le controller

## ProductsController.cs

```csharp
using BLL.Interfaces;
using Démo_simple_API.DTO.Product;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Démo_simple_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ProductResponse>>> GetAll()
        {
            var products = await _productService.GetAllProductsAsync();

            var response = products.Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price
            }).ToList();

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponse>> GetById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            var response = new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price
            };

            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<ProductResponse>> Create(ProductCreateRequest request)
        {
            var product = new Product
            {
                Name = request.Name,
                Price = request.Price
            };

            int newId = await _productService.CreateProductAsync(product);

            var response = new ProductResponse
            {
                Id = newId,
                Name = product.Name,
                Price = product.Price
            };

            return CreatedAtAction(nameof(GetById), new { id = newId }, response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, ProductUpdateRequest request)
        {
            if (id != request.Id)
            {
                throw new ArgumentException("L'id de l'URL ne correspond pas à l'id du body.");
            }

            var product = new Product
            {
                Id = request.Id,
                Name = request.Name,
                Price = request.Price
            };

            await _productService.UpdateProductAsync(product);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _productService.DeleteProductAsync(id);
            return NoContent();
        }
    }
}
```

---

# 18. Pourquoi on a corrigé UPDATE et DELETE

Dans SQL Server :

* `UPDATE ... WHERE Id = @Id`
* `DELETE ... WHERE Id = @Id`

ne lèvent pas forcément une exception si l’ID n’existe pas.

Ils renvoient simplement **0 ligne affectée**.

Donc si on ne teste pas ce résultat :

* l’API peut répondre `204 No Content`
* alors qu’aucun produit n’a été modifié ou supprimé

On a donc fait remonter un `bool` depuis le repository, puis le service transforme ça en :

```csharp
throw new KeyNotFoundException("Produit introuvable");
```

Le handler global convertit ensuite cela en **404 Not Found**.

---

# 19. Résultat côté client

## Cas 1 — produit introuvable

Si on appelle :

```http
GET /api/products/999
```

Réponse :

```json
{
  "title": "Produit introuvable",
  "status": 404
}
```

## Cas 2 — mauvaise requête

Si l’id de l’URL ne correspond pas à l’id du body :

```json
{
  "title": "L'id de l'URL ne correspond pas à l'id du body.",
  "status": 400
}
```

## Cas 3 — erreur technique interne

Si une erreur SQL ou une autre exception non prévue se produit :

```json
{
  "title": "Une erreur interne est survenue",
  "status": 500
}
```

---

# 20. Bonus : erreurs 404 sans exception

Le handler global s’occupe des **exceptions non gérées**.

Mais il y a aussi les statuts HTTP qui ne viennent pas d’une exception, par exemple :

* route inexistante
* 404 sans corps
* 405 etc.

Pour cela, `UseStatusCodePages()` peut produire une réponse exploitable, et `AddProblemDetails()` s’intègre justement à cette logique dans les APIs ASP.NET Core. ([Microsoft Learn][2])

Donc dans ton pipeline :

---

# 21. Pourquoi cette approche est meilleure qu’un middleware maison

Avant, on écrivait souvent un middleware personnalisé du genre :

```csharp
app.UseMiddleware<ExceptionMiddleware>();
```

Mais aujourd’hui, ASP.NET Core fournit déjà le mécanisme officiel :

* `UseExceptionHandler()`
* `IExceptionHandler`
* `ProblemDetails`

Donc :

* moins de code maison
* meilleure intégration au framework
* comportement plus standard
* maintenance plus simple

Microsoft documente précisément cette approche et le rôle central de `IExceptionHandler`. ([Microsoft Learn][1])

---

# 23. Schéma mental final

```text
Repository  -> accès données
Service     -> règles métier + exceptions métier
Controller  -> requête/réponse
Handler     -> transforme les exceptions en réponse HTTP
```

Et côté pipeline :

```text
UseExceptionHandler() = middleware global officiel
IExceptionHandler     = logique personnalisée
AddProblemDetails()   = format standard d’erreur
UseStatusCodePages()  = corps propre pour certains statuts HTTP
```

---

# 24. Conclusion

Avec cette approche, ton API devient beaucoup plus propre.

Tu ne gères plus les erreurs dans chaque controller.

Tu fais plutôt ceci :

* le repository exécute
* le service décide si c’est une erreur métier
* le handler transforme l’exception en réponse HTTP propre

En clair :

**on centralise la gestion des erreurs**

et on s’appuie sur les outils natifs d’ASP.NET Core :

* `UseExceptionHandler()`
* `IExceptionHandler`
* `AddProblemDetails()`
