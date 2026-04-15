# Cours — Les DTOs

Jusqu’ici, dans la feature `Product`, on a travaillé avec cette logique :

* le controller reçoit un `Product`
* le service manipule un `Product`
* le repository manipule un `Product`
* le controller renvoie aussi un `Product`

Au début, c’est pratique pour apprendre, parce que tout est simple.
Mais dans un vrai projet, cette approche pose vite des problèmes.

L’objectif de ce cours est donc de comprendre :

* ce qu’est un DTO
* pourquoi on en a besoin
* à quel endroit on l’utilise
* comment l’introduire proprement dans l’architecture existante
* comment distinguer les DTO de requête et les DTO de réponse

---

# 1. Le problème de départ

Imaginons qu’on ait cette classe métier :

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

Et qu’on fasse dans le controller :

```csharp
[HttpPost]
public ActionResult Create(Product product)
{
    _productService.CreateProduct(product);
    return Ok();
}
```

Ici, le client envoie directement un objet `Product`.

Cela veut dire que :

* l’extérieur connaît notre modèle interne
* l’extérieur peut envoyer toutes les propriétés du modèle
* notre API dépend directement de la structure de notre couche `Domain`

Au début, ça peut sembler normal. Mais en réalité, on mélange deux choses différentes :

* **le modèle métier interne**
* **le format de données échangé avec le client**

Et ces deux choses ne devraient pas être confondues pour plein de raisons, dont des raisons de sécurité.

---

# 2. Qu’est-ce qu’un DTO ?

DTO veut dire :

**Data Transfer Object**

En français, on peut le comprendre comme :

**objet de transfert de données**

Son rôle est simple :

un DTO sert à **transporter des données entre le client et l’API**.

Il ne représente pas forcément le métier.
Il représente surtout **ce qu’on veut recevoir** ou **ce qu’on veut envoyer**.

Autrement dit :

* le **Domain** représente le fonctionnement interne de l’application
* le **DTO** représente le contrat d’échange avec l’extérieur

---

# 3. Pourquoi ne pas utiliser directement les modèles du Domain ?

C’est la question la plus importante.

## Si on utilise directement `Product`

on dit implicitement :

> “Mon modèle métier interne est exactement identique à ce que j’accepte et renvoie au client.”

Or, ce n’est pas toujours vrai.

Avec le temps, on peut vouloir :

* cacher certaines propriétés
* empêcher le client d’envoyer certains champs
* renvoyer plus ou moins d’informations selon le cas
* faire évoluer la base ou le modèle interne sans casser l’API

Donc, utiliser directement le `Domain` dans les controllers crée un **couplage fort** entre :

* l’intérieur de l’application
* l’extérieur de l’application

Et ce couplage est une mauvaise chose.

---

# 4. Exemple concret du problème

Prenons `Product`.

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}
```

Si on utilise ce modèle directement dans un `POST`, le client peut envoyer :

```json
{
  "id": 999,
  "name": "Stylo",
  "price": 2.50
}
```

Mais pour créer un produit, est-ce que le client doit fournir l’`Id` ?
Non.

En général, c’est la base de données qui le génère.

Donc ici, le modèle `Product` contient une propriété utile pour l’interne, mais pas adaptée à l’entrée HTTP.

C’est précisément pour ça qu’on crée un DTO.

---

# 5. L’idée générale des DTO

Au lieu d’utiliser `Product` partout, on sépare les rôles.

On garde :

* `Product` pour l’intérieur de l’application
* des DTO pour les échanges HTTP

On aura donc par exemple :

* `ProductCreateRequest`
* `ProductUpdateRequest`
* `ProductResponse`

Cela permet de dire clairement :

* **ce que le client peut envoyer**
* **ce que l’API renvoie**
* **ce que l’application manipule en interne**

---

# 6. Différence entre modèle métier et DTO

## Le modèle métier

Le modèle métier appartient à la couche `Domain`.

Exemple :

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

Il sert à représenter un produit dans l’application.

On l’utilise dans :

* le repository
* le service
* la logique interne

## Le DTO

Le DTO sert uniquement pour la communication entre le client et l’API.

On l’utilise surtout dans :

* les paramètres des actions du controller
* les valeurs renvoyées par les actions du controller

Le DTO n’a pas pour but de représenter “tout le métier”.
Il a pour but de représenter **la forme des données échangées**.

---

# 7. Deux grandes familles de DTO

En pratique, on distingue souvent deux types :

## Les DTO de requête

Ils représentent ce que le client envoie à l’API.

Exemples :

* `ProductCreateRequest`
* `ProductUpdateRequest`

## Les DTO de réponse

Ils représentent ce que l’API renvoie au client.

Exemple :

* `ProductResponse`

Cette séparation est importante, parce que ce qu’on reçoit et ce qu’on renvoie ne sont pas toujours identiques.

---

# 8. Pourquoi faire plusieurs DTO et pas un seul ?

Quand on commence on peut se dire :

> “Pourquoi ne pas faire un seul `ProductDto` pour tout ?”

Parce que les besoins ne sont pas les mêmes selon l’action.

## Pour créer

Le client n’a pas besoin d’envoyer `Id`.

## Pour modifier

Le produit existe déjà, donc on a besoin de savoir lequel modifier.

## Pour répondre

On veut souvent renvoyer l’`Id`, et parfois d’autres données.

Donc chaque opération a son propre besoin.

C’est pour cela qu’on fait souvent plusieurs DTO spécialisés.

---

# 9. Exemple de DTO de création

Quand le client crée un produit, il doit seulement fournir les données nécessaires à la création.

```csharp
namespace MyApi.DTO.Product
{
    public class ProductCreateRequest
    {
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }
}
```

Pourquoi il n’y a pas `Id` ?

Parce que lors d’une création :

* le client ne choisit généralement pas l’identifiant
* c’est la base de données qui le génère

Donc on enlève volontairement `Id`.

---

# 10. Exemple de DTO de mise à jour

Pour une modification, on veut décrire les données modifiables.

```csharp
namespace MyApi.DTO.Product
{
    public class ProductUpdateRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }
}
```

Ici, on garde `Id`, car il faut savoir quel produit on modifie.

Même si l’`id` vient aussi souvent dans l’URL, ce DTO permet de structurer clairement la donnée reçue.

---

# 11. Exemple de DTO de réponse

Quand l’API renvoie un produit au client, elle ne renvoie pas forcément l’objet métier brut.

Elle renvoie un DTO pensé pour la sortie.

```csharp
namespace MyApi.DTO.Product
{
    public class ProductResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }
}
```

Pour l’instant, il ressemble au modèle `Product`.

Et c’est normal.

Au début, un DTO peut être très proche du modèle métier.
Mais l’important, ce n’est pas qu’il soit différent tout de suite.
L’important, c’est qu’il soit **séparé**.

Cette séparation protège l’architecture pour la suite.

---

# 12. Où placer les DTO dans le projet ?

Pour rester propre, on peut créer un dossier dédié.

Exemple :

```text
MyApi/
API/
├── Controllers/
├── DTO/
│   └── Product/
│       ├── ProductCreateRequest.cs
│       ├── ProductUpdateRequest.cs
│       └── ProductResponse.cs
├── Domain/
├── DAL/
├── BLL/
├── Program.cs
└── appsettings.json
```

Pourquoi faire un dossier `DTO` ?

Parce qu’un DTO n’est ni :

* un modèle métier
* un repository
* un service
* un controller

C’est une catégorie à part entière.

---

# 13. À quel niveau les DTO sont utilisés ?

C’est très important pour comprendre l’architecture.

## Dans le repository ?

Non.

Le repository travaille avec les modèles métier du `Domain`.

## Dans le service ?

Dans une architecture simple comme celle du cours, on peut décider que le service continue à manipuler des modèles métier pour le moment.

## Dans l'api ?

Oui, principalement.

Le controller fait le lien entre :

* le monde HTTP
* le monde métier

C’est donc lui qui reçoit des DTO, puis les transforme en objets métier.
Et c’est aussi lui qui prend les objets métier pour les transformer en DTO de réponse.

---

# 14. La notion de mapping

Une fois qu’on introduit des DTO, il faut faire une conversion entre :

* DTO et modèle métier
* modèle métier et DTO

Cette conversion s’appelle souvent :

**le mapping**

Par exemple :

* `ProductCreateRequest` → `Product`
* `Product` → `ProductResponse`

Au début, ce mapping se fait “à la main”, propriété par propriété.

---

# 15. Exemple simple de mapping en entrée

Imaginons ce DTO :

```csharp
public class ProductCreateRequest
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}
```

Dans le controller, on reçoit ce DTO, puis on crée un objet métier :

```csharp
var product = new Product
{
    Name = request.Name,
    Price = request.Price
};
```

Ici, on convertit une donnée reçue depuis HTTP vers un objet utilisé par la logique métier.

---

# 16. Exemple simple de mapping en sortie

Le service renvoie un `Product` :

```csharp
var product = _productService.GetProductById(id);
```

Mais le controller ne renvoie pas directement cet objet.
Il le transforme en DTO de réponse :

```csharp
var response = new ProductResponse
{
    Id = product.Id,
    Name = product.Name,
    Price = product.Price
};
```

Puis il renvoie :

```csharp
return Ok(response);
```

---

# 17. Schéma global du fonctionnement

Sans DTO, on avait ceci :

```text
Client HTTP → Controller → Service → Repository → SQL
                 ↑
               Product
```

Avec DTO, on obtient :

```text
Client HTTP → Controller → Service → Repository → SQL
                 ↓
          DTO Request → Product
                 ↑
         Product → DTO Response
```

Le controller devient donc l’endroit où on adapte les données entre l’extérieur et l’intérieur.

---

# 18. Pourquoi cette séparation est une bonne pratique ?

Elle apporte plusieurs avantages.

## 1. On protège le modèle interne

Le client ne dépend plus directement du `Domain`.

## 2. On contrôle ce que le client peut envoyer

On choisit exactement les propriétés autorisées.

## 3. On contrôle ce que l’API renvoie

On choisit exactement les données exposées.

## 4. On facilite l’évolution de l’application

On peut modifier le `Domain` sans forcément casser le contrat HTTP.

## 5. On prépare la validation

Les DTO sont un excellent endroit pour ajouter plus tard, la validation :

* `[Required]`
* `[StringLength]`
* `[Range]`

## 6. On rend l’intention plus claire

Un nom comme `ProductCreateRequest` est beaucoup plus parlant que `Product`.

---

# 19. Exemple réel de bénéfice

Supposons qu’un jour on ajoute dans `Product` une propriété interne :

```csharp
public DateTime InternalLastUpdate { get; set; }
```

Cette propriété peut être utile pour l’application, mais on ne veut peut-être pas :

* que le client l’envoie
* que le client la voie

Si on expose directement `Product`, cette propriété risque d’apparaître dans l’API.

Avec les DTO, ce problème n’existe pas, car on choisit explicitement ce qui entre et ce qui sort.

---

# 20. Ce qu’on ne change pas encore

Pour rester cohérent avec le niveau actuel, on garde les choses simples.

On ne fait pas encore :

* validation avancée
* AutoMapper
* logique de mapping centralisée
* architecture plus complexe
* DTO dans toutes les couches

Pour l’instant, l’objectif est juste de comprendre la séparation suivante :

* `Domain` pour l’interne
* `DTO` pour l’externe

Et de faire le mapping manuellement dans le controller.

---

# 21. Mise en place concrète dans la feature `Product`

On va maintenant appliquer cela à la feature `Product`.

La progression logique reste la même :

1. créer les DTO
2. modifier le controller
3. garder le service tel quel
4. garder le repository tel quel
5. tester dans Swagger

Pourquoi ne change-t-on pas le repository et le service ?

Parce que les DTO concernent surtout la communication HTTP.
Le repository et le service peuvent continuer à travailler avec les objets métier.

---

# 22. Créer les DTO de la feature `Product`

## `DTO/Product/ProductCreateRequest.cs`

```csharp
namespace MyApi.DTO.Product
{
    public class ProductCreateRequest
    {
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }
}
```

## `DTO/Product/ProductUpdateRequest.cs`

```csharp
namespace MyApi.DTO.Product
{
    public class ProductUpdateRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }
}
```

## `DTO/Product/ProductResponse.cs`

```csharp
namespace MyApi.DTO.Product
{
    public class ProductResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }
}
```

---

# 23. Modifier le controller pour utiliser les DTO

Le controller devient l’endroit où on fait l’adaptation.

## Avant

```csharp
[HttpPost]
public ActionResult Create(Product product)
{
    _productService.CreateProduct(product);
    return Ok();
}
```

## Après

```csharp
[HttpPost]
public ActionResult Create(ProductCreateRequest request)
{
    var product = new Product
    {
        Name = request.Name,
        Price = request.Price
    };

    _productService.CreateProduct(product);

    return Ok();
}
```

Ici, on reçoit un DTO, puis on fabrique l’objet métier.

---

# 24. Exemple pour le POST

```csharp
using Microsoft.AspNetCore.Mvc;
using MyApi.BLL;
using MyApi.Domain;
using MyApi.DTO.Product;

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

        [HttpPost]
        public ActionResult Create(ProductCreateRequest request)
        {
            var product = new Product
            {
                Name = request.Name,
                Price = request.Price
            };

            _productService.CreateProduct(product);

            return Ok();
        }
    }
}
```

---

# 25. Exemple pour le PUT

```csharp
[HttpPut("{id}")]
public ActionResult Update(int id, ProductUpdateRequest request)
{
    var product = new Product
    {
        Id = request.Id,
        Name = request.Name,
        Price = request.Price
    };

    _productService.UpdateProduct(product);

    return Ok();
}
```

---

# 26. Exemple pour le GET all

Avant, on faisait souvent :

```csharp
[HttpGet]
public ActionResult<List<Product>> GetAll()
{
    var products = _productService.GetAllProducts();
    return Ok(products);
}
```

Maintenant, on transforme la liste en DTO de réponse :

```csharp
[HttpGet]
public ActionResult<List<ProductResponse>> GetAll()
{
    var products = _productService.GetAllProducts();

    var response = products.Select(p => new ProductResponse
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price
    }).ToList();

    return Ok(response);
}
```

---

# 27. Exemple complet pour le GET by id

```csharp
[HttpGet("{id}")]
public ActionResult<ProductResponse> GetById(int id)
{
    var product = _productService.GetProductById(id);

    if (product == null)
    {
        return NotFound("Produit non trouvé");
    }

    var response = new ProductResponse
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price
    };

    return Ok(response);
}
```

---

# 28. Ce qui ne change pas dans le service

Le service peut rester tel quel :

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

        public void CreateProduct(Product product)
        {
            _productRepository.Add(product);
        }

        public void UpdateProduct(Product product)
        {
            _productRepository.Update(product);
        }

        public void DeleteProduct(int id)
        {
            _productRepository.Delete(id);
        }
    }
}
```

Pourquoi ?

Parce que les DTO sont surtout utiles à la frontière HTTP.
Le service, lui, peut continuer à travailler avec les objets métier.

---

# 29. Ce qui ne change pas dans le repository

Le repository reste également identique, car il travaille avec :

* SQL
* `Product`

Il n’a pas besoin de connaître les DTO.

C’est important à comprendre :

**les DTO ne remplacent pas le Domain**
Ils complètent l’architecture au niveau des échanges HTTP.

---

# 30. Ce que voit le client maintenant

## Requête POST

Le client envoie :

```json
{
  "name": "Clavier",
  "price": 49.99
}
```

Il n’envoie plus `id`.

## Réponse GET

Le client reçoit :

```json
{
  "id": 1,
  "name": "Clavier",
  "price": 49.99
}
```

Le contrat est donc plus clair.

---

# 31. Résumé conceptuel

Avec les DTO, on sépare trois choses :

## 1. Les données échangées avec le client

Ce sont les DTO.

## 2. Les objets manipulés par l’application

Ce sont les modèles métier du `Domain`.

## 3. Les données stockées et lues en base

Elles sont manipulées par le repository via SQL.

---

# 32. Schéma mental à retenir

Il faut retenir cette idée :

```text
Le client ne parle pas directement au Domain.
Le client parle à l’API via des DTO.
```

Et côté serveur :

```text
Request DTO → conversion → Model métier
Model métier → conversion → Response DTO
```

C’est ça le cœur de la logique.

---