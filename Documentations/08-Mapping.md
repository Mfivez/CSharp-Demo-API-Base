# Cours — Organiser le mapping (nettoyer les controllers)

Jusqu’ici, on a introduit les DTO et on fait le mapping directement dans les controllers.

Ça fonctionne, mais ça pose un problème :

**les controllers deviennent vite chargés et répétitifs**

---

# Le problème actuel

Aujourd’hui, dans le controller :

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

## Résultat :

* duplication de code
* logique mélangée
* controller moins lisible

Hors, le controller devrait juste :

* recevoir HTTP
* appeler le service
* renvoyer une réponse

---

On va donc déplacer le mapping ailleurs

Pour obtenir :

```csharp
var response = products.Select(ProductMapper.ToResponse).ToList();
```

---

# Où mettre le mapping ?

Dans une classe dédiée

---

## API/Mappers/ProductMapper.cs

Pourquoi dans l’API ?

* lié aux DTO
* utilisé par les controllers

---

# Créer un mapper simple

```csharp
using MyApi.Domain;
using MyApi.DTO.Product;

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

    public static Product ToEntity(ProductUpdateRequest request)
    {
        return new Product
        {
            Id = request.Id,
            Name = request.Name,
            Price = request.Price
        };
    }
}
```

---

# Utiliser le mapper dans le controller

---

## GET all

```csharp
[HttpGet]
public async Task<ActionResult<List<ProductResponse>>> GetAll()
{
    var products = await _productService.GetAllProductsAsync();

    var response = products.Select(ProductMapper.ToResponse).ToList();

    return Ok(response);
}
```

---

## GET by id

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<ProductResponse>> GetById(int id)
{
    var product = await _productService.GetProductByIdAsync(id);

    if (product == null)
    {
        return NotFound();
    }

    return Ok(ProductMapper.ToResponse(product));
}
```

---

## POST

```csharp
[HttpPost]
public async Task<ActionResult> Create(ProductCreateRequest request)
{
    var product = ProductMapper.ToEntity(request);

    await _productService.CreateProductAsync(product);

    return CreatedAtAction(nameof(GetById), new { id = product.Id }, null);
}
```

---

## PUT

```csharp
[HttpPut("{id}")]
public async Task<ActionResult> Update(int id, ProductUpdateRequest request)
{
    if (id != request.Id)
    {
        return BadRequest();
    }

    var product = ProductMapper.ToEntity(request);

    await _productService.UpdateProductAsync(product);

    return NoContent();
}
```


# Peut-on faire mieux ?

Oui, plus tard :

**AutoMapper**

Mais :

* plus complexe
* moins pédagogique au début

ici, le mapping manuel est parfait


---

# Schéma mental

```text
DTO ↔ Mapper ↔ Domain
```