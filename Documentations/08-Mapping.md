# Cours — Organiser le mapping (nettoyer les controllers)

Jusqu’ici, on a introduit les DTO et on fait encore le mapping directement dans les controllers.

Ça fonctionne, mais ça pose un problème :

**les controllers deviennent vite chargés et répétitifs**

---

# 1. Le problème actuel

Aujourd’hui, dans le controller, on retrouve du code de transformation directement dans les actions.

Exemple :

```csharp
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
```

---

## Résultat

On se retrouve avec :

* du code dupliqué
* de la logique mélangée
* des controllers moins lisibles

Or, le controller devrait surtout :

* recevoir la requête HTTP
* appeler le service
* renvoyer la réponse HTTP

Le mapping ne devrait pas alourdir cette couche.

---

# 2. L’idée

On va déplacer le mapping dans une classe dédiée.

Comme ça, le controller devient plus simple.

Au lieu d’écrire :

```csharp
var response = products.Select(p => new ProductResponse
{
    Id = p.Id,
    Name = p.Name,
    Price = p.Price
}).ToList();
```

on pourra écrire :

```csharp
var response = products.Select(ProductMapper.ToResponse).ToList();
```

---

# 3. Où mettre le mapping ?

Dans une classe dédiée, côté API.

Par exemple :

```text
Démo_simple_API/
 └── Mappers/
     └── ProductMapper.cs
```

---

## Pourquoi dans l’API ?

Parce que le mapping concerne ici :

* les DTO
* les controllers
* la forme des requêtes et des réponses HTTP

Le domaine (`Domain`) ne doit pas dépendre des DTO de l’API.

Le mapper a donc naturellement sa place dans le projet API.

---

# 4. Créer un mapper simple

## Mappers/ProductMapper.cs

```csharp
using Démo_simple_API.DTO.Product;
using Domain.Entities;

namespace Démo_simple_API.Mappers
{
    public static class ProductMapper
    {
        public static ProductResponse ToResponse(Product product)
        {
            return new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price
            };
        }

        public static Product ToEntity(ProductCreateRequest request)
        {
            return new Product
            {
                Name = request.Name,
                Price = request.Price
            };
        }

        public static Product ToEntity(ProductUpdateRequest request, int id)
        {
            return new Product
            {
                Id = id,
                Name = request.Name,
                Price = request.Price
            };
        }
    }
}
```

---

# 5. Utiliser le mapper dans le controller

Maintenant, on peut simplifier les actions du controller.

---

## GET all

Avant :

```csharp
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
```

Après :

```csharp
[HttpGet]
public async Task<ActionResult<List<ProductResponse>>> GetAll()
{
    var products = await _productService.GetAllProductsAsync();

    var response = products.Select(ProductMapper.ToResponse).ToList();

    return Ok(response);
}
```

Le controller devient immédiatement plus lisible.

---

## GET by id

Avant :

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

Après :

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<ProductResponse>> GetById(int id)
{
    var product = await _productService.GetProductByIdAsync(id);

    return Ok(ProductMapper.ToResponse(product));
}
```

---

## POST

Avant :

```csharp
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
```

Après :

```csharp
[HttpPost]
public async Task<ActionResult<ProductResponse>> Create(ProductCreateRequest request)
{
    var product = ProductMapper.ToEntity(request);

    int newId = await _productService.CreateProductAsync(product);

    product.Id = newId;

    return CreatedAtAction(nameof(GetById), new { id = newId }, ProductMapper.ToResponse(product));
}
```

---

## PUT

```csharp
[HttpPut("{id}")]
public async Task<ActionResult> Update(int id, ProductUpdateRequest request)
{
    var product = ProductMapper.ToEntity(request, id);

    await _productService.UpdateProductAsync(product);

    return NoContent();
}
```

---

# 6. Controller final nettoyé

## Controllers/ProductsController.cs

```csharp
using BLL.Interfaces;
using Démo_simple_API.DTO.Product;
using Démo_simple_API.Mappers;
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

            var response = products.Select(ProductMapper.ToResponse).ToList();

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponse>> GetById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            return Ok(ProductMapper.ToResponse(product));
        }

        [HttpPost]
        public async Task<ActionResult<ProductResponse>> Create(ProductCreateRequest request)
        {
            var product = ProductMapper.ToEntity(request);

            int newId = await _productService.CreateProductAsync(product);

            product.Id = newId;

            return CreatedAtAction(nameof(GetById), new { id = newId }, ProductMapper.ToResponse(product));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, ProductUpdateRequest request)
        {
            var product = ProductMapper.ToEntity(request, id);

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

# Pourquoi c’est mieux ?

Avec cette organisation :

* le mapping est centralisé
* les controllers sont plus courts
* le code est plus lisible
* si la structure change, on modifie à un seul endroit

Le controller reste concentré sur son vrai rôle : gérer HTTP.

---

# Schéma mental

```text
DTO ↔ Mapper ↔ Domain
```

---

# Peut-on faire mieux plus tard ?

Oui, plus tard on pourra utiliser un outil comme :

**AutoMapper**

Mais pour apprendre et pour un petit projet, le mapping manuel est très bien :

* plus simple
* plus lisible
* plus pédagogique

---

# Résumé

Avant :

* mapping dans le controller
* duplication
* controllers plus lourds

Après :

* mapping centralisé dans `ProductMapper`
* controllers plus propres
* code plus lisible

---

# Conclusion

Quand on commence à utiliser des DTO, il est préférable de ne pas laisser tout le mapping dans les controllers.

On crée plutôt une classe dédiée.

Comme ça :

* le code est mieux organisé
* le controller reste léger
* le mapping est regroupé au même endroit

En clair :

**on nettoie les controllers en sortant le mapping dans un mapper**