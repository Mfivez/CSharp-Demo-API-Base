# Cours — La validation des données

Jusqu’ici, avec les DTO, on a amélioré l’API :

* le controller ne reçoit plus directement un `Product`
* le client envoie des **DTO de requête**
* l’API renvoie des **DTO de réponse**

Cela permet de mieux contrôler **la forme des données**.

Mais il reste encore un problème important.

---

# 1. Le problème actuel

Imaginons ce DTO :

```csharp
public class ProductCreateRequest
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}
```

Et ce controller :

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

Aujourd’hui, le client peut envoyer :

```json
{
  "name": "",
  "price": -50
}
```

Et l’API va :

* accepter la requête 
* appeler le service 
* enregistrer en base 

---

## Pourquoi c’est un problème ?

Parce que :

* un produit sans nom n’a pas de sens
* un prix négatif est incohérent
* on enregistre des données invalides

On a contrôlé la **structure**, mais pas la **validité**

---

# 2. L’objectif de la validation

L’objectif est simple :

vérifier que les données sont correctes **avant de les utiliser**

Autrement dit :

```text
Client → Validation → Controller → Service → Repository
```

Si les données sont invalides :

on arrête tout immédiatement

---

# 3. Où faire la validation ?

Dans notre architecture :

* pas dans le repository
* pas dans le Domain
* pas dans SQL

👉 **dans les DTO**

Pourquoi ?

Parce que les DTO représentent :

> “ce que le client a le droit d’envoyer”

---

# 4. Principe de la validation avec ASP.NET Core

ASP.NET Core propose un système simple :

des **attributs** à mettre sur les propriétés

Ces attributs permettent de définir des règles.

---

# 5. Exemple de validation simple

On modifie le DTO :

```csharp
using System.ComponentModel.DataAnnotations;

public class ProductCreateRequest
{
    [Required]
    public string Name { get; set; } = "";

    [Range(0.01, 10000)]
    public decimal Price { get; set; }
}
```

---

## Signification

* `[Required]` → la valeur est obligatoire
* `[Range]` → la valeur doit être dans un intervalle

---

# 6. Ajouter des messages personnalisés

Pour améliorer les erreurs retournées :

```csharp
using System.ComponentModel.DataAnnotations;

public class ProductCreateRequest
{
    [Required(ErrorMessage = "Le nom est obligatoire")]
    [StringLength(100, ErrorMessage = "Maximum 100 caractères")]
    public string Name { get; set; } = "";

    [Range(0.01, 10000, ErrorMessage = "Le prix doit être positif")]
    public decimal Price { get; set; }
}
```

Cela rend les erreurs plus compréhensibles

---

# 7. Ce que fait ASP.NET Core automatiquement

Grâce à l’attribut :

```csharp
[ApiController]
```

ASP.NET Core :

* valide automatiquement les DTO
* détecte les erreurs
* renvoie un **400 Bad Request**

sans écrire de code supplémentaire

---

# 8. Exemple concret

## Requête invalide

```json
{
  "name": "",
  "price": -10
}
```

---

## Réponse automatique

```json
{
  "errors": {
    "Name": ["Le nom est obligatoire"],
    "Price": ["Le prix doit être positif"]
  }
}
```

généré automatiquement par ASP.NET Core

---

# 9. Validation pour la mise à jour

On applique les mêmes règles :

```csharp
public class ProductUpdateRequest
{
    [Required]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [Range(0.01, 10000)]
    public decimal Price { get; set; }
}
```

---

# 10. Position de la validation dans le flow

Avant :

```text
Client → Controller → Service → Repository
```

Maintenant :

```text
Client → Validation → Controller → Service → Repository
```

Si invalide → arrêt immédiat

---

# 11. Ce qui change dans le controller

>**Rien !**

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

La validation est automatique.

---

# 14. Résultat côté API

Avant :

* acceptait tout

Maintenant :

* refuse les données invalides
* retourne des erreurs claires 
* protège la base de données 

---

# 15. Lien avec les DTO

Les DTO permettent de :

* définir la forme des données

La validation permet de :

* vérifier la qualité des données

> **Les deux vont toujours ensemble**

---

# 16. Quelques exemples de validateurs

# 1. `[Required]` — Champ obligatoire

Vérifie que la valeur est présente

```csharp
[Required]
public string Name { get; set; }
```

## ✔️ Valide si :

* non null
* non vide (pour string)

## ❌ Refusé si :

* null
* chaîne vide

---

# 2. `[StringLength]` — Taille maximale / minimale

Limite la longueur d’une chaîne

```csharp
[StringLength(100)]
public string Name { get; set; }
```

## Variante avec minimum :

```csharp
[StringLength(100, MinimumLength = 2)]
```

---

# 3. `[MaxLength]` / `[MinLength]`

Alternative à `StringLength`

```csharp
[MaxLength(100)]
[MinLength(2)]
public string Name { get; set; }
```

---

# 4. `[Range]` — Intervalle de valeurs

Vérifie qu’une valeur est dans un intervalle

```csharp
[Range(0.01, 10000)]
public decimal Price { get; set; }
```

## ✔️ Valide si :

* dans l’intervalle

## ❌ Refusé si :

* trop petit ou trop grand

---

# 5. `[EmailAddress]` — Format email

Vérifie qu’une string est un email valide

```csharp
[EmailAddress]
public string Email { get; set; }
```

Très utile pour :

* inscription utilisateur
* formulaires

---

# 6. `[Phone]` — Numéro de téléphone

```csharp
[Phone]
public string PhoneNumber { get; set; }
```

Vérifie un format basique de téléphone

---

# 7. `[RegularExpression]` — Format personnalisé

Permet de définir une règle avec une regex

```csharp
[RegularExpression(@"^[A-Z][a-z]+$")]
public string Name { get; set; }
```

Très puissant mais plus avancé

---

# 8. `[Compare]` — Comparer deux champs

Vérifie que deux propriétés sont identiques

```csharp
[Compare("Password")]
public string ConfirmPassword { get; set; }
```

Utilisé pour :

* confirmation de mot de passe