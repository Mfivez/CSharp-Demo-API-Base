# Exercice — Implémenter le reste du CRUD

## Objectif

Actuellement, l'API qu'on a mis en place, permet uniquement de :

* récupérer tous les produits (`GET`)
* récupérer un produit par son `Id` (`GET`)

Il manque encore :

* **Créer un produit** (`POST`)
* **Modifier un produit** (`PUT`)
* **Supprimer un produit** (`DELETE`)

Tu vas implémenter ces fonctionnalités en respectant **l’architecture mis en place au cours** :

```
Controller → BLL → DAL → SQL
```

---

# Étape 1 — Vérifier le modèle `Product`

## Fichier

```
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

Rien à modifier ici, mais c’est la base de tout.

---

# Étape 2 — Compléter les interfaces

## But

Définir les nouvelles opérations du CRUD **avant de les implémenter**.

---

## DAL/IProductRepository.cs

```csharp
void Add(Product product);
void Update(Product product);
void Delete(int id);
```

---

## BLL/IProductService.cs
```csharp
void CreateProduct(Product product);
void UpdateProduct(Product product);
void DeleteProduct(int id);
```

---

## Pourquoi ?

On définit d’abord **le contrat**, pour garder une architecture propre et claire.

---

# Étape 3 — Implémenter le repository (DAL)

## DAL/ProductRepository.cs

---

## CREATE

```csharp
public void Add(Product product)
{
    using SqlConnection connection = new SqlConnection(_connectionString);

    string query = "INSERT INTO Products (Name, Price) VALUES (@Name, @Price)";

    using SqlCommand command = new SqlCommand(query, connection);

    command.Parameters.AddWithValue("@Name", product.Name);
    command.Parameters.AddWithValue("@Price", product.Price);

    connection.Open();
    command.ExecuteNonQuery();
}
```

---

## Update

```csharp
public void Update(Product product)
{
    using SqlConnection connection = new SqlConnection(_connectionString);

    string query = "UPDATE Products SET Name = @Name, Price = @Price WHERE Id = @Id";

    using SqlCommand command = new SqlCommand(query, connection);

    command.Parameters.AddWithValue("@Id", product.Id);
    command.Parameters.AddWithValue("@Name", product.Name);
    command.Parameters.AddWithValue("@Price", product.Price);

    connection.Open();
    command.ExecuteNonQuery();
}
```

---

## Delete

```csharp
public void Delete(int id)
{
    using SqlConnection connection = new SqlConnection(_connectionString);

    string query = "DELETE FROM Products WHERE Id = @Id";

    using SqlCommand command = new SqlCommand(query, connection);

    command.Parameters.AddWithValue("@Id", id);

    connection.Open();
    command.ExecuteNonQuery();
}
```

---

# Étape 4 — Implémenter le service (BLL)

## BLL/ProductService.cs

---

## Create

```csharp
public void CreateProduct(Product product)
{
    _productRepository.Add(product);
}
```

---

## Update

```csharp
public void UpdateProduct(Product product)
{
    _productRepository.Update(product);
}
```

---

## Delete

```csharp
public void DeleteProduct(int id)
{
    _productRepository.Delete(id);
}
```

---

# Étape 5 — Compléter le controller

## Controllers/ProductsController.cs

---

## POST — créer un produit

```csharp
[HttpPost]
public ActionResult Create(Product product)
{
    _productService.CreateProduct(product);
    return Ok();
}
```

---

## PUT — modifier un produit

```csharp
[HttpPut("{id}")]
public ActionResult Update(int id, Product product)
{
    _productService.UpdateProduct(product);
    return Ok();
}
```

---

## DELETE — supprimer un produit

```csharp
[HttpDelete("{id}")]
public ActionResult Delete(int id)
{
    _productService.DeleteProduct(id);
    return Ok();
}
```

---

# Étape 6 — Tester avec Swagger

## Endpoints à tester

```
GET     /api/products
GET     /api/products/{id}
POST    /api/products
PUT     /api/products/{id}
DELETE  /api/products/{id}
```

---

## Cas à tester

### Create

* ajouter un produit
* vérifier en base

---

### Update

* modifier un produit existant
* vérifier en base

---

### Delete

* supprimer un produit
* vérifier en base

---


# Résultat final attendu


| Action   | Méthode HTTP | Route              |
| -------- | ------------ | ------------------ |
| Read all | GET          | /api/products      |
| Read one | GET          | /api/products/{id} |
| Create   | POST         | /api/products      |
| Update   | PUT          | /api/products/{id} |
| Delete   | DELETE       | /api/products/{id} |

---