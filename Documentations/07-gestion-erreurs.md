# Cours — La gestion des erreurs dans une API ASP.NET Core (.NET 9)

Jusqu’ici, notre API :

* reçoit des requêtes
* valide les données
* appelle le service
* accède à la base

Il est maintenant temps d’attaquer :

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
* réponse non maîtrisée
* parfois une erreur technique envoyée au client

En production, une API ne doit pas renvoyer une stack trace brute ou une réponse incohérente. ASP.NET Core distingue d’ailleurs clairement le comportement de développement et celui de production. ([Microsoft Learn][2])

---

# 2. Objectif de la gestion des erreurs

Toujours renvoyer une réponse :

* claire
* contrôlée
* cohérente

En API moderne, le format recommandé est **Problem Details**. ASP.NET Core sait générer ce format via `AddProblemDetails()`. ([Microsoft Learn][1])

Exemple :

```json
{
  "type": "about:blank",
  "title": "Une erreur est survenue",
  "status": 500
}
```

---

# 3. Première approche : try/catch dans le controller

On pourrait faire :

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

Chaque action finit par gérer ses propres erreurs, ce qui alourdit inutilement le code.

---

# 4. L’approche recommandée en .NET 9

En **.NET 9**, on préfère utiliser le système natif fourni par ASP.NET Core :

* `UseExceptionHandler()`
* `IExceptionHandler`
* `AddProblemDetails()`

`UseExceptionHandler()` ajoute le middleware global de gestion des exceptions. `IExceptionHandler` permet de centraliser la logique métier de traitement des exceptions. `AddProblemDetails()` permet de produire des réponses d’erreur standardisées pour les APIs. ([Microsoft Learn][1])

---

# 5. Principe

```text
Client → Middleware → Controller → Service → Repository
                 ↑
            IExceptionHandler
                 ↑
         Réponse ProblemDetails
```

Si une exception non gérée remonte, le middleware d’exception l’intercepte. Ensuite, un `IExceptionHandler` peut la traiter et renvoyer une réponse propre. Si aucun handler ne la traite, le middleware applique son comportement par défaut. ([Microsoft Learn][3])

---

# 6. Enregistrer les services

## Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

app.MapControllers();

app.Run();
```

`AddProblemDetails()` enregistre le service chargé de produire les réponses d’erreur. `AddExceptionHandler<T>()` enregistre ton gestionnaire global d’exceptions. En environnement non développement, `UseExceptionHandler()` active le middleware d’exception. ([Microsoft Learn][1])

---

# 7. Créer le handler global

## GlobalExceptionHandler.cs

```csharp
using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = StatusCodes.Status500InternalServerError;
        var title = "Une erreur interne est survenue";

        if (exception is ArgumentException)
        {
            statusCode = StatusCodes.Status400BadRequest;
            title = exception.Message;
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
```

`IExceptionHandler` expose une seule méthode : `TryHandleAsync(HttpContext, Exception, CancellationToken)`. Si cette méthode retourne `true`, cela signifie que l’exception a été gérée. ([Microsoft Learn][4])

---

# 8. Résultat

Si une exception se produit, le client reçoit maintenant une réponse propre :

```json
{
  "title": "Une erreur interne est survenue",
  "status": 500
}
```

Et si l’exception correspond à une erreur métier simple :

```json
{
  "title": "Produit introuvable",
  "status": 400
}
```

ASP.NET Core recommande justement l’usage de **Problem Details** pour décrire les erreurs HTTP dans une API. ([Microsoft Learn][1])

---

# 9. Pourquoi cette approche est meilleure que le middleware maison

Avant, on écrivait souvent un middleware personnalisé du style :

```csharp
app.UseMiddleware<ExceptionMiddleware>();
```

Mais avec .NET 9, ce n’est plus nécessaire dans la majorité des cas :

* le middleware officiel existe déjà avec `UseExceptionHandler()`
* `IExceptionHandler` permet de brancher une logique personnalisée proprement
* l’intégration avec `ProblemDetails` est native

Donc le code est plus standard, plus propre, et plus proche de ce qu’attend l’écosystème ASP.NET Core moderne. ([Microsoft Learn][5])

---

# 10. Où gérer les erreurs métier ?

Dans le service.

Exemple :

```csharp
public Product GetProductById(int id)
{
    var product = _repository.GetById(id);

    if (product == null)
    {
        throw new ArgumentException("Produit introuvable");
    }

    return product;
}
```

Le service détecte l’erreur métier.
Le handler global transforme ensuite cette exception en réponse HTTP.

---

# 11. Ce qui change dans le controller

Presque rien.

```csharp
[HttpGet("{id}")]
public ActionResult GetById(int id)
{
    var product = _productService.GetProductById(id);
    return Ok(product);
}
```

On évite de mettre des `try/catch` partout.
La gestion des erreurs reste centralisée.

---

# 12. Bonus : erreurs 404 et autres codes HTTP

`UseExceptionHandler()` gère les **exceptions non gérées**.
Mais pour les réponses comme **404 Not Found** sans corps, ASP.NET Core peut aussi générer une réponse standard avec `UseStatusCodePages()` combiné à `AddProblemDetails()`. ([Microsoft Learn][1])

Exemple :

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

app.UseStatusCodePages();

app.MapControllers();

app.Run();
```

Ainsi :

* exception non gérée → ProblemDetails
* 404 sans corps → ProblemDetails
* 400/500 → format cohérent

([Microsoft Learn][1])

---

# 13. Résumé

Avant :

* erreurs non gérées
* réponses incohérentes
* middleware maison ou `try/catch` partout

Après :

* erreurs centralisées
* middleware natif ASP.NET Core
* réponses standardisées
* code plus propre

---

# 14. Schéma mental

```text
UseExceptionHandler() = middleware global officiel
IExceptionHandler     = logique personnalisée
AddProblemDetails()   = format propre pour les erreurs API
```

---

# 15. Conclusion

Avec l’approche moderne en **.NET 9** :

* l’API devient plus robuste
* tu évites la répétition
* tu utilises le middleware officiel
* tu renvoies des erreurs propres au format standard

En clair :

**on ne crée plus forcément son propre `ExceptionMiddleware`**

on s’appuie sur :

* `UseExceptionHandler()`
* `IExceptionHandler`
* `ProblemDetails`

## Docs
[1]: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0 "Handle errors in ASP.NET Core APIs | Microsoft Learn"
[2]: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0&utm_source=chatgpt.com "Handle errors in ASP.NET Core APIs"
[3]: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0&utm_source=chatgpt.com "Handle errors in ASP.NET Core"
[4]: https://learn.microsoft.com/fr-fr/dotnet/api/microsoft.aspnetcore.diagnostics.iexceptionhandler?view=aspnetcore-9.0&utm_source=chatgpt.com "IExceptionHandler Interface (Microsoft.AspNetCore. ..."
[5]: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0 "Handle errors in ASP.NET Core | Microsoft Learn"
