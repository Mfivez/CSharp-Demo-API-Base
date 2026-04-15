# Cours — Async / Await

Jusqu’ici, notre API fonctionne correctement :

* elle accède à la base de données
* elle traite les données
* elle renvoie des réponses

Mais il y a un problème invisible :

**tout est synchrone**

---

# 1. Le problème actuel

Aujourd’hui, dans le repository :

```csharp
public List<Product> GetAll()
{
    using SqlConnection connection = new SqlConnection(_connectionString);

    string query = "SELECT Id, Name, Price FROM Products";

    using SqlCommand command = new SqlCommand(query, connection);

    connection.Open();

    using SqlDataReader reader = command.ExecuteReader();

    // lecture...
}
```

Ce code est **bloquant**

---

## Qu’est-ce que ça veut dire ?

Quand une requête arrive :

* ASP.NET exécute ton code
* il attend que SQL réponde
* pendant ce temps → il ne fait rien d’autre

Le thread est **bloqué**

---

# 2. Pourquoi c’est un problème ?

Si plusieurs utilisateurs appellent ton API :

* chaque requête bloque un thread
* le serveur sature plus vite
* performances limitées

Mauvais pour la scalabilité

---

# 3. L’idée de l’asynchrone

Avec `async/await`, on dit :

“Pendant que j’attends la base, fais autre chose”

---

## Avant

```text
Requête → attente SQL → réponse
```

## Après

```text
Requête → lance SQL → libère thread → reprend quand fini
```

---

# 4. Les mots-clés à connaître

## `async`

indique qu’une méthode est asynchrone

## `await`

attend le résultat **sans bloquer**

## `Task`

représente une opération en cours

---

# 5. Modifier le repository

---

## Avant

```csharp
public List<Product> GetAll()
```

---

## Après

```csharp
public async Task<List<Product>> GetAllAsync()
```

---

## Code complet

```csharp
public async Task<List<Product>> GetAllAsync()
{
    var products = new List<Product>();

    using SqlConnection connection = new SqlConnection(_connectionString);

    string query = "SELECT Id, Name, Price FROM Products";

    using SqlCommand command = new SqlCommand(query, connection);

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

# 6. Modifier l’interface du repository

```csharp
Task<List<Product>> GetAllAsync();
Task<Product?> GetByIdAsync(int id);
```

---

# 7. Modifier le service

---

## Avant

```csharp
public List<Product> GetAllProducts()
{
    return _productRepository.GetAll();
}
```

---

## Après

```csharp
public async Task<List<Product>> GetAllProductsAsync()
{
    return await _productRepository.GetAllAsync();
}
```

---

# 8. Modifier le controller

---

## Avant

```csharp
[HttpGet]
public ActionResult<List<ProductResponse>> GetAll()
```

---

## Après

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

# 9. Le flow complet devient

```text
Controller (async)
   ↓ await
Service (async)
   ↓ await
Repository (async)
   ↓ await
SQL Server
```

Tout est non-bloquant

---

# 10. Règles importantes

---

## Toujours propager async

Si le repository est async :

service doit être async
controller doit être async

---

## Toujours utiliser `await`

```csharp
return await _repo.GetAllAsync();
```

---

# 12. Ce qui ne change pas

* DTO
* validation
* logique métier
* SQL

seul le mode d’exécution change

---

# 13. Résumé conceptuel

Avant :

```text
bloquant → 1 requête = 1 thread occupé
```

Après :

```text
non bloquant → meilleure gestion des ressources
```

---

# 14. Schéma mental

```text
async = libérer le thread pendant l’attente
await = reprendre quand c’est prêt
```

---