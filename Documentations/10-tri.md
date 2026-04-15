# Cours — Filtres et recherche dans une API ASP.NET Core

Jusqu’ici, notre API permet :

* de récupérer des produits
* de paginer les résultats

Mais il manque une fonctionnalité importante :

**filtrer les données**

---

# 1. Le problème actuel

Aujourd’hui :

```http
GET /api/products
```

renvoie tous les produits (ou paginés)

Mais le client ne peut pas :

* filtrer par prix
* rechercher par nom
* affiner les résultats

---

# 2. Objectif

Permettre au client de faire :

```http
GET /api/products?minPrice=10&maxPrice=100
```

ou :

```http
GET /api/products?name=stylo
```

pour récupérer uniquement ce qui l’intéresse

---

# 3. Principe

On ajoute des **paramètres optionnels** :

```text
page, pageSize, minPrice, maxPrice, name
```

---

# 4. Adapter le controller

```csharp
[HttpGet]
public async Task<ActionResult<List<ProductResponse>>> GetAll(
    int page = 1,
    int pageSize = 10,
    decimal? minPrice = null,
    decimal? maxPrice = null,
    string? name = null)
{
    var products = await _productService.GetAllProductsAsync(page, pageSize, minPrice, maxPrice, name);

    var response = products.Select(ProductMapper.ToResponse).ToList();

    return Ok(response);
}
```

---

# 5. Adapter le service

```csharp
public async Task<List<Product>> GetAllProductsAsync(
    int page,
    int pageSize,
    decimal? minPrice,
    decimal? maxPrice,
    string? name)
{
    return await _productRepository.GetAllAsync(page, pageSize, minPrice, maxPrice, name);
}
```

---

# 6. Adapter le repository

C’est ici que ça devient intéressant

---

## Construire une requête dynamique

```csharp
public async Task<List<Product>> GetAllAsync(
    int page,
    int pageSize,
    decimal? minPrice,
    decimal? maxPrice,
    string? name)
{
    var products = new List<Product>();

    using SqlConnection connection = new SqlConnection(_connectionString);

    var query = "SELECT Id, Name, Price FROM Products WHERE 1=1";

    using SqlCommand command = new SqlCommand();
    command.Connection = connection;

    // filtre minPrice
    if (minPrice.HasValue)
    {
        query += " AND Price >= @MinPrice";
        command.Parameters.AddWithValue("@MinPrice", minPrice.Value);
    }

    // filtre maxPrice
    if (maxPrice.HasValue)
    {
        query += " AND Price <= @MaxPrice";
        command.Parameters.AddWithValue("@MaxPrice", maxPrice.Value);
    }

    // filtre name
    if (!string.IsNullOrWhiteSpace(name))
    {
        query += " AND Name LIKE @Name";
        command.Parameters.AddWithValue("@Name", $"%{name}%");
    }

    // pagination
    query += @"
        ORDER BY Id
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY";

    int offset = (page - 1) * pageSize;

    command.Parameters.AddWithValue("@Offset", offset);
    command.Parameters.AddWithValue("@PageSize", pageSize);

    command.CommandText = query;

    await connection.OpenAsync();

    using SqlDataReader reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
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
```

---

# 7. Exemple d’utilisation

---

## Filtrer par prix

```http
GET /api/products?minPrice=10&maxPrice=50
```

---

## Recherche par nom

```http
GET /api/products?name=stylo
```

---

## Combiner avec pagination

```http
GET /api/products?page=2&pageSize=5&minPrice=10
```

---

# 8. Pourquoi utiliser des paramètres optionnels ?

Parce que le client choisit ce qu’il veut filtrer

* aucun filtre → tous les produits
* un filtre → filtré
* plusieurs → combiné

---
