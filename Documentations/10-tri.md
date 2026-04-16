# Cours — Filtres et recherche dans une API ASP.NET Core

Jusqu’ici, notre API permet :

* de récupérer des produits
* de paginer les résultats

Mais il manque une fonctionnalité essentielle :

**filtrer les données**

---

# 1. Le problème actuel

Aujourd’hui :

```http
GET /api/products
```

renvoie :

* tous les produits
* ou une page de produits

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

pour récupérer uniquement ce qui l’intéresse.

---

# 3. Principe

On ajoute des **paramètres optionnels** dans la requête HTTP :

```text
page, pageSize, minPrice, maxPrice, name
```

Chaque paramètre peut être :

* présent → filtre appliqué
* absent → ignoré

---

# 4. Adapter le controller

On ajoute simplement les paramètres dans l’action.

## ProductsController.cs

```csharp
[HttpGet]
public async Task<ActionResult<List<ProductResponse>>> GetAll(
    int page = 1,
    int pageSize = 10,
    decimal? minPrice = null,
    decimal? maxPrice = null,
    string? name = null)
{
    if (page < 1)
    {
        page = 1;
    }

    if (pageSize > 50)
    {
        pageSize = 50;
    }

    var products = await _productService.GetAllProductsAsync(
        page,
        pageSize,
        minPrice,
        maxPrice,
        name);

    var response = products.Select(ProductMapper.ToResponse).ToList();

    return Ok(response);
}
```

---

# 5. Adapter le service

Le service ne fait que transmettre les paramètres.

## ProductService.cs

```csharp
public async Task<List<Product>> GetAllProductsAsync(
    int page,
    int pageSize,
    decimal? minPrice,
    decimal? maxPrice,
    string? name)
{
    return await _productRepository.GetAllAsync(
        page,
        pageSize,
        minPrice,
        maxPrice,
        name);
}
```

---

## Interface

```csharp
Task<List<Product>> GetAllProductsAsync(
    int page,
    int pageSize,
    decimal? minPrice,
    decimal? maxPrice,
    string? name);
```

---

# 6. Adapter le repository

C’est ici que l’on applique réellement les filtres.

---

## Principe

On construit la requête SQL **dynamiquement**.

On part d’une base :

```sql
WHERE 1=1
```

Puis on ajoute les filtres uniquement si nécessaire.

---

## Code

### ProductRepository.cs

```csharp
public async Task<List<Product>> GetAllAsync(
    int page,
    int pageSize,
    decimal? minPrice,
    decimal? maxPrice,
    string? name)
{
    var products = new List<Product>();

    await using SqlConnection connection = new SqlConnection(_connectionString);

    var query = "SELECT Id, Name, Price FROM Products WHERE 1=1";

    await using SqlCommand command = new SqlCommand();
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

    await using SqlDataReader reader = await command.ExecuteReaderAsync();

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

## Interface repository

```csharp
Task<List<Product>> GetAllAsync(
    int page,
    int pageSize,
    decimal? minPrice,
    decimal? maxPrice,
    string? name);
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

## Combiner filtres + pagination

```http
GET /api/products?page=2&pageSize=5&minPrice=10
```

---

## Combinaison complète

```http
GET /api/products?page=1&pageSize=10&minPrice=10&maxPrice=100&name=stylo
```

---

# 8. Pourquoi utiliser des paramètres optionnels ?

Parce que le client choisit ce qu’il veut faire :

* aucun filtre → tous les produits
* un filtre → filtrage simple
* plusieurs filtres → filtrage combiné

---

# 9. Pourquoi `WHERE 1=1` ?

C’est une astuce pour simplifier la construction dynamique.

Au lieu de gérer :

```sql
WHERE Price >= ...
```

ou :

```sql
WHERE Price >= ... AND Name LIKE ...
```

on écrit toujours :

```sql
WHERE 1=1
```

Puis on ajoute simplement :

```sql
AND ...
```

---

# 10. Sécurité (important)

Même si on construit la requête dynamiquement :

on utilise **toujours des paramètres SQL**

```csharp
command.Parameters.AddWithValue(...)
```

Donc :

* pas de concaténation dangereuse
* protection contre les injections SQL

---

# 11. Schéma mental

```text
Controller → reçoit les filtres
Service    → transmet
Repository → construit la requête SQL
SQL        → applique filtres + pagination
```

---

# 12. Résumé

Avant :

* impossible de filtrer
* données trop générales

Après :

* filtres dynamiques
* recherche par nom
* filtrage par prix
* combinable avec pagination

---

# 13. Conclusion

Avec les filtres :

* l’API devient beaucoup plus utile
* le client récupère seulement ce dont il a besoin
* les performances sont meilleures

On combine maintenant :

* pagination
* filtres
* mapping propre

---

# 14. Schéma final

```text
GET /api/products?page=2&pageSize=5&minPrice=10&name=stylo

→ Controller
→ Service
→ Repository (SQL dynamique)
→ Résultat filtré + paginé
```