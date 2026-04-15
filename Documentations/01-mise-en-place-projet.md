# Construire une API ASP.NET Core en couches : marche à suivre

L’idée n’est pas de coder “au hasard”, mais d’avancer dans un ordre logique.

## Vue d’ensemble

Quand on débute, le plus simple est de suivre cette logique :

1. **Créer l’architecture du projet**
2. **Créer les modèles métier à partir de la base de données**
3. **Créer l’accès aux données (DAL / Repository)**
4. **Créer la logique métier (BLL / Service)**
5. **Créer les endpoints API (Controllers)**
6. **Configurer l’application**
7. **Créer la base SQL**
8. **Lancer et tester**

En résumé, on part de la structure, puis des données, puis de la logique, puis de l’API.

---

# 1. Commencer par l’architecture

Avant d’écrire du code métier, on prépare les dossiers pour séparer les responsabilités.

```text
├── API/
│   ├── Program.cs
│   └── appsettings.json
├── Domain/
├── DAL/
├── BLL/
```

## À quoi servent ces dossiers ?

* **Domain** : contient les modèles métier, donc les classes qui représentent les données
* **DAL** (*Data Access Layer*) : contient le code d’accès à la base de données
* **BLL** (*Business Logic Layer*) : contient la logique métier
* **Controllers** : contient les endpoints HTTP de l’API

---

# 2. Créer les modèles à partir du schéma de base de données

Ensuite, on regarde la base SQL et on crée les classes C# qui représentent les tables.

Par exemple, si on a une table `Products` avec :

* `Id`
* `Name`
* `Price`

alors on crée le modèle :

```text
Domain/Product.cs
```

```csharp
namespace MyApi.Domain
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }
}
```

## Pourquoi commencer par là ?

Parce que ce modèle sera utilisé partout :

* dans le repository
* dans le service
* dans le controller

Autrement dit, **le modèle est la base du reste**.

---

# 3. Créer les contrats d’abord : interfaces repository et service

Avant d’écrire les classes concrètes, on définit ce qu’elles devront faire.

## Interface du repository

Le repository s’occupe de parler à la base de données.

```text
DAL/IProductRepository.cs
```

```csharp
using MyApi.Domain;

namespace MyApi.DAL
{
    public interface IProductRepository
    {
        List<Product> GetAll();
        Product? GetById(int id);
    }
}
```

## Interface du service

Le service s’occupe de la logique métier.

```text
BLL/IProductService.cs
```

```csharp
using MyApi.Domain;

namespace MyApi.BLL
{
    public interface IProductService
    {
        List<Product> GetAllProducts();
        Product? GetProductById(int id);
    }
}
```

## Pourquoi faire les interfaces avant les classes ?

Parce que ça force à réfléchir à la question :

**“De quoi mon application a besoin ?”**

Au lieu de partir directement dans l’implémentation, on définit d’abord le contrat.

C’est aussi utile pour :

* garder un code plus propre
* remplacer plus facilement une implémentation plus tard
* utiliser l’injection de dépendances

---

# 4. Implémenter le repository : accès aux données

Une fois l’interface prête, on crée la vraie classe qui interroge SQL Server avec ADO.NET.

```text
DAL/ProductRepository.cs
```

```csharp
using Microsoft.Data.SqlClient;
using MyApi.Domain;

namespace MyApi.DAL
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public List<Product> GetAll()
        {
            var products = new List<Product>();

            using SqlConnection connection = new SqlConnection(_connectionString);
            string query = "SELECT Id, Name, Price FROM Products";

            using SqlCommand command = new SqlCommand(query, connection);

            connection.Open();

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                var product = new Product
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString() ?? "",
                    Price = Convert.ToDecimal(reader["Price"])
                };

                products.Add(product);
            }

            return products;
        }

        public Product? GetById(int id)
        {
            Product? product = null;

            using SqlConnection connection = new SqlConnection(_connectionString);
            string query = "SELECT Id, Name, Price FROM Products WHERE Id = @Id";

            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            connection.Open();

            using SqlDataReader reader = command.ExecuteReader();

            if (reader.Read())
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
    }
}
```

## Rôle du repository

Le repository fait uniquement la partie “base de données” :

* ouvrir la connexion
* exécuter la requête SQL
* lire les résultats
* convertir les résultats en objets C#

Il ne doit pas gérer les règles métier ou les réponses HTTP.

---

# 5. Implémenter le service : logique métier

Le service se place entre le controller et le repository.

```text
BLL/ProductService.cs
```

```csharp
using MyApi.Domain;
using MyApi.DAL;

namespace MyApi.BLL
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public List<Product> GetAllProducts()
        {
            return _productRepository.GetAll();
        }

        public Product? GetProductById(int id)
        {
            return _productRepository.GetById(id);
        }
    }
}
```

## À quoi sert le service si pour l’instant il “ne fait rien” ?

Au début, il peut sembler inutile parce qu’il ne fait que relayer les appels vers le repository. Mais plus tard, c’est ici qu’on mettra :

* les validations
* les règles métier
* les contrôles avant insertion ou modification
* les calculs
* les traitements spécifiques

Donc même si aujourd’hui il est simple, il prépare une architecture propre pour la suite.

---

# 6. Créer le controller : exposer les routes HTTP

Le controller est la couche visible par le client.

```text
Controllers/ProductsController.cs
```

```csharp
using Microsoft.AspNetCore.Mvc;
using MyApi.BLL;
using MyApi.Domain;

namespace MyApi.Controllers
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
        public ActionResult<List<Product>> GetAll()
        {
            var products = _productService.GetAllProducts();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public ActionResult<Product> GetById(int id)
        {
            var product = _productService.GetProductById(id);

            if (product == null)
            {
                return NotFound("Produit non trouvé");
            }

            return Ok(product);
        }
    }
}
```

## Rôle du controller

Le controller :

* reçoit la requête HTTP
* appelle le service
* renvoie une réponse HTTP

Par exemple :

* `200 OK` si tout va bien
* `404 Not Found` si le produit n’existe pas

Le controller ne doit pas contenir de SQL.

---

# 7. Configurer l’application

Il faut maintenant enregistrer les dépendances et activer les composants nécessaires.

## `Program.cs`

```csharp
using MyApi.DAL;
using MyApi.BLL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
```

## Pourquoi enregistrer les services ?

Grâce à l’injection de dépendances, ASP.NET Core sait automatiquement :

* créer un `ProductRepository` quand on demande `IProductRepository`
* créer un `ProductService` quand on demande `IProductService`

C’est ce qui permet d’écrire des constructeurs propres dans les controllers et services.

---

# 8. Configurer la connexion SQL

L’application a besoin de savoir où se trouve la base de données.

## `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=bstorm;Database=asp-api-demo;Trusted_Connection=True;TrustServerCertificate=True;"  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Le repository lit cette valeur grâce à :

```csharp
configuration.GetConnectionString("DefaultConnection")
```

---

# 9. Nettoyer le template

Quand on crée une API ASP.NET Core, le template ajoute souvent des fichiers d’exemple inutiles pour notre projet.

À supprimer :

```text
Controllers/WeatherForecastController.cs
WeatherForecast.cs
```

---

# 10. Créer la base SQL

On prépare maintenant la base avec une table simple `Products`.

```sql
CREATE DATABASE MyDb;
GO

USE MyDb;
GO

CREATE TABLE Products
(
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL
);
GO

INSERT INTO Products (Name, Price)
VALUES ('Stylo', 2.50),
       ('Cahier', 5.90),
       ('Livre', 12.00);
GO
```

## Logique globale

On part souvent du schéma de base, puis on remonte :

* table SQL
* modèle C#
* repository
* service
* controller

C’est une bonne méthode pour apprendre.

---

# 11. Lancer le projet

Une fois le code prêt :

* démarrer SQL Server
* vérifier la chaîne de connexion
* lancer l’application
* ouvrir Swagger

Swagger sera accessible avec une URL du type :

```text
https://localhost:xxxx/swagger
```

---

# 12. Tester l’API

## Endpoints disponibles

```http
GET https://localhost:xxxx/api/products
GET https://localhost:xxxx/api/products/1
```

## Ce qui se passe quand on appelle l’API

L’ordre d’exécution est :

```text
Controller -> BLL -> DAL -> SQL Server
```

Exemple pour `GET /api/products/1` :

1. le client appelle l’URL
2. le `ProductsController` reçoit la requête
3. le controller appelle `ProductService`
4. le service appelle `ProductRepository`
5. le repository exécute la requête SQL
6. le résultat remonte jusqu’au controller
7. l’API renvoie la réponse JSON

---

# 13. Arborescence finale

```text
MyApi/
├── Controllers/
│   └── ProductsController.cs
├── Domain/
│   └── Product.cs
├── DAL/
│   ├── IProductRepository.cs
│   └── ProductRepository.cs
├── BLL/
│   ├── IProductService.cs
│   └── ProductService.cs
├── appsettings.json
├── Program.cs
└── MyApi.csproj
```

---

# Méthode de travail conseillée

## Étape 1 : préparer l’architecture

Créer les dossiers ou projets :

* `Domain`
* `DAL`
* `BLL`
* `Controllers`

Le but est d’avoir une structure claire dès le départ.

## Étape 2 : partir du schéma de base de données

On regarde les tables SQL et on crée les classes du dossier `Domain`.

Exemple :

* table `Products` → classe `Product`

## Étape 3 : créer les repositories

On crée d’abord les interfaces, puis les classes qui accèdent à la base.

Le repository sait lire ou écrire les données.

## Étape 4 : créer les services

On ajoute la logique métier entre l’API et la base.

Même si le service est simple au début, il sert à garder un bon découpage.

## Étape 5 : travailler par feature

Une fois l’architecture posée, on avance **fonctionnalité par fonctionnalité**.

Exemple pour la feature `Product` :

* modèle `Product`
* interface repository `IProductRepository`
* repository `ProductRepository`
* interface service `IProductService`
* service `ProductService`
* controller `ProductsController`

Puis on passe à la feature suivante :

* `Category`
* `Order`
* `Customer`


---
## Ordre conseillé

1. créer l’architecture
2. créer les modèles depuis la base
3. créer les interfaces
4. créer les repositories
5. créer les services
6. créer les controllers
7. configurer `Program.cs`
8. tester avec Swagger

---