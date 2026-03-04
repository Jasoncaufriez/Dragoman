# Dragoman — Configuration de déploiement et procédure complète

---

## 1. Variables d'environnement

### 1.1 ASP.NET Core

| Variable | Valeur dev | Valeur prod | Définie dans |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Production` | `launchSettings.json` (dev), variable d'env système ou `web.config` (prod) |
| `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES` | `Microsoft.AspNetCore.SpaProxy` | *(absente)* | `launchSettings.json` uniquement |

**En production**, `ASPNETCORE_ENVIRONMENT` doit valoir `Production` pour que `appsettings.Production.json` soit chargé (surcharge de la connection string). Ce n'est **pas défini explicitement** dans le `web.config` publié — il est hérité de la variable d'environnement système Windows ou du pool IIS.

### 1.2 Angular

| Variable | Valeur dev | Valeur prod | Définie dans |
|---|---|---|---|
| `environment.production` | `false` | `true` | `environment.ts` / `environment.prod.ts` |
| `environment.apiUrl` | `/api` | `/api` | identique — substitution via `fileReplacements` dans `angular.json` |

Aucune variable d'environnement OS n'est nécessaire côté Angular. Le build prod (`ng build --configuration production`) utilise automatiquement `environment.prod.ts`.

### 1.3 Variables spécifiques à des fonctionnalités

| Clé `appsettings` | Valeur par défaut | Usage |
|---|---|---|
| `AdStatus:CsvPath` | `D:\Dragoman\Data\AD_Users.csv` | Chemin du fichier CSV Active Directory lu par `AdStatusController` |

Ce chemin est hardcodé dans le fallback du contrôleur :
```csharp
_csvPath = configuration.GetValue<string>("AdStatus:CsvPath")
          ?? @"D:\Dragoman\Data\AD_Users.csv";
```

Le fichier de persistance JSON est dérivé automatiquement :
```
D:\Dragoman\Data\adstatus_persistence.json
```

---

## 2. Chaînes de connexion

### 2.1 Développement — `appsettings.json` + `appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=LAURENTIDE;User ID=DRAGOMAN;Password=InterTolk;"
  }
}
```

- **TNS Name** : `LAURENTIDE` — résolu via `tnsnames.ora` sur le poste de développement
- **Auth** : Login/mot de passe Oracle en clair
- **Driver** : Oracle Managed Data Access (ODP.NET Core)

### 2.2 Production — `appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "User Id=DRAGOMAN;Password=InterTolk;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=10.4.4.22)(PORT=1529))(CONNECT_DATA=(SID=CCE11g)))"
  }
}
```

- **Connexion directe** : pas de TNS, descriptor Oracle complet inline
- **Host** : `10.4.4.22` (IP serveur Oracle interne)
- **Port** : `1529` (port non standard — Oracle par défaut est 1521)
- **SID** : `CCE11g`
- **Auth** : Login/mot de passe identiques au dev (`DRAGOMAN`/`InterTolk`)

### 2.3 ?? Problème de sécurité

Le mot de passe Oracle est en **clair** dans les trois fichiers `appsettings*.json` **et** dans le dossier `publish/` commité dans le dépôt Git. Le fichier `appsettings.Production.json` contient l'IP du serveur Oracle de production.

**Fichiers exposés dans le repo** :
```
Dragoman.Server\appsettings.json                          ? mot de passe en clair
Dragoman.Server\appsettings.Development.json               ? mot de passe en clair
Dragoman.Server\appsettings.Production.json                ? mot de passe + IP prod en clair
Dragoman.Server\publish\appsettings.json                   ? mot de passe en clair
Dragoman.Server\publish\appsettings.Development.json       ? commité
Dragoman.Server\publish\publish\appsettings.json           ? doublon imbriqué
Dragoman.Server\publish\publish\publish\appsettings.json   ? triplon imbriqué
```

### 2.4 Enregistrement dans `Program.cs`

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("DefaultConnection")));
```

Le provider Oracle EF Core (`Oracle.EntityFrameworkCore 8.23.60`) utilise `Oracle.ManagedDataAccess.Core 23.6.0` (driver managed .NET, pas de client Oracle natif requis).

---

## 3. Authentification Windows (NTLM)

### 3.1 Architecture

```
Navigateur (IE/Edge/Chrome intranet)
    ?  NTLM handshake (401 ? Authorization: NTLM base64)
    ?
IIS (Windows Authentication activée)
    ?  Identité Windows transmise au processus ASP.NET Core
    ?
ASP.NET Core (in-process)
    ?  User.Identity.Name = "DOMAIN\username"
    ?
Contrôleurs ([Authorize] ou User?.Identity?.Name)
```

### 3.2 Configuration IIS — `web.config`

```xml
<security>
  <authentication>
    <anonymousAuthentication enabled="true" />
    <windowsAuthentication enabled="true" useKernelMode="true">
      <providers>
        <clear />
        <add value="NTLM" />
      </providers>
      <extendedProtection tokenChecking="None" />
    </windowsAuthentication>
  </authentication>
</security>
```

**Dual-auth** : l'anonyme ET la Windows Auth sont activés simultanément.
- Les endpoints **sans** `[Authorize]` ? accessible en anonyme
- Les endpoints **avec** `[Authorize]` ? IIS déclenche le challenge NTLM (401)
- `useKernelMode="true"` ? authentification dans le noyau Windows (plus performant)
- `NTLM` uniquement (pas de Negotiate/Kerberos) — compatibilité maximale intranet

### 3.3 Configuration ASP.NET Core — `Program.cs`

```csharp
builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();
// ...
app.UseAuthentication();
app.UseAuthorization();
```

`IISDefaults.AuthenticationScheme` (`"Windows"`) = délègue l'authentification à IIS en mode in-process. ASP.NET Core ne gère pas directement le handshake NTLM.

### 3.4 Endpoints protégés

| Endpoint | Attribut | Rôle requis |
|---|---|---|
| `GET /api/auth/whoami` | `[Authorize]` | Tout utilisateur Windows authentifié |
| `GET/POST /api/adstatus/*` | `[Authorize(Roles = @"INTRRDM01\gg_rol_SystemAdministrator")]` | Groupe AD spécifique uniquement |
| Tous les autres endpoints | *(aucun)* | Accessibles en anonyme |

### 3.5 Configuration dev — `launchSettings.json`

```json
"iisSettings": {
  "windowsAuthentication": true,
  "anonymousAuthentication": false,
  "iisExpress": {
    "applicationUrl": "http://localhost:1306",
    "sslPort": 0
  }
}
```

**?? Incohérence** : en dev IIS Express, `anonymousAuthentication: false` — tous les endpoints exigent une auth Windows. En production, l'anonyme est activé (`true` dans `web.config`).

### 3.6 Fallback `X-Remote-User`

```csharp
// AuthController.WhoAmI()
var header = Request.Headers["X-Remote-User"].FirstOrDefault();
```

Si le header `X-Remote-User` est présent (injecté par un reverse proxy Apache/Nginx en amont), il est utilisé comme fallback pour identifier l'utilisateur.

```csharp
// UserController.GetCurrentUser()
var login = Request.Headers["X-Remote-User"].ToString();
```

---

## 4. CORS

### 4.1 Configuration

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "http://rvv-ccesrv21")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

app.UseCors("AllowAngularApp");
```

### 4.2 Origines autorisées

| Origine | Environnement | Usage |
|---|---|---|
| `http://localhost:4200` | Développement | Angular CLI dev server |
| `http://rvv-ccesrv21` | Production | Nom NetBIOS du serveur IIS de production |

### 4.3 Remarques

- `AllowCredentials()` ? nécessaire pour le handshake NTLM (`withCredentials: true` côté Angular)
- `AllowAnyHeader()` + `AllowAnyMethod()` ? pas de restriction sur les verbes HTTP ni les headers
- **HTTP uniquement** — pas de HTTPS configuré. Le serveur de production écoute en HTTP (`http://rvv-ccesrv21`)
- **?? Pas d'origine HTTPS** ? si un reverse proxy (Apache, F5) redirige vers HTTPS, les requêtes CORS seront bloquées

---

## 5. Configuration IIS

### 5.1 Hosting model

```xml
<aspNetCore processPath="dotnet"
            arguments=".\Dragoman.Server.dll"
            hostingModel="inprocess"
            stdoutLogEnabled="true"
            stdoutLogFile=".\logs\stdout" />
```

| Paramètre | Valeur | Signification |
|---|---|---|
| `hostingModel` | `inprocess` | ASP.NET Core s'exécute dans le processus IIS (`w3wp.exe`). Plus performant que `outofprocess` |
| `processPath` | `dotnet` | L'exécutable .NET est trouvé via le PATH système |
| `arguments` | `.\Dragoman.Server.dll` | DLL d'entrée de l'application |
| `stdoutLogEnabled` | `true` | Logs stdout/stderr écrits dans `.\logs\stdout_*.log` |
| `stdoutLogFile` | `.\logs\stdout` | Préfixe du fichier de log — IIS ajoute la date et le PID |

### 5.2 Handler ASP.NET Core Module V2

```xml
<handlers>
  <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
</handlers>
```

Toutes les requêtes (`*`) sont routées vers ASP.NET Core Module V2. Pas de contenu statique servi directement par IIS (les fichiers Angular sont servis par ASP.NET Core via le middleware static files).

### 5.3 Prérequis IIS

| Composant | Requis | Vérification |
|---|---|---|
| IIS 10+ | ? | Rôle Windows Server `Web-Server` |
| ASP.NET Core Module V2 (ANCM) | ? | Installé avec le `.NET 8 Hosting Bundle` |
| Windows Authentication (IIS feature) | ? | Rôle `Web-Windows-Auth` |
| Anonymous Authentication (IIS feature) | ? | Rôle `Web-Default-Doc` (inclus par défaut) |

### 5.4 Configuration du pool d'applications IIS

| Paramètre | Valeur recommandée | Raison |
|---|---|---|
| .NET CLR Version | `No Managed Code` | ASP.NET Core in-process utilise son propre runtime |
| Pipeline Mode | `Integrated` | Requis pour ANCM |
| Identity | `ApplicationPoolIdentity` ou compte de service | Le compte doit avoir accès au réseau Oracle (port 1529) et au partage `D:\Dragoman\Data\` |
| Enable 32-bit Applications | `False` | .NET 8 est 64-bit |
| Load User Profile | `True` | Nécessaire pour le certificat SSL dev et les clés de protection des données |

---

## 6. Profils de publication

### 6.1 FolderProfile — Publication pour serveur

```xml
<!-- Properties\PublishProfiles\FolderProfile.pubxml -->
<PublishUrl>C:\EX_DRAGO</PublishUrl>
<WebPublishMethod>FileSystem</WebPublishMethod>
<LastUsedBuildConfiguration>Release</LastUsedBuildConfiguration>
<DeleteExistingFiles>true</DeleteExistingFiles>
<SelfContained>false</SelfContained>
<TargetFramework>net8.0</TargetFramework>
```

- **Destination** : `C:\EX_DRAGO` — dossier local de staging
- **Framework-dependent** (`SelfContained=false`) — .NET 8 Runtime requis sur le serveur
- **DeleteExistingFiles** : `true` — le dossier de destination est vidé avant publication

### 6.2 FolderProfile1 — Publication de test

```xml
<!-- Properties\PublishProfiles\FolderProfile1.pubxml -->
<PublishUrl>C:\Users\jason\Desktop\dragoman</PublishUrl>
<DeleteExistingFiles>false</DeleteExistingFiles>
```

Publication vers le bureau du développeur (test local).

### 6.3 Dossier `publish/` commité

Le repo contient un dossier `Dragoman.Server\publish\` avec une copie complète du dernier build publié, incluant :
- Toutes les DLL compilées
- Les fichiers `appsettings*.json` (avec mots de passe)
- Le `web.config` de production
- Les DLL de localisation Humanizer (40+ langues)
- Des sous-dossiers `publish\publish\` et `publish\publish\publish\` (publications imbriquées accidentelles)

---

## 7. Dépendances système requises

### 7.1 Serveur de production (Windows Server)

| Composant | Version | Usage |
|---|---|---|
| **Windows Server** | 2016+ | OS serveur |
| **.NET 8 Runtime** | 8.0.x | Exécution de l'application (`SelfContained=false`) |
| **.NET 8 ASP.NET Core Runtime** | 8.0.x | Middleware web |
| **.NET 8 Hosting Bundle** | 8.0.x | Installe le runtime + ANCM V2 pour IIS |
| **IIS 10+** | — | Serveur web |
| **Oracle Database** | 11g (SID=CCE11g) | Base de données de production |

**Pas de client Oracle natif requis** : le driver `Oracle.ManagedDataAccess.Core 23.6.0` est fully managed et inclus dans le publish.

### 7.2 Packages NuGet — Backend

| Package | Version | Usage |
|---|---|---|
| `Oracle.EntityFrameworkCore` | 8.23.60 | Provider EF Core pour Oracle |
| `Oracle.ManagedDataAccess.Core` | 23.6.0 | Driver Oracle ADO.NET managed |
| `Microsoft.EntityFrameworkCore` | 8.0.10 | ORM |
| `QuestPDF` | 2025.7.4 | Génération des factures PDF |
| `ClosedXML` | 0.105.0 | Génération des rapports Excel |
| `DocumentFormat.OpenXml` | 3.3.0 | Génération des rapports Word |
| `AutoMapper` | 15.0.1 | Mapping entités ? DTOs |
| `Swashbuckle.AspNetCore` | 6.4.0 | Swagger UI (dev uniquement) |
| `Microsoft.AspNetCore.Authentication.Negotiate` | 8.0.8 | Support Negotiate/NTLM |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.10 | **Non utilisé** — référencé mais pas configuré |
| `Microsoft.AspNetCore.SpaProxy` | 8.x | Proxy dev Angular ? Kestrel |

### 7.3 Packages npm — Frontend

| Package | Version | Usage |
|---|---|---|
| `@angular/core` | ^17.3.0 | Framework frontend |
| `@angular/cli` | ^17.3.11 | Build tools |
| `rxjs` | ~7.8.0 | Programmation réactive |
| `zone.js` | ~0.14.3 | Change detection Angular |
| `bootstrap` | ^5.3.3 | **Référencé** dans `package.json` mais pas dans le build prod (`angular.json`) |
| `typescript` | ~5.4.2 | Compilateur TypeScript |

### 7.4 Dépendances réseau

| Ressource | Protocole | Port | Direction |
|---|---|---|---|
| Oracle DB (`10.4.4.22`) | TCP | 1529 | Serveur IIS ? Oracle |
| Google Fonts (`fonts.googleapis.com`) | HTTPS | 443 | Client navigateur ? Internet |
| Domaine AD `INTRRDM01` | LDAP/Kerberos | 389/88 | Serveur IIS ? contrôleur de domaine |

**?? Google Fonts** : la page `index.html` charge la police Inter depuis `fonts.googleapis.com`. Si le poste client n'a pas accès à Internet, la police ne se charge pas (fallback sur `"Segoe UI", system-ui`).

### 7.5 Fichiers système requis sur le serveur

| Fichier | Chemin | Producteur | Usage |
|---|---|---|---|
| `AD_Users.csv` | `D:\Dragoman\Data\AD_Users.csv` | Script PowerShell externe (tâche planifiée) | Données Active Directory pour le module AdStatus |
| `adstatus_persistence.json` | `D:\Dragoman\Data\adstatus_persistence.json` | Créé automatiquement par l'application | Persistance des commentaires et statuts "Normal" AdStatus |
| Dossier `logs\` | Relatif à la racine du site IIS | Créé par ANCM | Logs stdout de l'application |

---

## 8. Procédure de déploiement complète

### 8.1 Prérequis (une seule fois)

```powershell
# 1. Installer le .NET 8 Hosting Bundle sur le serveur
# Télécharger depuis https://dotnet.microsoft.com/download/dotnet/8.0
# Exécuter l'installeur ? installe Runtime + ASP.NET Core Runtime + ANCM V2

# 2. Activer les features IIS requises
Install-WindowsFeature Web-Server, Web-Windows-Auth, Web-Asp-Net45

# 3. Vérifier ANCM V2
Get-WebGlobalModule | Where-Object { $_.Name -like "*AspNetCore*" }

# 4. Créer le dossier de données AD
New-Item -ItemType Directory -Path "D:\Dragoman\Data" -Force

# 5. Créer le site IIS
New-IISSite -Name "Dragoman" -PhysicalPath "C:\inetpub\Dragoman" -BindingInformation "*:80:rvv-ccesrv21"

# 6. Configurer le pool d'applications
Set-ItemProperty "IIS:\AppPools\Dragoman" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty "IIS:\AppPools\Dragoman" -Name "processModel.loadUserProfile" -Value $true
```

### 8.2 Build Angular (depuis le poste de développement)

```powershell
cd dragoman.client

# Installer les dépendances
npm ci

# Build production
npx ng build --configuration production

# Résultat dans : dragoman.client\dist\dragoman.client\
```

### 8.3 Publish .NET (depuis Visual Studio ou CLI)

**Option A — Visual Studio** :
1. Clic droit sur `Dragoman.Server` ? Publier
2. Sélectionner le profil `FolderProfile`
3. Publier ? sortie dans `C:\EX_DRAGO`

**Option B — CLI** :
```powershell
cd Dragoman.Server

dotnet publish -c Release -o C:\EX_DRAGO /p:EnvironmentName=Production
```

### 8.4 Vérifier le contenu publié

```
C:\EX_DRAGO\
??? Dragoman.Server.dll           ? point d'entrée
??? Dragoman.Server.deps.json
??? Dragoman.Server.runtimeconfig.json
??? web.config                    ? configuration IIS
??? appsettings.json              ? config de base
??? appsettings.Production.json   ? connection string Oracle prod
??? wwwroot\                      ? fichiers Angular compilés
?   ??? index.html
?   ??? main.*.js
?   ??? polyfills.*.js
?   ??? styles.*.css
?   ??? ...
??? Oracle.ManagedDataAccess.dll  ? driver Oracle managed
??? QuestPDF.dll                  ? génération PDF
??? ClosedXML.dll                 ? génération Excel
??? logs\                         ? créé automatiquement
??? ... (autres DLL)
```

### 8.5 Copier vers le serveur de production

```powershell
# Arrêter le site IIS avant la copie
Stop-IISSite -Name "Dragoman" -Confirm:$false

# Copier les fichiers
robocopy "C:\EX_DRAGO" "\\rvv-ccesrv21\C$\inetpub\Dragoman" /MIR /XD logs

# Redémarrer le site
Start-IISSite -Name "Dragoman"
```

### 8.6 Configurer la variable d'environnement

Sur le serveur de production, s'assurer que `ASPNETCORE_ENVIRONMENT=Production` est défini :

**Option A** — Variable d'environnement système :
```powershell
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
```

**Option B** — Dans le `web.config` (méthode recommandée) :
```xml
<aspNetCore processPath="dotnet" arguments=".\Dragoman.Server.dll" hostingModel="inprocess">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  </environmentVariables>
</aspNetCore>
```

**?? Actuellement non configuré** dans le `web.config` publié. Si la variable système n'est pas définie, l'application démarre en mode `Production` par défaut (comportement ASP.NET Core), mais c'est implicite et fragile.

### 8.7 Vérifications post-déploiement

```powershell
# 1. Vérifier que le site répond
Invoke-WebRequest -Uri "http://rvv-ccesrv21/api/auth/whoami" -UseDefaultCredentials

# 2. Vérifier la connexion Oracle (via les logs stdout)
Get-Content "C:\inetpub\Dragoman\logs\stdout_*.log" -Tail 50

# 3. Vérifier le dashboard
Start-Process "http://rvv-ccesrv21"

# 4. Vérifier Swagger (uniquement si ASPNETCORE_ENVIRONMENT=Development)
# http://rvv-ccesrv21/swagger  ? 404 en production (normal)
```

---

## 9. Diagramme de déploiement

```
??????????????????????????????????????????????????????????????????????
?                    POSTE DÉVELOPPEUR                               ?
?                                                                    ?
?  Visual Studio 2022          Angular CLI                           ?
?  ????????????????????        ????????????????????                  ?
?  ? Dragoman.Server   ?        ? dragoman.client   ?                 ?
?  ? dotnet run :5171  ????????? ng serve :4200    ?                  ?
?  ? (Kestrel)         ? proxy  ? (Webpack DevServer)?                ?
?  ????????????????????        ????????????????????                  ?
?           ?                                                        ?
?           ?                                                        ?
?  Oracle "LAURENTIDE" (dev TNS)                                     ?
??????????????????????????????????????????????????????????????????????

                  dotnet publish / ng build
                           ?
                           ?

??????????????????????????????????????????????????????????????????????
?                    SERVEUR PRODUCTION                               ?
?                    rvv-ccesrv21 (Windows Server)                    ?
?                                                                    ?
?  ????????????????????????????????????????????                      ?
?  ?            IIS 10 (:80)                   ?                     ?
?  ?  Site: Dragoman                           ?                     ?
?  ?  Pool: No Managed Code, Integrated        ?                     ?
?  ?  Auth: Anonymous + Windows (NTLM)         ?                     ?
?  ?                                           ?                     ?
?  ?  ???????????????????????????????????????  ?                     ?
?  ?  ?  ANCM V2 (in-process)              ?  ?                     ?
?  ?  ?  ?????????????????????????????????  ?  ?                     ?
?  ?  ?  ?  ASP.NET Core 8.0            ?  ?  ?                     ?
?  ?  ?  ?  Dragoman.Server.dll         ?  ?  ?                     ?
?  ?  ?  ?  ??? /api/*   ? Controllers  ?  ?  ?                     ?
?  ?  ?  ?  ??? /*       ? wwwroot/     ?  ?  ?                     ?
?  ?  ?  ?       (Angular SPA)          ?  ?  ?                     ?
?  ?  ?  ?????????????????????????????????  ?  ?                     ?
?  ?  ???????????????????????????????????????  ?                     ?
?  ????????????????????????????????????????????                      ?
?           ?                              ?                         ?
?           ?                              ?                         ?
?  Oracle 11g (10.4.4.22:1529)    D:\Dragoman\Data\                  ?
?  SID=CCE11g                     AD_Users.csv                       ?
?  User=DRAGOMAN                  adstatus_persistence.json          ?
??????????????????????????????????????????????????????????????????????
```

---

## 10. Synthèse des problèmes de déploiement identifiés

| Problème | Sévérité | Détail |
|---|---|---|
| **Mots de passe en clair dans Git** | ?? Critique | `appsettings*.json` contiennent le login/mdp Oracle + IP prod, commités dans le repo public GitHub |
| **Dossier `publish/` commité** | ?? Critique | Le dossier `Dragoman.Server\publish\` contient une copie complète des binaires + configs avec mots de passe, publiée sur GitHub |
| **Publications imbriquées** | ?? Moyen | `publish\publish\` et `publish\publish\publish\` — publications accidentelles, augmentent la taille du repo de ~100 Mo inutilement |
| **ASPNETCORE_ENVIRONMENT non explicite** | ?? Moyen | Ni dans `web.config` ni en variable système documentée — risque de démarrer avec le mauvais `appsettings` |
| **HTTP uniquement, pas de HTTPS** | ?? Moyen | Les credentials NTLM transitent en HTTP sur le réseau interne |
| **Swagger exposé si env=Development** | ?? Moyen | Si `ASPNETCORE_ENVIRONMENT` n'est pas correctement à `Production`, Swagger est accessible publiquement |
| **Google Fonts externe** | ?? Faible | Dépendance réseau Internet pour charger la police Inter |
| **Package JwtBearer inutilisé** | ?? Faible | `Microsoft.AspNetCore.Authentication.JwtBearer` est référencé mais jamais configuré |
| **`CredentialsInterceptor` Angular non enregistré** | ?? Moyen | L'interceptor qui ajoute `withCredentials: true` existe mais n'est pas branché — certains appels API n'envoient pas les credentials NTLM |
