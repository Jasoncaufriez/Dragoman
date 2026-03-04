# Dragoman — Architecture technique frontend (Angular 17)

---

## 1. Modèle de compilation et configuration

### Builder
```
@angular-devkit/build-angular:browser  (Webpack — pas encore esbuild/application builder)
```
Le projet utilise le **legacy Webpack builder**, pas le nouveau builder `application` introduit en Angular 17. Ce choix est cohérent avec le maintien de `NgModule` (voir §2).

### Schematics configurés (`angular.json`)
```json
"@schematics/angular:component": { "standalone": false }
"@schematics/angular:directive":  { "standalone": false }
"@schematics/angular:pipe":       { "standalone": false }
```
Par défaut, tout nouveau composant généré par la CLI est **déclaré dans un module** (non-standalone). Exception : `HDComponent`, `HDPrestationJourComponent`, `HDRecapSemaineComponent` sont standalone (décisions ponctuelles).

### Environnements
```typescript
// environment.ts (dev) et environment.prod.ts (prod)
export const environment = {
  production: false,  // true en prod
  apiUrl: '/api'      // identique dans les deux
};
```
La valeur d'`apiUrl` est `/api` dans les deux environnements. Le fichier `environment.prod.ts` est substitué au build via `fileReplacements`. En pratique la distinction dev/prod n'apporte aucune différence de comportement ici.

### Proxy de développement (`src/proxy.conf.js`)
```javascript
{ context: ["/api", "/weatherforecast"],
  target: "http://localhost:5171",
  secure: false, changeOrigin: true }
```
Toutes les requêtes `/api/*` sont proxifiées vers l'API ASP.NET Core sur le port `5171`. Configuré dans `angular.json` ? `serve.options.proxyConfig`.

### Bootstrap
```typescript
// main.ts
platformBrowserDynamic().bootstrapModule(AppModule)
```
Bootstrap classique par `NgModule`. Pas de `bootstrapApplication` (mode standalone).

---

## 2. Module unique — `AppModule`

L'application entière est contenue dans **un seul module** (`AppModule`). Pas de feature modules, pas de shared module, pas de core module.

### Imports de modules Angular
```
BrowserModule         — composants de base (NgIf, NgFor…)
RouterModule          — directives router (routerLink, routerLinkActive)
AppRoutingModule      — configuration des routes
FormsModule           — ngModel (template-driven forms)
ReactiveFormsModule   — FormGroup, FormBuilder, FormControl
HttpClientModule      — HttpClient
```

### Composants déclarés (19)
```
AppComponent
NavbarComponent / NavbarInterComponent
DashboardComponent
CalendarComponent
InterpreteListComponent / InterpreteDetailComponent
AdressesComponent / LanguesComponent / TvaComponent / IndispoComponent
InterpreteAudiencesComponent / ConvocationComponent
PresenceInterpretesComponent / PrestationsComponent
FacturesComponent / GenerationFacturesComponent
InventoryComponent
AdStatusDashboardComponent
```

### Composants **non déclarés** dans AppModule (standalone)
```
HDComponent                  — standalone, importé via route lazy
HDPrestationJourComponent    — standalone, lazy loaded
HDRecapSemaineComponent      — standalone, lazy loaded
```

### Absence notable
- Pas de `HTTP_INTERCEPTORS` déclaré dans `AppModule`. L'interceptor `CredentialsInterceptor` existe dans `src/app/core/` mais **n'est pas enregistré** dans le module.
- Pas d'`APP_INITIALIZER` déclaré. La méthode `warmup()` de `AuthentificationService` existe mais n'est pas appelée au démarrage.

---

## 3. Routing

### Configuration (`AppRoutingModule`)

```typescript
RouterModule.forRoot(routes, {
  scrollPositionRestoration: 'enabled'
})
```

`scrollPositionRestoration: 'enabled'` : restaure la position de scroll à la navigation (nécessaire car certains composants ont des tableaux longs).

### Tableau des routes

| Path | Composant | Type |
|---|---|---|
| `` (empty) | ? redirect `dashboard` | redirect |
| `dashboard` | `DashboardComponent` | eager |
| `calendar` | `CalendarComponent` | eager |
| `factures` | `FacturesComponent` | eager |
| `generation-factures` | `GenerationFacturesComponent` | eager |
| `interpretes` | `InterpreteListComponent` | eager |
| `interpretes/:tolkcode/detail` | `InterpreteDetailComponent` | eager |
| `interpretes/:tolkcode/audiences` | `InterpreteAudiencesComponent` | eager |
| `interpretes/:tolkcode/convocation` | `ConvocationComponent` | eager |
| `interpretes/:tolkcode/adresses` | `AdressesComponent` | eager |
| `interpretes/:tolkcode/langues` | `LanguesComponent` | eager |
| `interpretes/:tolkcode/tva` | `TvaComponent` | eager |
| `interpretes/:tolkcode/indispo` | `IndispoComponent` | eager |
| `presence-interpretes` | `PresenceInterpretesComponent` | eager |
| `prestations` | `PrestationsComponent` | eager |
| `hd` | `HDComponent` | eager (standalone) |
| `hd/fiche-jour` | `HDPrestationJourComponent` | **lazy** (`loadComponent`) |
| `hd/recap-semaine` | `HDRecapSemaineComponent` | **lazy** (`loadComponent`) |
| `ad-status` | `AdStatusDashboardComponent` | eager |
| `globalprotect` | `InventoryComponent` | eager |
| `**` | ? redirect `dashboard` | wildcard |

### Lazy loading

Seuls les deux sous-routes HD utilisent `loadComponent` (lazy loading de composant standalone). Tous les autres composants sont chargés de manière **eager** au démarrage de l'application.

### Paramètre de route `:tolkcode`

7 routes partagent le segment `/interpretes/:tolkcode/`. La `NavbarInterComponent` lit ce paramètre depuis `ActivatedRoute.snapshot.paramMap` pour charger les informations de l'interprète courant.

---

## 4. Interceptors

### `CredentialsInterceptor` (`src/app/core/credentials.interceptor.ts`)

```typescript
if (req.url.startsWith('/api/') || req.url.includes('://rvv-ccesrv21/')) {
  req = req.clone({ withCredentials: true });
}
return next.handle(req);
```

**Rôle** : ajoute automatiquement `withCredentials: true` sur toutes les requêtes vers `/api/*` et le serveur de production, pour que le navigateur transmette les credentials NTLM/Windows.

**?? Non enregistré** : ce fichier existe mais n'est pas déclaré dans `AppModule` via `HTTP_INTERCEPTORS`. L'interceptor est **inactif**. En compensation, certains services ajoutent `{ withCredentials: true }` manuellement :
- `CalendarService.getCalendarData()`
- `UserService.getCurrentUser()` et `addUserWindows()`
- `AuthentificationService.warmup()` et `getLogin()`

Les autres services (la majorité) n'envoient pas `withCredentials`, ce qui peut provoquer des échecs d'authentification NTLM sur des endpoints protégés.

---

## 5. Guards

**Aucun guard n'est défini** dans l'application. Il n'y a ni `CanActivate`, ni `CanDeactivate`, ni `CanLoad/CanMatch`. La route `ad-status` porte un `data: { title: '...' }` mais pas de guard de rôle côté Angular — la protection est uniquement côté API (`[Authorize(Roles="...")]` dans `AdStatusController`).

---

## 6. Services — pattern et organisation

### Emplacement

```
src/app/services/          — 14 services partagés (métier + infra)
src/app/ad-status/         — AdStatusService (co-localisé avec son composant)
src/app/hd/                — HDPrestationsService (co-localisé avec son module HD)
```

### Pattern d'injection

Tous les services utilisent `@Injectable({ providedIn: 'root' })` ? **singleton applicatif**, enregistré dans le root injector. Pas de scoping par module ou par composant.

### Catalogue des services

| Service | Pattern notable |
|---|---|
| `AuthentificationService` | `BehaviorSubject<string\|null>` en cache du login. `warmup()` retourne une `Promise<void>` pour APP_INITIALIZER (non branché). `getLogin()` utilise le cache si disponible |
| `InterpretesService` | Service "fourre-tout" : identité, langues, tolklink, recherche, match, convocations. Dépendance sur `environment.apiUrl` |
| `TolklinkService` | Service dédié aux opérations TOLKLINK (`addOne`, `addBulk`) — séparé d'`InterpretesService` pour clarté |
| `AdressesService` | Expose `replace()` (logique métier "clôture + crée") en plus des CRUD classiques |
| `LanguesService` | Charge sources et destinations en parallèle via `forkJoin` dans `LanguesComponent`. Paramètre `destOnly` en querystring |
| `IndispoService` | Seul service qui **injecte un autre service** (`AuthentificationService`). Enrichit le payload avec le login Windows via `switchMap` avant envoi |
| `TvaService` | Base URL `/api` sans constante d'environnement — URLs relatives hardcodées |
| `PaiementsService` | DTOs complets définis inline dans le service (pas dans `dtos/`) |
| `FacturesGenService` | DTOs et interfaces définis inline dans le service. Gère les téléchargements `blob` (PDF, EML) |
| `CalendarService` | Seul service à définir son interface DTO (`CalendarData`) inline |
| `DashboardService` | Contient des **mocks** commentés (`getResumeMock`, etc.) laissés en code de production. Agrégation côté client via `forkJoin` dans `loadResume()` |
| `ReportsService` | Pas de typage fort (retourne `any[]`). 3 méthodes de téléchargement blob (Excel, Word, PDF) |
| `InventoryService` | Upload `FormData` via `POST multipart/form-data` |
| `AdStatusService` | Co-localisé dans `ad-status/`. DTOs définis dans le même fichier |
| `HDPrestationsService` | En-tête `Cache-Control: no-cache` sur toutes les requêtes. Co-localisé dans `hd/` |
| `UserService` | Dépend de `environment.apiUrl`. Deux endpoints (`/current` et `/addUser`) |

---

## 7. DTOs et modèles

### Organisation

```
src/app/dtos/              — DTOs partagés entre composants
  ??? interprete-dto.model.ts   ? InterpreteSearchDto, AudienceDto, InterpreteMatchDto
  ??? indispo-dto.model.ts      ? IndispoRowDto, NewIndispoDto
  ??? tva-dto.model.ts          ? TvaRowDto, StatutDto, NewTvaDto

src/app/models/            — Modèles de domaine (entités plates)
  ??? ad-status.model.ts        ? AdUserStatus
  ??? machine-record.ts         ? MachineRecord

src/app/hd/
  ??? hd-prestations.model.ts   ? HDTicket, HDAutreTache, HDPrestationJour, HDSaveResultat

Définis inline dans les services :
  factures.service.ts     ? GenererFacturesRequest/Result, FactureListItem, UpdateStatutResult…
  paiements.service.ts    ? PaiementMoisInterpreteRowDto, PaiementMoisDetailDto…
  calendar.service.ts     ? CalendarData
  dashboard.service.ts    ? Resume, LangueToday, AudienceSupprimee…
```

**Incohérence** : certains DTOs sont dans `dtos/`, d'autres dans les services, d'autres dans `models/`. Il n'y a pas de convention uniforme.

### Typage

La grande majorité des appels HTTP est **fortement typée** (`http.get<T[]>`, `http.post<T>`). Exceptions :
- `InterpretesService.getIdentite()` retourne `Observable<Object>` (sans générique)
- `ReportsService.getInterpretes()` retourne `any[]`
- Plusieurs `saveIdentite(payload: any)` et handlers `.next((data: any) => ...)`

---

## 8. Structure des composants

### Pattern général

Tous les composants suivent le pattern **Smart Component** (pas de séparation Smart/Dumb). Chaque composant :
1. Injecte ses services directement
2. Gère son propre état local (propriétés de classe)
3. Appelle les services dans `ngOnInit` ou sur événement
4. Gère les erreurs en propriété locale (`error?: string`)

Il n'y a **pas de composants de présentation** (Dumb/Presentational). Pas de `@Input`/`@Output` entre composants sauf `NavbarInterComponent.tolkcode` (`@Input`).

### Formulaires

**ReactiveFormsModule** est utilisé partout pour les formulaires complexes :
- `FormBuilder` + `FormGroup` + `Validators` dans `InterpreteDetailComponent`, `IndispoComponent`, `InterpreteListComponent`, `PrestationsComponent`, `AdressesComponent`, `TvaComponent`, `CalendarComponent`
- `FormArray` dans `HDPrestationJourComponent` (tickets + autres tâches)

**FormsModule** (`ngModel`) est importé mais peu utilisé directement dans les composants principaux (présent pour compatibilité).

### Réactivité

`CalendarComponent` est le plus avancé côté réactivité :
```typescript
filteredRows$ = combineLatest([
  filterForm.valueChanges.pipe(startWith(...)),
  searchForm.valueChanges.pipe(startWith(...)),
  filterEmpty$.pipe(startWith(...)),
  excludeNoInterp$.pipe(startWith(...))
]).pipe(map(([f, s, e, n]) => this.applyFilters(...)))
```
Les autres composants utilisent des propriétés simples mutées dans les callbacks `subscribe`.

---

## 9. Styles — Design System

### Approche

**CSS personnalisé uniquement**. Pas de framework CSS (pas de Bootstrap en production — uniquement dans `karma.conf.js` pour les tests, pas dans `angular.json` build prod).

### Tokens CSS (`styles.css`)

```css
:root {
  --color-primary:      #2f80ed;   /* IBZ Civic Light blue */
  --color-background:   #f7f9fc;
  --color-surface:      #ffffff;
  --radius:             10px;
  --navbar-height:      54px;
  --layout-padding:     28px;      /* single source of truth alignement */
  --transition:         0.18s cubic-bezier(.4,0,.2,1);
  /* ... 40+ tokens */
}
```

### Composants CSS globaux définis dans `styles.css`

`.navbar--glass`, `.navbarinter`, `.card`, `.card--panel`, `.toolbar`, `.tabs`, `.btn`, `.btn--primary`, `.btn--danger`, `.btn--sm`, `.btn--xs`, `.table`, `.table-wrap`, `.badge`, `.alert--error`, `.link-btn`, `.link-btn--ghost`, `.link-btn--light`, `.fade-in`

### CSS scoped (composant)

Chaque composant a son `.css` scopé (`ViewEncapsulation.Emulated` par défaut). Les composants réutilisent les classes globales définies dans `styles.css` (`.card`, `.btn`, `.table`, etc.) et ajoutent leurs règles spécifiques dans leur fichier local.

### Police

`Inter` chargée depuis Google Fonts dans `index.html` (connexion réseau externe au démarrage).

---

## 10. Récapitulatif des patterns et anomalies

### Patterns utilisés

| Pattern | Présence |
|---|---|
| Module unique (`NgModule`) | ? Un seul `AppModule` |
| Service layer (`providedIn: 'root'`) | ? Tous les services |
| Reactive Forms | ? Tous les formulaires complexes |
| RxJS (`combineLatest`, `forkJoin`, `BehaviorSubject`, `switchMap`) | ? Utilisé dans plusieurs services/composants |
| Lazy loading (composants standalone) | ? Partiel (HD uniquement) |
| DTO typed interfaces | ? Partiel (mélange DTO/inline/any) |
| CSS Design Tokens (variables CSS) | ? Centralisé dans `styles.css` |
| Smart/Dumb components | ? Pas de séparation — tout smart |
| Feature Modules | ? Un seul module |
| Route Guards | ? Aucun |
| HTTP Interceptor actif | ? Écrit mais non enregistré |
| APP_INITIALIZER pour warmup NTLM | ? Méthode écrite mais non branchée |
| State management (NgRx, Signal) | ? État local uniquement |
| Signals Angular 17 | ? Non utilisés |
| Tests (Karma/Jasmine) | ? Configurés mais aucun spec écrit |
