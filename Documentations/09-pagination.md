Parfait, je te refais ça **proprement dans ton style**, adapté à **ton projet actuel** (avec mapper, service, repository, etc.), sans partir dans des trucs avancés.

---

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
* lenteur côté client
* surcharge serveur
* consommation réseau inutile

Le client n’a souvent besoin que d’une petite partie des données.

---

# 2. Objectif de la pagination

Ne renvoyer qu’un “morceau” des données.

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

On modifie l’action `GetAll` :

```csharp
[HttpGet]
public async Task<ActionResult<List<ProductResponse>>> GetAll(int page = 1, int pageSize = 10)
```

---

## Valeurs par défaut

* `page = 1`
* `pageSize = 10`

Donc si le client ne met rien :

```http
GET /api/products
```

on retourne automatiquement la première page.

---

# 5. Adapter le service

On propage simplement les paramètres vers le repository.

## ProductService.cs

```csharp
public async Task<List<Product>> GetAllProductsAsync(int page, int pageSize)
{
    return await _productRepository.GetAllAsync(page, pageSize);
}
```

---

## Interface

```csharp
Task<List<Product>> GetAllProductsAsync(int page, int pageSize);
```

---

# 6. Adapter le repository

C’est ici que la pagination est réellement appliquée.

---

## Principe SQL

On utilise :

```sql
OFFSET ... ROWS FETCH NEXT ... ROWS ONLY
```

---

## Explication

* `OFFSET` = nombre de lignes à ignorer
* `FETCH NEXT` = nombre de lignes à récupérer

---

## Calcul de l’offset

```csharp
int offset = (page - 1) * pageSize;
```

---

## Code complet

### ProductRepository.cs

```csharp
public async Task<List<Product>> GetAllAsync(int page, int pageSize)
{
    var products = new List<Product>();

    await using SqlConnection connection = new SqlConnection(_connectionString);

    const string query = @"
        SELECT Id, Name, Price
        FROM Products
        ORDER BY Id
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY";

    await using SqlCommand command = new SqlCommand(query, connection);

    int offset = (page - 1) * pageSize;

    command.Parameters.AddWithValue("@Offset", offset);
    command.Parameters.AddWithValue("@PageSize", pageSize);

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
Task<List<Product>> GetAllAsync(int page, int pageSize);
```

---

# 7. Retour dans le controller avec mapping

On utilise notre `ProductMapper` pour garder le controller propre.

## ProductsController.cs

```csharp
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

Exemples :

```http
GET /api/products?page=1&pageSize=5
```

→ produits 1 à 5

```http
GET /api/products?page=2&pageSize=5
```

→ produits 6 à 10

---

# 9. Bonnes pratiques

---

## Limiter pageSize

Pour éviter qu’un client demande trop de données :

```csharp
if (pageSize > 50)
{
    pageSize = 50;
}
```

---

## Vérifier page

```csharp
if (page < 1)
{
    page = 1;
}
```

---

## Où mettre ces validations ?

Dans le controller (simple et clair) :

```csharp
[HttpGet]
public async Task<ActionResult<List<ProductResponse>>> GetAll(int page = 1, int pageSize = 10)
{
    if (page < 1)
    {
        page = 1;
    }

    if (pageSize > 50)
    {
        pageSize = 50;
    }

    var products = await _productService.GetAllProductsAsync(page, pageSize);

    var response = products.Select(ProductMapper.ToResponse).ToList();

    return Ok(response);
}
```

---

# 10. Ce que fait réellement la pagination

Quand tu fais :

```http
GET /api/products?page=2&pageSize=5
```

Alors :

```csharp
offset = (2 - 1) * 5 = 5
```

SQL exécute :

```sql
OFFSET 5 ROWS FETCH NEXT 5 ROWS ONLY
```

Donc :

* ignore les 5 premiers produits
* récupère les 5 suivants

---

# 11. Schéma mental

```text
Controller → reçoit page/pageSize
Service    → transmet
Repository → applique OFFSET/FETCH
SQL        → retourne seulement une partie des données
```

---

# 12. Limite de cette approche

Cette pagination est **simple**.

Elle ne donne pas :

* le nombre total de produits
* le nombre de pages
* des infos comme “page suivante”

On renvoie uniquement une liste.

---

# 13. Résumé

Avant :

* toutes les données renvoyées
* pas scalable
* pas performant

Après :

* données découpées en pages
* meilleures performances
* API plus propre

---

# 14. Conclusion

La pagination est essentielle dans une API.

Elle permet :

* de limiter la taille des réponses
* d’améliorer les performances
* de mieux contrôler l’accès aux données

Dans cette version simple :

* le controller reçoit `page` et `pageSize`
* le service transmet
* le repository applique la pagination SQL

---

# 15. Schéma final

```text
GET /api/products?page=2&pageSize=5

→ Controller
→ Service
→ Repository
→ SQL OFFSET / FETCH
→ 5 produits retournés
```

---

## On peut faire mieux ?

Oui, on peut faire de la pagination avancée avec :

* total count
* metadata (page, totalPages)
* objet `PagedResult<T>`

Mais chaque chose en son temps !