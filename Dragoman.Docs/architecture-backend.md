# Dragoman — Architecture technique backend (ASP.NET Core .NET 8)

---

## 1. Structure des dossiers

```
Dragoman.Server/
??? Controllers/          — 16 controllers API (1 fichier = 1 controller)
??? Data/
?   ??? ApplicationDbContext.cs  — DbContext actif (utilisé par toute l'appli)
??? Dtos/                 — Data Transfer Objects (entrée/sortie API)
??? Mapping/
?   ??? MappingProfile.cs — Profil AutoMapper unique
??? Models/               — Entités EF Core + modèles legacy (voir §4)
??? Pdf/
?   ??? FacturesBatchPdfDocument.cs — Document QuestPDF (factures + notes de crédit)
??? Properties/
?   ??? launchSettings.json
??? publish/              — Artefacts de publication IIS (web.config inclus)
??? appsettings.json
??? appsettings.Development.json
??? appsettings.Production.json
??? Program.cs            — Point d'entrée unique (minimal hosting model)
```

**Observation importante** : le dossier `Models/` contient deux contextes EF :
- `ApplicationDbContext` — contexte **actif**, utilisé par tous les controllers
- `ModelContext` — contexte **legacy**, généré automatiquement par le scaffold Oracle (EF Scaffold-DbContext). Il n'est pas injecté dans le DI. Il contient une chaîne de connexion hardcodée (`OnConfiguring`) et mappe une trentaine d'entités/vues supplémentaires non utilisées en production. Il représente l'état initial du scaffold et n'a pas été supprimé.

---

## 2. Point d'entrée — `Program.cs`

Le projet utilise le **Minimal Hosting Model** introduit avec .NET 6. Pas de `Startup.cs`.

### Ordre d'enregistrement des services (`builder.Services`)

```
1. QuestPDF.Settings.License = Community          — Licence QuestPDF
2. AddDbContext<ApplicationDbContext>              — EF Core Oracle
3. AddCors("AllowAngularApp")                      — CORS
4. AddControllers()                                — MVC Controllers
5. AddEndpointsApiExplorer()                       — Support Swagger
6. AddSwaggerGen()                                 — Swagger UI
7. AddAutoMapper(MappingProfile assembly)          — AutoMapper
8. AddAuthentication(IISDefaults.AuthenticationScheme) — Windows Auth via IIS
9. AddAuthorization()                              — Autorisation
```

### Pipeline HTTP (`app.Use*`)

```
1. app.UseCors("AllowAngularApp")    — doit être avant UseAuthentication
2. app.UseSwagger()                  — en Development uniquement
3. app.UseSwaggerUI()                — en Development uniquement
4. app.UseAuthentication()           — traitement du token Windows/NTLM
5. app.UseAuthorization()            — contrôle des [Authorize]
6. app.MapControllers()              — routage attribut
```

**Middlewares absents** : pas de `UseHttpsRedirection`, pas de `UseStaticFiles`, pas de `UseRouting` explicite (il est implicite avec `MapControllers` en .NET 8), pas de gestion d'erreurs globale (`UseExceptionHandler`).

---

## 3. Configuration de l'authentification

### Mécanisme

Windows Authentication via IIS Integration (`Microsoft.AspNetCore.Authentication.Negotiate` + `IISDefaults`).

```csharp
builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);
```

### Configuration IIS (web.config — production)

```xml
<anonymousAuthentication enabled="true" />
<windowsAuthentication enabled="true" useKernelMode="true">
    <providers>
        <add value="NTLM" />
    </providers>
</windowsAuthentication>
```

Les deux modes sont activés simultanément :
- **Anonyme activé** ? les endpoints sans `[Authorize]` sont accessibles sans authentification
- **Windows activé** ? les endpoints avec `[Authorize]` déclenchent le challenge NTLM

### Comportement effectif par controller

| Controller | Protection | Mécanisme |
|---|---|---|
| `AuthController.WhoAmI` | `[Authorize]` | Challenge NTLM si non authentifié, retourne `User.Identity.Name` |
| `AdStatusController` | `[Authorize(Roles = "INTRRDM01\\gg_rol_SystemAdministrator")]` | Vérifie le groupe AD Windows |
| Tous les autres | Aucun `[Authorize]` | Accessibles à tout utilisateur du réseau interne sans authentification |

### Profil de développement (launchSettings.json)

```json
"iisSettings": {
  "windowsAuthentication": true,
  "anonymousAuthentication": false   // anonyme désactivé en dev IIS Express
}
```

Profil `http` (Kestrel) : pas de Windows Auth native — le header `X-Remote-User` est utilisé comme fallback dans `UserController` et `AuthController`.

---

## 4. Gestion EF Core

### DbContext actif : `ApplicationDbContext`

- Injection par constructeur (`DbContextOptions<ApplicationDbContext>`)
- Schéma Oracle : **DRAGOMAN** (implicite, pas déclaré avec `HasDefaultSchema` dans `ApplicationDbContext` — contrairement à `ModelContext`)
- Collation : non précisée dans `ApplicationDbContext`
- Toutes les entités sont mappées manuellement dans `OnModelCreating`

### Entités mappées

**Tables (avec clé primaire) :**

| Entité C# | Table Oracle | PK | Séquence |
|---|---|---|---|
| `Tolkidentity` | `TOLKIDENTITY` | `TOLKCODE` (int) | Manuelle (séquence appelée explicitement dans controller) |
| `Tolkadresse` | `TOLKADRESSE` | `ID_ADRESSE` (int) | `NR_AUTO_ADRESSE.NEXTVAL` via `HasDefaultValueSql` |
| `Langue` | `LANGUE` | `IDLANGUE` (byte) | `ValueGeneratedNever` |
| `LangueSource` | `LANGUE_SOURCE` | `ID_LANGUESOURCE` (int) | Manuelle (NEXTVAL en SQL brut dans controller) |
| `LangueDestination` | `LANGUE_DESTINATION` | `ID_LANGUEDESTINATION` (int) | Manuelle |
| `TolkTva` | `TOLK_TVA` | `ID_TOLK_TVA` (decimal) | Non définie dans ApplicationDbContext |
| `Statut` | `STATUT` | `ID_STATUT` (byte) | — |
| `Tolkindispo` | `TOLKINDISPO` | `ID_INDISPO` (int) | Non définie |
| `Tolklink` | `TOLKLINK` | `ID_TOLKLINK` (int) | `NR_AUTO_TOLKLINK.NEXTVAL` via `HasDefaultValueSql` |
| `Prestation` | `PRESTATION` | `ID_PRESTATION` (int) | `ID_PRESTATION_AUTO.NEXTVAL` via `HasDefaultValueSql` |
| `Paiement` | `PAIEMENT` | `ID_PAIEMENT` (int) | `NR_AUTO_PAIEMENT.NEXTVAL` via `HasDefaultValueSql` |
| `Facture` | `FACTURE` | `ID_FACTURE` (int) | `NR_AUTO_FACTURE.NEXTVAL` via `HasDefaultValueSql` |
| `Indexation` | `INDEXATION` | `ID_INDEX` (int) | — |

**Vues (keyless — `HasNoKey()`) :**

| Entité C# | Vue Oracle | Usage |
|---|---|---|
| `VueCalendarVrmPc` | `VUE_CALENDAR_ALL` | Calendrier principal (audiences VRM/PCS) |
| `VueCalendarAnn` | `VUE_CALENDAR_ANN` | Calendrier annulations |
| `ReportInterpreteRow` | `V_INTERPRETES_AUDIENCES_JOUR` | Rapports de présence |
| `VAudienceInterpreteDetail` | `V_AUDIENCE_INTERPRETE_DETAIL` | Dashboard + présence détaillée |

### Spécificités EF Core / Oracle

**Conversion bool ? NUMBER(1) globale** : appliquée automatiquement à toutes les propriétés `bool` et `bool?` via une boucle sur les entités au moment de `OnModelCreating` :

```csharp
property.SetValueConverter(
    new ValueConverter<bool?, int>(
        v => v.HasValue && v.Value ? 1 : 0,
        v => v == 1
    )
);
```

**Séquences déclarées** : `ID_PRESTATION_AUTO`, `NR_AUTO_PAIEMENT`, `NR_AUTO_TOLKLINK`, `NR_AUTO_ADRESSE` (dans `ApplicationDbContext`). `NR_AUTO_FACTURE` déclarée uniquement dans `ModelContext`.

**Séquences appelées manuellement** (SQL brut via `GetDbConnection()`) : pour `NR_AUTO_ADRESSE`, `NR_AUTO_LANGUE_SOURCE`, `NR_AUTO_DESTINATION`, `NR_TOLK` — car EF Core Oracle ne déclenche pas systématiquement `NEXTVAL` à l'insertion selon la version du provider.

**Mapping de colonne sensible à la casse** : `VAudienceInterpreteDetail` mappe des colonnes en casse mixte (`Tolkcode`, `Nom`, `Prenom`) mais `TAALROL` en majuscules — reflet de l'incohérence de casse dans la vue Oracle.

**Conflit de mapping EF** : `TOLKLINK`, `PRESTATION`, `PAIEMENT` et `FACTURE` sont configurés deux fois dans `ModelContext.OnModelCreating` (doublon probable dû à des fusions de branches). Dans `ApplicationDbContext`, ces entités n'ont qu'une seule configuration.

**Relations définies :**
- `Tolklink` ? `Prestation` : FK `ID_PRESTATION`, `DeleteBehavior.NoAction`
- `Prestation` ? `Paiement` : FK `ID_PAIEMENT`, `DeleteBehavior.NoAction`
- `Facture` ? `Paiement` (1-N) : FK `ID_FACTURE`, `DeleteBehavior.NoAction`

---

## 5. Configuration Oracle

### Chaînes de connexion

| Environnement | `Data Source` | Protocole |
|---|---|---|
| Development | `LAURENTIDE` (alias TNS local) | TNS Names |
| Production | `(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=10.4.4.22)(PORT=1529))(CONNECT_DATA=(SID=CCE11g)))` | EZConnect inline |

```json
// appsettings.Production.json
"DefaultConnection": "User Id=DRAGOMAN;Password=InterTolk;Data Source=(DESCRIPTION=...)"
```

**Credentials** : identiques en dev et prod (`DRAGOMAN` / `InterTolk`). Pas de rotation, pas de secrets managés (Azure Key Vault, User Secrets, etc.).

### Provider NuGet

```xml
<PackageReference Include="Oracle.EntityFrameworkCore" Version="8.23.60" />
<PackageReference Include="Oracle.ManagedDataAccess.Core" Version="23.6.0" />
```

- `Oracle.EntityFrameworkCore` : provider EF Core pour Oracle (Oracle officiel)
- `Oracle.ManagedDataAccess.Core` : driver ODP.NET managé utilisé pour les appels SQL bruts (`GetDbConnection().CreateCommand()`)

### Appels SQL bruts utilisés

Plusieurs controllers ouvrent directement la connexion ADO.NET pour exécuter des `NEXTVAL` ou des `INSERT INTO ... SELECT` complexes :

```csharp
var conn = _db.Database.GetDbConnection();
if (conn.State != ConnectionState.Open) await conn.OpenAsync();
using var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT NR_AUTO_ADRESSE.NEXTVAL FROM DUAL";
```

---

## 6. Services enregistrés dans le DI

| Service | Lifetime | Description |
|---|---|---|
| `ApplicationDbContext` | **Scoped** (défaut `AddDbContext`) | Contexte EF Core Oracle |
| `IMapper` (AutoMapper) | **Singleton** (défaut AutoMapper) | Mapping entités ? DTOs |
| `IAuthenticationService` | Singleton (framework) | Windows Auth / NTLM |
| `IAuthorizationService` | Singleton (framework) | Vérification des rôles AD |
| Controllers | **Transient** (par requête) | Instanciés par le framework MVC |

**Aucun service applicatif personnalisé** n'est enregistré dans le DI. Toute la logique métier est directement dans les controllers (pas de couche Service/Repository).

---

## 7. Configuration CORS

```csharp
options.AddPolicy("AllowAngularApp", policy =>
    policy
        .WithOrigins("http://localhost:4200", "http://rvv-ccesrv21")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
);
```

| Paramètre | Valeur | Impact |
|---|---|---|
| Origines autorisées | `localhost:4200` (dev Angular) + `rvv-ccesrv21` (serveur prod IBZ) | Aucune autre origine ne peut appeler l'API |
| `AllowCredentials()` | Activé | Nécessaire pour transmettre les cookies/credentials Windows |
| `AllowAnyHeader()` | Activé | Pas de restriction sur les en-têtes |
| `AllowAnyMethod()` | Activé | GET, POST, PUT, PATCH, DELETE autorisés |
| Politique appliquée | `app.UseCors("AllowAngularApp")` — **globale**, avant `UseAuthentication` | S'applique à tous les endpoints |

**Remarque** : `AllowCredentials()` + `AllowAnyOrigin()` est une combinaison interdite par le navigateur (CORS spec). L'utilisation de `WithOrigins(...)` explicite est correcte.

---

## 8. Logging

### Configuration

```json
// appsettings.json + appsettings.Development.json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

Le provider de logging utilisé est le **Console logger** par défaut du host ASP.NET Core générique (`WebApplication.CreateBuilder` inclut `Console` + `Debug` + `EventSource`).

### En production (IIS In-Process)

```xml
<aspNetCore stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
```

Les logs stdout sont redirigés vers `.\logs\stdout_*.log` sur le serveur IIS.

### Absence de logging applicatif

Aucun `ILogger<T>` n'est injecté dans les controllers. Les erreurs sont soit :
- Retournées directement au client (`return BadRequest(...)`, `return NotFound(...)`)
- Silencieusement ignorées (ex: `catch { /* silencieux */ }` dans `NavbarinterComponent` côté front, mais côté back les exceptions non catchées produiront une réponse 500 non loguée)

---

## 9. Paquets NuGet — résumé

| Package | Version | Usage |
|---|---|---|
| `Oracle.EntityFrameworkCore` | 8.23.60 | Provider EF Core Oracle |
| `Oracle.ManagedDataAccess.Core` | 23.6.0 | Driver ADO.NET Oracle (SQL brut) |
| `Microsoft.EntityFrameworkCore` | 8.0.10 | ORM principal |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.10 | Scaffold + migrations (dev) |
| `AutoMapper` | 15.0.1 | Mapping entités ? DTOs |
| `QuestPDF` | 2025.7.4 | Génération PDF (factures, présence) |
| `ClosedXML` | 0.105.0 | Export Excel (.xlsx) |
| `DocumentFormat.OpenXml` | 3.3.0 | Export Word (.docx) |
| `Microsoft.AspNetCore.Authentication.Negotiate` | 8.0.8 | Windows Auth / Kerberos |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.10 | **Installé mais non utilisé** |
| `Swashbuckle.AspNetCore` | 6.4.0 | Swagger UI (dev uniquement) |
| `Microsoft.AspNetCore.SpaProxy` | 8.*-* | Proxy dev vers Angular (npm start) |

**Paquet non utilisé** : `Microsoft.AspNetCore.Authentication.JwtBearer` est référencé dans le `.csproj` mais aucun `AddJwtBearer()` n'est présent dans `Program.cs`. Il s'agit probablement d'un résidu d'une piste explorée et abandonnée.

---

## 10. Intégration SPA (Angular)

```xml
<SpaRoot>..\dragoman.client</SpaRoot>
<SpaProxyLaunchCommand>npm start</SpaProxyLaunchCommand>
<SpaProxyServerUrl>https://localhost:4200</SpaProxyServerUrl>
```

En développement, `Microsoft.AspNetCore.SpaProxy` démarre automatiquement `npm start` dans le dossier `dragoman.client` et proxifie les requêtes vers `https://localhost:4200`. L'API ASP.NET Core écoute sur `http://localhost:5171` (profil `http` de `launchSettings.json`).

En production, le frontend Angular compilé est servi statiquement par IIS indépendamment. L'API est un module IIS In-Process (`AspNetCoreModuleV2`).
