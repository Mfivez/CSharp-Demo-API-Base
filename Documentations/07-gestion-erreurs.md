Parfait 👍 on continue dans le même style, clair et progressif.

---

# Cours — La gestion des erreurs dans une API ASP.NET Core

Jusqu’ici, notre API :

* reçoit des requêtes
* valide les données
* appelle le service
* accède à la base

Il est maintenant temps d'attaquer :

**la gestion des erreurs**

---

# 1. Le problème actuel

Aujourd’hui, si une erreur se produit :

```csharp
using SqlConnection connection = new SqlConnection(_connectionString);
connection.Open(); // erreur possible
```

que se passe-t-il ?
* exception levée
* API plante
* réponse incohérente

---

## Exemple

Si SQL est arrêté :

le client peut recevoir :

* une erreur technique
* ou une réponse non contrôlée

---

## Pourquoi c’est un problème ?

* mauvaise expérience utilisateur
* difficile à déboguer
* pas propre

et je le rappelle, une API doit toujours répondre proprement, même quand il s'agit d'erreur

---

# 2. Objectif de la gestion des erreurs

Toujours renvoyer une réponse claire et controllée

Exemple :

```json
{
  "message": "Une erreur est survenue"
}
```

---

# 3. Première approche : try/catch

On peut gérer les erreurs dans le controller :

```csharp
[HttpGet]
public ActionResult GetAll()
{
    try
    {
        var products = _productService.GetAllProducts();
        return Ok(products);
    }
    catch (Exception)
    {
        return StatusCode(500, "Erreur serveur");
    }
}
```

---

## Problème de cette approche

* répétitif 
* lourd 
* difficile à maintenir 

chaque méthode doit gérer ses erreurs

---

# 4. L’idée du middleware global

ASP.NET Core permet de :

intercepter toutes les erreurs au même endroit

C’est ce qu’on appelle :

**un middleware**

---

# 5. Principe du middleware d’erreur

```text
Requête → Pipeline → Controller → Exception
                      ↓
                 Middleware
                      ↓
                 Réponse propre
```

---

# 6. Créer un middleware simple

## API/Middlewares/ExceptionMiddleware.cs

```csharp
using System.Net;
using System.Text.Json;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception)
        {
            await HandleException(context);
        }
    }

    private static async Task HandleException(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var response = new
        {
            message = "Une erreur interne est survenue"
        };

        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}
```

---

# 7. Ajouter le middleware dans l’application

## Program.cs

Ajouter :

```csharp
app.UseMiddleware<ExceptionMiddleware>();
```

À placer avant :

```csharp
app.MapControllers();
```

---

# 8. Résultat

Avant :

* crash
* stacktrace

Maintenant :

```json
{
  "message": "Une erreur interne est survenue"
}
```

---

# 9. Améliorer les messages (option simple)

On peut adapter selon l’exception :

```csharp
catch (Exception ex)
{
    await HandleException(context, ex);
}
```

---

```csharp
private static async Task HandleException(HttpContext context, Exception ex)
{
    context.Response.ContentType = "application/json";

    var statusCode = HttpStatusCode.InternalServerError;
    var message = "Une erreur interne est survenue";

    if (ex is ArgumentException)
    {
        statusCode = HttpStatusCode.BadRequest;
        message = ex.Message;
    }

    context.Response.StatusCode = (int)statusCode;

    var response = new { message };

    var json = JsonSerializer.Serialize(response);

    await context.Response.WriteAsync(json);
}
```

---

# 10. Où gérer les erreurs métier ?

Dans le service

Exemple :

```csharp
if (product == null)
{
    throw new ArgumentException("Produit introuvable");
}
```

le middleware s’occupe de la réponse

---

# 11. Ce qui change dans le controller

Rien

```csharp
[HttpGet("{id}")]
public ActionResult GetById(int id)
{
    var product = _productService.GetProductById(id);

    if (product == null)
    {
        return NotFound();
    }

    return Ok(product);
}
```

pas de try/catch partout

---

# 12. Position dans le flow

```text
Client → Middleware → Controller → Service → Repository
                 ↑
              Exception
```

---

# 13. Résumé

Avant :

* erreurs non gérées

Après :

* erreurs centralisées
* réponses propres
* code simplifié

---

# 14. Schéma mental

```text
Middleware = gestion globale des erreurs
```

---

# 15. Conclusion

Avec un middleware :

* l'API devient robuste
* tu évites la répétition
* tu contrôles toutes les erreurs
