# Cours — La pagination dans une API ASP.NET Core

Jusqu’ici, notre API permet de récupérer des données avec :

```http
GET /api/products
```

Mais il y a un problème important :

**on renvoie toutes les données**

---

# 1. Le problème actuel

Imaginons que la base contient :

* 10 produits → OK
* 100 produits → OK
* 10 000 produits → PAS OK

---

## Pourquoi c’est un problème ?

* réponse trop lourde
* lenteur
* surcharge serveur
* inutile pour le client

Le client n’a souvent besoin que d’une partie et non l'ensemble de la db...

---

# 2. Objectif de la pagination

Ne renvoyer qu’un “morceau” des données

Exemple :

```http
GET /api/products?page=1&pageSize=10
```

Résultat :

* page 1
* 10 produits

---

# 3. Principe

On découpe les données en pages :

```text
Page 1 → produits 1 à 10  
Page 2 → produits 11 à 20  
Page 3 → produits 21 à 30  
```

---

# 4. Ajouter les paramètres dans le controller

```csharp id="onm7tf"
[HttpGet]
public async Task<ActionResult<List<ProductResponse>>> GetAll(int page = 1, int pageSize = 10)
```

Valeurs par défaut :

* page = 1
* pageSize = 10

---

# 5. Adapter le service

```csharp id="fdvib0"
public async Task<List<Product>> GetAllProductsAsync(int page, int pageSize)
{
    return await _productRepository.GetAllAsync(page, pageSize);
}
```

---

# 6. Adapter le repository

---

## Principe SQL

On utilise :

```sql
OFFSET ... ROWS FETCH NEXT ... ROWS ONLY
```

---

## Code

```csharp id="70nfgi"
public async Task<List<Product>> GetAllAsync(int page, int pageSize)
{
    var products = new List<Product>();

    using SqlConnection connection = new SqlConnection(_connectionString);

    string query = @"
        SELECT Id, Name, Price
        FROM Products
        ORDER BY Id
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY";

    using SqlCommand command = new SqlCommand(query, connection);

    int offset = (page - 1) * pageSize;

    command.Parameters.AddWithValue("@Offset", offset);
    command.Parameters.AddWithValue("@PageSize", pageSize);

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

# 7. Retour dans le controller avec mapping

```csharp id="o4t3uv"
[HttpGet]
public async Task<ActionResult<List<ProductResponse>>> GetAll(int page = 1, int pageSize = 10)
{
    var products = await _productService.GetAllProductsAsync(page, pageSize);

    var response = products.Select(ProductMapper.ToResponse).ToList();

    return Ok(response);
}
```

---

# 8. Tester dans Swagger

Exemple :

```http
GET /api/products?page=2&pageSize=5
```

renvoie :

* produits 6 à 10

---

# 9. Bonnes pratiques

---

## Limiter pageSize

```csharp id="0g6cz6"
if (pageSize > 50)
{
    pageSize = 50;
}
```

histoire d'éviter les abus

---

## Vérifier page

```csharp id="4ggw9g"
if (page < 1)
{
    page = 1;
}
```

---

PS: Noter que ce .md n'aborde qu'une pagination simple.