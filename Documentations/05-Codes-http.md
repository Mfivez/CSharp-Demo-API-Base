# Cours — Les codes HTTP dans une API ASP.NET Core

Jusqu’ici, notre API fonctionne correctement :

* elle reçoit des requêtes
* elle traite les données
* elle renvoie des réponses

Mais il reste un point important :

**on renvoie toujours `200 OK`**

Or, en REST, le code HTTP est **très important**.

---

# 1. Le problème actuel

Aujourd’hui, on fait souvent :

```csharp
return Ok();
```

ou :

```csharp
return Ok(product);
```

Peu importe la situation, on renvoie `200 OK`.

---

## Pourquoi c’est un problème ?

Le client ne sait pas :

* si une ressource a été créée
* si une suppression a réussi
* si quelque chose n’existe pas
* si une erreur s’est produite

On perd de l’information

---

# 2. Rôle des codes HTTP

Les codes HTTP servent à :

**décrire le résultat de la requête**

Sans lire le contenu, le client comprend déjà ce qu’il s’est passé.

---

## Exemple

| Code | Signification    |
| ---- | ---------------- |
| 200  | OK               |
| 201  | Créé             |
| 204  | Pas de contenu   |
| 400  | Requête invalide |
| 404  | Non trouvé       |

---

# 3. Les grandes familles de codes

## 2xx — Succès

* tout s’est bien passé

## 4xx — Erreur client

* problème dans la requête

## 5xx — Erreur serveur

* problème côté API

---

# 4. Les codes à connaître absolument

On va se concentrer sur les plus utilisés.

---

# 5. `200 OK`

Tout s’est bien passé

### Exemple

```csharp
return Ok(products);
```

Utilisé pour :

* GET
* réponses avec contenu

---

# 6. `201 Created`

Une ressource a été créée

### Exemple

```csharp
return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
```

Utilisé pour :

* POST

---

## Pourquoi pas `200` ?

Parce que :

ce n’est pas juste “OK”

c’est “créé”

---

# 7. `204 NoContent`

Succès mais **sans contenu**

### Exemple

```csharp
return NoContent();
```

Utilisé pour :

* DELETE
* UPDATE (souvent)

---

# 8. `400 BadRequest`

Requête invalide

### Exemple

```csharp
return BadRequest("Id incohérent");
```

ou automatique avec validation

---

# 9. `404 NotFound`

Ressource inexistante

### Exemple

```csharp
if (product == null)
{
    return NotFound();
}
```

Très important pour les GET

---

# 10. Mise en pratique dans la feature Product

---

## GET all

```csharp
[HttpGet]
public ActionResult<List<ProductResponse>> GetAll()
{
    var products = _productService.GetAllProducts();

    return Ok(products);
}
```

`200 OK`

---

## GET by id

```csharp
[HttpGet("{id}")]
public ActionResult<ProductResponse> GetById(int id)
{
    var product = _productService.GetProductById(id);

    if (product == null)
    {
        return NotFound();
    }

    return Ok(product);
}
```

`200` ou `404`

---

## POST

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

    return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
}
```

`201 Created`

---

## PUT

```csharp
[HttpPut("{id}")]
public ActionResult Update(int id, ProductUpdateRequest request)
{
    if (id != request.Id)
    {
        return BadRequest();
    }

    _productService.UpdateProduct(new Product
    {
        Id = request.Id,
        Name = request.Name,
        Price = request.Price
    });

    return NoContent();
}
```

`204 NoContent`

---

## DELETE

```csharp
[HttpDelete("{id}")]
public ActionResult Delete(int id)
{
    _productService.DeleteProduct(id);

    return NoContent();
}
```

`204 NoContent`

---

# 11. Résumé des bonnes pratiques

| Action            | Code |
| ----------------- | ---- |
| GET               | 200  |
| GET (not found)   | 404  |
| POST              | 201  |
| PUT               | 204  |
| DELETE            | 204  |
| Erreur validation | 400  |

---

# 12. Ce que voit le client

## POST réussi

```http
201 Created
```

## GET inexistant

```http
404 Not Found
```

## Erreur validation

```http
400 Bad Request
```

---

# 13. Pourquoi c’est important

Les codes HTTP permettent :

* une API claire
* une API standard REST
* une meilleure communication avec le front

---

# 14. Résumé conceptuel

Le code HTTP décrit le résultat

Le body contient les données

---
