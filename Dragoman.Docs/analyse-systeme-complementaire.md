# Dragoman — Analyse Système Complémentaire

> Document complémentaire au rapport LaTeX existant.
> Couvre exclusivement les informations techniques manquantes pour une analyse système exhaustive.
> Basé sur l'examen intégral du code source (commit courant, branche `main`).

---

## 1. Dictionnaire Détaillé des Composants Frontend (Angular 17)

### 1.1 Architecture Globale et Patterns Structurels

L'application Angular suit une architecture **monolithique modulaire** sans couche de state management (pas de NgRx/Akita). La totalité des composants métier sont déclarés dans un unique `AppModule` (pas de feature modules). Deux composants font exception en étant **standalone** et **lazy-loaded** : `HDPrestationJourComponent` et `HDRecapSemaineComponent`.

**Patterns observés :**

| Pattern | Application | Détail |
|---|---|---|
| Smart Components uniquement | Tous les composants | Aucun composant "Dumb" / presentational. Chaque composant injecte directement ses services, gère son état local et contient la logique métier |
| Reactive Forms | `InterpreteDetailComponent`, `CalendarComponent`, `PrestationsComponent`, `IndispoComponent`, `InterpreteListComponent`, `HDPrestationJourComponent` | Utilisation de `FormBuilder`, `FormGroup`, `FormArray`, `Validators`, `AbstractControl` |
| Template-driven Forms (ngModel) | `GenerationFacturesComponent`, `FacturesComponent`, `PresenceInterpretesComponent`, `InventoryComponent` | Coexistence des deux paradigmes de formulaires dans le même projet |
| `combineLatest` + `BehaviorSubject` | `CalendarComponent` | Pipeline réactif combinant 4 flux (filtres d'en-tête, recherche carte, filtre tolkcode vide, filtre exclusion) via `combineLatest` pour le filtrage temps réel |
| Appels API parallèles (`forkJoin`) | `LanguesComponent`, `NavbarInterComponent` | Chargement simultané de données indépendantes (langues source + destination, identité + langues) |
| Lazy loading standalone | `HDPrestationJourComponent`, `HDRecapSemaineComponent` | Seuls composants utilisant `loadComponent()` dans le routeur. Standalone imports : `CommonModule`, `ReactiveFormsModule`, `RouterModule` |
| Aucune gestion d'état centralisée | Global | Chaque composant gère son propre état via des propriétés de classe. Pas de store partagé |
| `HttpInterceptor` unique | `CredentialsInterceptor` | Ajoute `withCredentials: true` sur les requêtes vers `/api/` ou `rvv-ccesrv21` pour le handshake NTLM |

### 1.2 Fiches Composants Majeurs

#### `CalendarComponent`

| Attribut | Détail |
|---|---|
| **Rôle** | Vue calendrier global des audiences (union VRM + ANN). Assignation, désassignation, navigation vers la fiche interprète |
| **Formulaire** | `FormGroup` réactif avec 10 champs de filtre (dateFrom, dateTo, heure, langueRole, langueRequete, langueCgoe, salle, nom, proc, tolkcode) + `FormGroup` dédié recherche (nroRoleGen, idAffAudience) |
| **Réactivité** | `combineLatest([filterForm.valueChanges, searchForm.valueChanges, filterEmpty$, excludeNoInterp$])` ? pipeline `map()` vers `applyFilters()`. Toutes les données chargées en mémoire (pas de pagination serveur) |
| **Filtrage** | Côté client exclusivement. Normalisation `.toLowerCase().trim()` sur chaque champ. Filtrage par plage de dates ISO |
| **Modal assignation** | Chargement complet de la liste des interprètes (`listAllTolkcodes()`) lors de la première ouverture. Recherche locale par nom/prénom/tolkcode. Appel `addTolklink()` puis rechargement intégral |
| **Spécificité** | Le rechargement complet des données (`getCalendarData()`) est déclenché après chaque assignation/désassignation — pas de mise à jour optimiste |

#### `InterpreteDetailComponent`

| Attribut | Détail |
|---|---|
| **Rôle** | Fiche identité complète d'un interprète, formulaire accordéon en 6 sections collapsibles |
| **Formulaire** | `FormGroup` réactif avec 24 champs. Sections : Identité (5), Contact (3), Langue & Statut (6), Banque & TVA (4), Entreprise (4), Divers (4) |
| **Gestion téléphones** | Normalisation entrée/sortie : 3 champs DB (`gsm`, `tel`, `telbis`) ? 2 champs formulaire (`telephone1`, `telephone2`). À la sauvegarde, classification automatique GSM (04xx/+324xx) vs fixe via `isBelgianMobile()`, puis dédoublonnage via `uniq()` |
| **Gestion casse API** | Triple fallback sur chaque propriété : `data.nom ?? data.Nom ?? data.NOM` — compense l'inconsistance de casse entre les sérialiseurs Oracle/EF Core |
| **Ouverture sections** | Sections avancées (Entreprise, Divers) masquées par défaut. `openSectionsWithErrors()` ouvre automatiquement les sections contenant des erreurs de validation au chargement et à la soumission |
| **Helpers locaux** | Fonctions utilitaires déclarées en dehors de la classe : `sanitizePhone()`, `isBelgianMobile()`, `uniq()`, `toNum()`, `to01()`, `to12()`, `toIsoDate()` |

#### `ConvocationComponent`

| Attribut | Détail |
|---|---|
| **Rôle** | Génération d'un email de convocation HTML bilingue FR/NL pour un interprète |
| **Pipeline** | 1. Charge l'identité (`getIdentite`) ? 2. Charge les convocations validées ? 3. Charge les audiences disponibles ? 4. L'utilisateur sélectionne les audiences ? 5. Génération du HTML |
| **Construction HTML** | Méthode `buildMailHtml()` construit manuellement le HTML inline (styles CSS inline pour compatibilité email). Deux tableaux : audiences confirmées (en-tête rouge `#9E3039`) et nouvelles audiences (en-tête verte `#059669`) |
| **Copie presse-papier** | Double stratégie : 1) API `ClipboardItem` moderne (HTTPS/localhost) 2) Fallback `document.execCommand('copy')` via div `contentEditable` (fonctionne en HTTP) |
| **Envoi** | Ouverture du client mail natif via `window.location.href = mailto:` avec sujet pré-rempli. Le corps n'est pas passé via mailto (trop long) — l'utilisateur doit coller manuellement |

#### `PrestationsComponent`

| Attribut | Détail |
|---|---|
| **Rôle** | Encodage des heures de début/fin des prestations du jour |
| **Formulaire** | `FormGroup` réactif minimal (start, end) avec `Validators.required` |
| **Pré-remplissage** | Heure audience ? 15 minutes, via `subtractMinutes()`. Uniquement l'heure de début est pré-remplie |
| **Scroll UX** | `@ViewChild('formCard')` + `scrollIntoView({ behavior: 'smooth', block: 'start' })` — scroll automatique vers le formulaire quand un interprète est sélectionné |
| **Limitation** | Pas de validation côté client de la cohérence horaire (ex : fin avant début vérifiée uniquement côté API) |

#### `GenerationFacturesComponent`

| Attribut | Détail |
|---|---|
| **Rôle** | Workflow complet de facturation en 3 onglets (Générer, Enregistrer, Historique) |
| **Mode génération** | Deux modes exclusifs : par mois (`<input type="month">`) ou par période libre (dateDebut/dateFin) |
| **Onglet Enregistrer** | Tableau de toutes les factures du mois avec actions contextuelles selon le statut. Checkbox "Transmis" (confirmation modale) ? appel API `transmettre`. Bouton "? Fedcom" pour validation. Bouton "? Annuler" pour les factures approuvées |
| **Téléchargement .eml** | Téléchargement indépendant du marquage de transmission. L'utilisateur télécharge le `.eml`, l'envoie manuellement via Outlook, puis coche "Transmis" |
| **Tracking** | `Set<number>` pour `sendingEmail` (téléchargement en cours) et `transmittingId` (confirmation en cours) — évite les doubles clics |

#### `HDPrestationJourComponent` (Standalone)

| Attribut | Détail |
|---|---|
| **Rôle** | Fiche journalière Helpdesk IT — tickets et tâches |
| **FormArray** | Deux `FormArray<FormGroup>` : `hdTickets` (date, heure, numero, type, dureeMin) et `hdAutresTaches` (denomination, date, dureeMin). Ajout/suppression dynamiques |
| **Persistance** | JSON fichier serveur (pas de base de données). Rechargement automatique au changement de date via `valueChanges.subscribe()` |
| **Nettoyage** | `nettoyerTicketsVides()` et `nettoyerAutresVides()` suppriment les lignes vides avant sauvegarde. `normaliserChampsAvantSave()` remplit les champs vides avec des valeurs par défaut |
| **Calcul semaine ISO** | Fonction locale `hdSemaineISO()` calcule la semaine ISO (format `YYYY-Www`) à partir d'une date, utilisée comme clé de regroupement |

#### `HDRecapSemaineComponent` (Standalone)

| Attribut | Détail |
|---|---|
| **Rôle** | Récapitulatif hebdomadaire Helpdesk IT — agrégation des 5 jours ouvrables |
| **Navigation** | `<input type="week">` natif HTML5 (support variable selon navigateurs). Calcul du lundi via `mondayFromISOWeek()` |
| **Accordéon** | Un `JourVue[]` de 5 éléments (lundi à vendredi) avec propriété `ouvert`. `toggleAll()` pour plier/déplier |
| **Totaux** | Calcul temps réel : `totalMinutesTickets()`, `totalMinutesAutres()`, `totalMinutes()` — pas de memoization |
| **Export** | Téléchargement Word via blob (`telechargerSemaineWord()`). Fichier nommé `FicheHelpdesk_{user}_{semaine}.docx` |

#### `AdStatusDashboardComponent`

| Attribut | Détail |
|---|---|
| **Rôle** | Tableau de bord des comptes Active Directory avec alertes de sécurité |
| **Catégories** | 5 niveaux d'alerte : mot de passe expiré, expiration imminente (3 seuils), compte inactif 90+ jours |
| **Interaction** | Marquage `IsNormal` + commentaire persistés en JSON côté serveur. Ouverture chat Teams directe |
| **Contrôle d'accès** | Seul composant vérifiant un rôle AD côté serveur (`gg_rol_SystemAdministrator`). Côté client, pas de guard Angular |

#### `InventoryComponent`

| Attribut | Détail |
|---|---|
| **Rôle** | Inventaire des machines avec client VPN GlobalProtect |
| **Import** | `<input type="file">` avec import automatique au `change`. CSV PowerShell parsé côté serveur. Merge avec données existantes |
| **Filtres** | Texte global, localisation, version exacte, vérifié/non. Stats calculées côté client (total, avec/sans GP, bureau, TT, %) |
| **Persistance inline** | `VerifiedByTeam` et `Remark` sauvegardés au `blur` (perte de focus). Pas de bouton "Sauvegarder" explicite |
| **Export** | CSV avec BOM UTF-8 (`\ufeff`) pour compatibilité Excel |

### 1.3 Synthèse des Patterns Techniques

| Technique | Composants utilisant | Commentaire |
|---|---|---|
| `ReactiveForms` | Calendar, InterpreteDetail, InterpreteList, Prestations, Indispo, HDPrestationJour, HDRecapSemaine | Formulaires complexes avec validation dynamique |
| `FormsModule` (ngModel) | GenerationFactures, Factures, PresenceInterpretes, Inventory | Formulaires simples, two-way binding |
| `combineLatest` | Calendar | Seul composant avec pipeline réactif multi-flux |
| `forkJoin` | Langues, NavbarInter | Appels parallèles indépendants |
| `BehaviorSubject` | Calendar, AuthentificationService | État réactif local |
| `FormArray` | HDPrestationJour | Lignes dynamiques (tickets, tâches) |
| `@ViewChild` + scroll | Prestations | UX : scroll vers le formulaire |
| `standalone` + `loadComponent` | HDPrestationJour, HDRecapSemaine | Seuls composants lazy-loaded |
| Clipboard API + fallback | Convocation | Double stratégie copie HTML |
| `type="week"` natif | HDRecapSemaine | Sélecteur semaine ISO HTML5 |

---

## 2. Inventaire et Patterns de l'API Backend (.NET 8)

### 2.1 Architecture : MVC sans Couche Service

L'API adopte une architecture **Controller-only** : toute la logique métier, l'accès aux données (EF Core), les calculs de paiement, la génération de fichiers (PDF, EML, Word, Excel) et les requêtes SQL brutes sont concentrés directement dans les contrôleurs.

Il n'existe aucune couche intermédiaire :
- Pas de couche **Service** / **Business Logic Layer**
- Pas de couche **Repository** (accès direct au `ApplicationDbContext`)
- Pas de **mediator** (MediatR) ni de **CQRS**

Le `ApplicationDbContext` est injecté via le constructeur dans chaque contrôleur et utilisé directement pour toutes les opérations CRUD et les requêtes complexes.

**Conséquence** : les contrôleurs contiennent entre 10 et 600+ lignes de code. Le `FacturesController` (le plus volumineux) dépasse 600 lignes et contient la logique de génération, d'approbation, d'annulation (9 étapes en transaction), de génération PDF, de construction de fichiers `.eml` et de la notice Peppol trilingue. Le `ReportsController` (~600 lignes) contient l'intégralité de la logique d'export tri-format (Excel/Word/PDF) avec construction manuelle de documents OOXML et QuestPDF.

### 2.2 Inventaire des Contrôleurs

Le projet contient **19 fichiers contrôleur** hébergeant **20 classes contrôleur** (le fichier `ValuesController.cs` contient deux classes : `ValuesController` et `CalendarController`).

#### Contrôleurs métier — Interprètes

| Contrôleur | Fichier | Route de base | Endpoints | Responsabilités clés |
|---|---|---|---|---|
| `InterpretesController` | `InterpretesController.cs` | `/api/interpretes` | 9 | CRUD identité (`GET/{tolkcode}`, `POST`, `PUT/{tolkcode}`, `DELETE/{tolkcode}`), recherche rapide par nom ou tolkcode (`GET /search`), matching langue source/destination + disponibilité avec tri par distance km (`GET /match`), audiences compatibles exactes (`GET /{tolkcode}/audiences-exact`), convocations validées (`GET /{tolkcode}/convocations`), liste complète des tolkcode (`GET /tolkcodes`). Validation format téléphone belge et TVA (`IsValidPhone`, `IsValidTva`). Séquence `NR_TOLK` pour la création |
| `TolklinkController` | `TolklinkController.cs` | `/api/interpretes/{tolkcode}/tolklink` | 3 | Assignation unitaire (`POST`), bulk (`POST /bulk`), soft-delete désassignation (`DELETE/{idAffAudience}` ? `Datesupp = DateTime.Now`). Vérification doublon via `COUNT` Oracle-safe |
| `AdressesController` | `AdressesController.cs` | `/api/interpretes/{tolkcode}/adresses` | 5 | Liste par interprète (`GET`, option `onlyActive`), création (`POST`), remplacement avec clôture automatique (`POST /replace`), modification (`PUT /adresses/{id}`), suppression physique (`DELETE /adresses/{id}`). Séquence manuelle `NR_AUTO_ADRESSE` |
| `LanguesController` | `LanguesController.cs` | `/api` (multi-routes) | 6 | Référentiel langues (`GET /langues`, option `destOnly`), langues source par interprète (`GET/POST/DELETE /interpretes/{tolkcode}/langues/sources`), langues destination par interprète (`GET/POST/DELETE /interpretes/{tolkcode}/langues/destination`). Séquences manuelles `NR_AUTO_LANGUE_SOURCE` et `NR_AUTO_DESTINATION`. DTOs déclarés en bas du même fichier |
| `TvaController` | `TvaController.cs` | `/api/interpretes/{tolkcode}/tva` | 3 | Historique TVA par interprète (`GET`), ajout avec clôture automatique du statut précédent (`POST`), référentiel statuts (`GET /api/tva/statuts`). Utilise `AutoMapper.ProjectTo` |
| `IndispoController` | `IndispoController.cs` | `/api/interpretes/{tolkcode}/indispo` | 3 | Liste des périodes (`GET`), ajout avec anti-chevauchement **en mémoire** et clôture automatique de la période ouverte (`POST`), suppression physique (`DELETE/{id}`). Utilise `AutoMapper.ProjectTo` et `Map` |

#### Contrôleurs métier — Planification et présence

| Contrôleur | Fichier | Route de base | Endpoints | Responsabilités clés |
|---|---|---|---|---|
| `CalendarController` | `ValuesController.cs` | `/api/calendar` | 3 | Récupération de toutes les données du calendrier VRM (`GET`), endpoint de test (`GET /test`), dump des en-têtes HTTP incluant `X-Remote-User` (`GET /headers`). **Note** : cette classe est définie dans le fichier `ValuesController.cs` et non dans un fichier dédié |
| `DashboardController` | `DashboardController.cs` | `/api/dashboard` | 6 | Audiences du jour (`GET /audiences/today`), compteur audiences (`GET /audiences/count-today`), compteur interprètes (`GET /interpretes/count-today`), langues demandées avec comptage (`GET /langues/today`), détail audiences avec interprètes depuis la vue `V_AUDIENCE_INTERPRETE_DETAIL` (`GET /audiences/detail-today`), audiences supprimées (`GET /audiences-supprimees/today`). Méthodes helper statiques `AllVrm()` / `AllAnn()` pour factoriser l'accès aux vues |
| `PrestationsController` | `PrestationsController.cs` | `/api/prestations` | 4 | Liste du jour avec union VRM+ANN+TOLKLINK+fallback prestations (`GET /jour`), création prestation avec calcul automatique montant/transport/TVA dans une transaction (`POST`), marquage absence (`POST /absence`), remplacement interprète (`POST /remplacement`). Méthode privée `CalculerEtMettreAJourPaiementAsync` (calcul barème, arrondi 15 min, minimum 75 min, transport 1×/jour, TVA 21%). Méthode privée `NextValAsync` pour les séquences Oracle |
| `ReportsController` | `ReportsController.cs` | `/api/reports` | 4 | Données JSON brutes (`GET /interpretes`), export Excel via ClosedXML (`GET /interpretes/excel`), export Word via OpenXml SDK avec 1 page par audience + page synthèse en paysage (`GET /interpretes/word`), export PDF via QuestPDF A4 paysage (`GET /interpretes/pdf`). Méthode privée `GetData()` (~200 lignes) avec fallback : vue `V_AUDIENCE_INTERPRETE_DETAIL` ? PRESTATION+TOLKLINK si vue vide. Enrichissement des magistrats via croisement `VUE_CALENDAR_ALL` |

#### Contrôleurs métier — Facturation et paiements

| Contrôleur | Fichier | Route de base | Endpoints | Responsabilités clés |
|---|---|---|---|---|
| `FacturesController` | `FacturesController.cs` | `/api/factures` | 5 | Listing avec filtres mois/statut/tolkcode (`GET`), génération groupée par interprète dans une transaction (`POST /generer`), changement de statut avec logique d'annulation en 9 étapes SQL brut (`PATCH /{id}/statut`), marquage transmission avec changement statut ? TRANSMISE (`PATCH /{id}/transmettre`), PDF batch (`GET /pdf`), génération `.eml` RFC 2822 avec PDF attaché + notice Peppol trilingue FR/NL/EN (`GET /{id}/eml`). Workflow : GENEREE ? TRANSMISE ? APPROUVEE ? ANNULEE + NOTE DE CREDIT ? CREDIT VALIDE |
| `PaiementsController` | `PaiementsController.cs` | `/api/paiements` | 4 | Récapitulatif mensuel par interprète avec totaux (`GET /mois`), détail par interprète avec km résolu par date (`GET /mois/{tolkcode}`), PDF batch mensuel via QuestPDF avec factures formatées BBAN (`GET /mois/pdf`), suppression avec libération des TOLKLINK (`DELETE/{id}`) |

#### Contrôleurs — Authentification et utilisateurs

| Contrôleur | Fichier | Route de base | Endpoints | Responsabilités clés |
|---|---|---|---|---|
| `AuthController` | `AuthController.cs` | `/api/auth` | 1 | `GET /whoami` — **seul endpoint avec `[Authorize]`** dans tout le projet. Récupère `User.Identity.Name`, fallback sur l'en-tête `X-Remote-User`, sinon `Challenge(IISDefaults.AuthenticationScheme)` pour forcer le handshake NTLM |
| `UserController` | `UserController.cs` | `/api/user` | 2 | `GET /current` — récupère l'utilisateur depuis l'en-tête `X-Remote-User` (pas depuis `User.Identity`). `POST /addUser` — **code inachevé** : génère un `newId` mais n'ajoute jamais l'entité au DbContext (`_context.Add()` est absent avant `SaveChangesAsync()`). Le `SaveChanges` est appelé sans modification ? no-op |

#### Contrôleurs — Modules IT

| Contrôleur | Fichier | Route de base | Endpoints | Responsabilités clés |
|---|---|---|---|---|
| `HdPrestationsController` | `HdPrestationsController.cs` | `/api/hd-prestations` | 5 | Enregistrer jour JSON (`POST /jour`), lire jour avec fallback par date (`GET /jour`), lire semaine avec fallback intelligent (`GET /semaine`), enregistrer semaine complète (`PUT /semaine`), export Word avec tableaux bordés et bloc signature (`GET /semaine/export/word`). Stockage fichier JSON sur disque (`data/hd-prestations/{user}/{semaine}/`). Sanitization des noms de fichier via `San()`. DTOs déclarés en haut du même fichier |
| `AdStatusController` | `AdStatusController.cs` | `/api/adstatus` | 3 | `GET` — lecture complète du CSV PowerShell avec parsing robuste multi-format de dates, fusion avec la persistance JSON. `POST /comment` — sauvegarde commentaire. `POST /normalstatus` — marquage IsNormal. **Seul contrôleur avec `[Authorize(Roles = ...)]` au niveau classe** : `[Authorize(Roles = @"INTRRDM01\gg_rol_SystemAdministrator")]`. Persistance fichier JSON avec `lock` pour thread-safety. Chemin CSV configurable via `IConfiguration["AdStatus:CsvPath"]` |
| `InventoryController` | `InventoryController.cs` | `/api/inventory` | 3 | Liste triée avec scoring (bureau+en ligne, version GP, nom) (`GET`), import CSV PowerShell avec merge préservant annotations manuelles (`POST /import`), mise à jour annotations (`PUT/{computerName}`). Stockage JSON (`Data/GlobalProtectInventory.json`). Tri par `Version.TryParse` pour comparaison sémantique des versions GP |

#### Contrôleurs — Scaffolding / non utilisés

| Contrôleur | Fichier | Route de base | Statut |
|---|---|---|---|
| `ValuesController` | `ValuesController.cs` | `/api/values` | **Vide** — classe sans aucune méthode |
| `ValuesController1` | `ValuesController1.cs` | `/api/valuescontroller1` | **Vide** — classe sans aucune méthode |
| `WeatherForecastController` | `WeatherForecastController.cs` | `/api/weatherforecast` | Scaffolding par défaut .NET, non utilisé en production |

#### Synthèse quantitative

| Métrique | Valeur |
|---|---|
| Fichiers contrôleur | 19 |
| Classes contrôleur | 20 (dont 3 vides/non utilisées) |
| Contrôleurs métier actifs | 17 |
| Total endpoints estimé | ~60+ |
| Contrôleurs avec `[Authorize]` | 2 (`AuthController` sur 1 endpoint, `AdStatusController` sur toute la classe) |
| Contrôleurs sans aucune autorisation | 18 |
| Contrôleurs injectant `ApplicationDbContext` | 14 |
| Contrôleurs injectant `AutoMapper` | 3 (`TolklinkController`, `TvaController`, `IndispoController`) |
| Contrôleurs avec stockage fichier (pas de DB) | 3 (`HdPrestationsController`, `AdStatusController`, `InventoryController`) |

### 2.3 Absence de Middleware de Gestion d'Erreurs

Le pipeline ASP.NET Core dans `Program.cs` ne contient **aucun middleware de gestion d'erreurs globale** :

```
UseCors ? UseAuthentication ? UseAuthorization ? MapControllers
```

**Comportements observés :**

| Situation | Résultat côté client |
|---|---|
| Exception non gérée dans un contrôleur | HTTP 500 avec stack trace en développement, message vide en production |
| `InvalidOperationException` (ex : pas de barème d'indexation) | HTTP 500 silencieux — l'utilisateur voit "Erreur inconnue" |
| Erreur Oracle (connexion perdue, verrou) | HTTP 500 silencieux — pas de retry ni de message adapté |
| Requête SQL brute avec erreur de syntaxe | HTTP 500 avec le message Oracle non filtré |

Il n'existe pas de :
- `app.UseExceptionHandler()` ni `app.UseDeveloperExceptionPage()` en production
- Middleware de logging structuré des erreurs
- Filtres d'exception globaux (`IExceptionFilter`)
- Pattern `ProblemDetails` (RFC 7807) pour les réponses d'erreur

### 2.4 Utilisation d'AutoMapper

AutoMapper est configuré mais utilisé de manière limitée :

| Contrôleur | Utilisation |
|---|---|
| `IndispoController` | `ProjectTo<IndispoDto>()` pour la projection et `Map<Tolkindispo>(dto)` pour l'insertion |
| `TvaController` | `ProjectTo<TvaRowDto>()` et `ProjectTo<StatutDto>()` |
| `TolklinkController` | Injecte `IMapper` mais ne l'utilise dans aucune méthode visible (injection superflue) |
| Tous les autres (17 contrôleurs) | Mapping manuel — construction d'objets anonymes, DTOs inline ou records déclarés dans le même fichier |

---

## 3. Dette Technique et Anomalies de Base de Données (Oracle & EF Core)

### 3.1 Conflits de Typage `TOLKCODE`

La colonne `TOLKCODE` est la clé métier centrale du système (identifiant unique d'un interprète). Son type varie selon les tables et les couches :

| Table / Entité | Type Oracle | Type C# | Commentaire |
|---|---|---|---|
| `TOLKIDENTITY` | `NUMBER` | `int` | Clé primaire, type natif |
| `TOLKADRESSE` | `VARCHAR2(5)` | `string` | FK logique — **pas de FK physique** |
| `TOLKINDISPO` | `VARCHAR2(5)` | `string` | FK logique — **pas de FK physique** |
| `TOLKLINK` | `NUMBER` | `int` | FK logique vers TOLKIDENTITY |
| `PRESTATION` | `VARCHAR2` | `string` | FK logique — comparaisons string |
| `PAIEMENT` | `VARCHAR2` | `string` | FK logique — comparaisons string |
| `FACTURE` | `VARCHAR2` | `string` | FK logique — comparaisons string |

**Conséquences :**

1. **Conversions explicites omniprésentes** : `tolkcode.ToString()`, `int.TryParse(facture.Tolkcode, out var tolkInt)` — dispersées dans les contrôleurs
2. **Risque `ORA-01722: invalid number`** : toute jointure ou comparaison Oracle entre un `VARCHAR2` et un `NUMBER` nécessite une conversion implicite. Si un `TOLKCODE` contient un caractère non numérique (ex : espace, tiret), Oracle lève `ORA-01722`
3. **Jointures EF Core impossibles** : EF Core ne peut pas créer de navigation entre `Tolkidentity.Tolkcode` (int) et `Tolkadresse.Tolkcode` (string). Toutes les jointures interprète ? adresse/indispo/prestation/paiement sont faites manuellement dans le code C#
4. **Filtre `int.TryParse` défensif** : dans `FacturesController.List()`, chaque tolkcode est filtré via `int.TryParse(s, out var i) ? i : 0` avant toute jointure avec `Tolkidentity` — si le parse échoue, l'interprète est silencieusement ignoré

### 3.2 Gestion Hybride des Séquences Oracle

Le projet utilise deux stratégies concurrentes pour la génération des clés primaires via séquences Oracle :

**Stratégie 1 — EF Core `HasDefaultValueSql` :**

| Entité | Séquence | Configuration |
|---|---|---|
| `Tolkadresse` | `NR_AUTO_ADRESSE` | `.HasDefaultValueSql("NR_AUTO_ADRESSE.NEXTVAL")` |
| `Tolklink` | `NR_AUTO_TOLKLINK` | `.HasDefaultValueSql("NR_AUTO_TOLKLINK.NEXTVAL")` |
| `Prestation` | `ID_PRESTATION_AUTO` | `.HasDefaultValueSql("ID_PRESTATION_AUTO.NEXTVAL")` |
| `Paiement` | `NR_AUTO_PAIEMENT` | `.HasDefaultValueSql("NR_AUTO_PAIEMENT.NEXTVAL")` |
| `Facture` | `NR_AUTO_FACTURE` | `.HasDefaultValueSql("NR_AUTO_FACTURE.NEXTVAL")` |

**Stratégie 2 — SQL brut `SELECT NEXTVAL FROM DUAL` :**

| Contrôleur | Méthode | Séquence(s) | Raison |
|---|---|---|---|
| `PrestationsController` | `NextValAsync(string)` | `NR_AUTO_PAIEMENT`, `ID_PRESTATION_AUTO` | Le contrôleur récupère l'ID *avant* l'insertion EF Core, puis l'assigne manuellement à l'entité |
| `AdressesController` | `NextIdAdresseAsync()` | `NR_AUTO_ADRESSE` | Même approche : ID pré-alloué avant `Add()` |
| `LanguesController` | `GetNextValAsync(string)` | `NR_AUTO_LANGUE_SOURCE`, `NR_AUTO_DESTINATION` | Même approche : ID pré-alloué pour les langues source et destination |
| `InterpretesController` | SQL brut inline | `NR_TOLK` | `SELECT NR_TOLK.NEXTVAL FROM DUAL` pour la création d'un nouvel interprète |
| `FacturesController` | SQL brut dans `UpdateStatut` | `NR_AUTO_PAIEMENT`, `ID_PRESTATION_AUTO` | Insertion complète via `DbCommand` avec `RETURNING INTO` — EF Core contourné pour éviter les conflits de change tracker |

**Duplication observée** : la logique `SELECT {sequence}.NEXTVAL FROM DUAL` est implémentée dans **4 méthodes privées distinctes** (`NextValAsync` dans `PrestationsController`, `NextIdAdresseAsync` dans `AdressesController`, `GetNextValAsync` dans `LanguesController`, code inline dans `InterpretesController`) — chacune avec de légères variantes dans la gestion de la connexion (ouverture/fermeture, `using` vs `try/finally`).

**Risques identifiés :**

- **Désynchronisation** : si EF Core tente d'utiliser `HasDefaultValueSql` sur la même séquence qu'un appel SQL brut, les IDs peuvent entrer en conflit (bien que les séquences Oracle soient monotoniques, le change tracker peut être incohérent)
- **ID gaspillés** : l'appel `NEXTVAL` incrémente la séquence même si la transaction est ensuite annulée (comportement normal des séquences Oracle, mais non documenté dans le code)
- **Absence d'abstraction** : la logique `NextVal` est dupliquée dans 4 contrôleurs avec des signatures et des comportements de gestion de connexion différents. Pas de classe utilitaire partagée ni d'extension sur `ApplicationDbContext`

### 3.3 Requêtes SQL Brutes et Risques d'Injection

Plusieurs endpoints utilisent `ExecuteSqlRawAsync` ou `DbCommand` avec construction de chaîne :

**Cas 1 — Suppression par clause `IN` (FacturesController, annulation) :**

```csharp
var prestaIdList = string.Join(",", prestationIds);
await _db.Database.ExecuteSqlRawAsync(
    $"DELETE FROM PRESTATION WHERE ID_PRESTATION IN ({prestaIdList})", ct);
```

```csharp
var paiIdList = string.Join(",", paiementIds);
await _db.Database.ExecuteSqlRawAsync(
    $"DELETE FROM PAIEMENT WHERE ID_PAIEMENT IN ({paiIdList})", ct);
```

**Évaluation du risque** : Les valeurs `prestationIds` et `paiementIds` sont des `List<int>` issues de requêtes EF Core précédentes (pas d'input utilisateur direct). Le risque d'injection SQL est **faible** dans ce contexte spécifique car les valeurs sont des entiers, mais le pattern reste une **mauvaise pratique** :
- L'interpolation de chaîne contourne la paramétrisation
- Si le type changeait vers `string` dans une refactorisation future, le risque deviendrait critique
- L'analyseur de sécurité statique (SonarQube, Roslyn) signalerait ce pattern comme vulnérabilité

**Cas 2 — Insertion avec `DbCommand` (FacturesController, note de crédit) :**

```csharp
cmd.CommandText = @"INSERT INTO PAIEMENT (...) VALUES (NR_AUTO_PAIEMENT.NEXTVAL, :tk, :dp, ...)
                    RETURNING ID_PAIEMENT INTO :newid";
```

Ce cas utilise correctement des **paramètres nommés** (`:tk`, `:dp`, etc.) — pas de risque d'injection.

**Cas 3 — Séquence par interpolation (PrestationsController) :**

```csharp
cmd.CommandText = $"SELECT {sequenceName}.NEXTVAL FROM DUAL";
```

Le paramètre `sequenceName` est une constante codée en dur dans l'appel (`"NR_AUTO_PAIEMENT"`, `"ID_PRESTATION_AUTO"`) — pas d'input utilisateur. Le risque est nul dans l'implémentation actuelle, mais le pattern est fragile.

### 3.4 Chargement Mémoire et Absence de Pagination

| Requête | Contrôleur | Volume estimé | Impact |
|---|---|---|---|
| `_db.Indexations.ToListAsync()` | `PrestationsController` | ~10 lignes | Négligeable, mais chargé à **chaque** création de prestation |
| `_db.VueCalendarVrmPcs` + `VueCalendarAnns` (toutes les audiences du jour) | `PrestationsController.GetJour()` | ~200-500 lignes/jour | Acceptable |
| `_db.Factures.Where(...)` (toutes les factures d'un mois) | `FacturesController.List()` | ~100-500 lignes | Pas de pagination |
| Toutes les identités des interprètes (`Tolkidentities.Where(...)`) | Multiple contrôleurs | ~500-1000 lignes | Chargé en dictionnaire à chaque requête listant des factures/paiements |
| `listAllTolkcodes()` côté Angular | `CalendarComponent` | Liste complète des interprètes | Chargée une seule fois, stockée côté client |

### 3.5 Transactions et Isolation

| Opération | Isolation | Risque |
|---|---|---|
| Génération de factures (`Generer`) | Transaction explicite (`BeginTransactionAsync`) | Correcte — groupement par tolkcode dans une seule transaction |
| Annulation + note de crédit | Transaction explicite (9 étapes) | Correcte — atomicité garantie |
| Création prestation + calcul paiement | Transaction explicite | Correcte |
| Approbation Fedcom | Pas de transaction explicite | Risque faible (opération simple sur 1 ligne) |
| Marquage transmission | Pas de transaction explicite | Risque faible |
| Modification adresse "replace" | Transaction explicite | Correcte |

---

## 4. Sécurité, Concurrence et Vulnérabilités de Déploiement

### 4.1 Mots de Passe en Clair dans le Dépôt Git

**Criticité : ÉLEVÉE**

Les fichiers de configuration contiennent les identifiants Oracle en clair :

| Fichier | Contenu exposé |
|---|---|
| `Dragoman.Server/appsettings.json` | `User ID=DRAGOMAN;Password=InterTolk` (serveur dev `LAURENTIDE`) |
| `Dragoman.Server/appsettings.Production.json` | `User Id=DRAGOMAN;Password=InterTolk` (serveur production `10.4.4.22:1529`, SID `CCE11g`) |
| `Dragoman.Server/publish/appsettings.json` | Copie identique |
| `Dragoman.Server/publish/publish/appsettings.json` | Copie identique (dossiers publish imbriqués) |
| `Dragoman.Server/publish/publish/publish/appsettings.json` | Copie identique (triple imbrication) |

**Constats :**
- Les fichiers `appsettings*.json` sont versionnés dans Git (pas dans `.gitignore`)
- Le dépôt est hébergé sur **GitHub public** (`https://github.com/Jasoncaufriez/Dragoman`)
- Les dossiers `publish/` contiennent des copies complètes du build (y compris les DLL, les configurations et les secrets)
- L'adresse IP du serveur Oracle de production (`10.4.4.22`) est exposée
- Le mot de passe est **identique** entre les environnements de développement et de production

**Recommandation** : migration immédiate vers `dotnet user-secrets` (développement) et variables d'environnement / Azure Key Vault / fichier hors-VCS (production). Ajout de `appsettings.Production.json` et des dossiers `publish/` au `.gitignore`. Rotation du mot de passe Oracle.

### 4.2 Absence d'Autorisation sur les Endpoints

**Criticité : ÉLEVÉE**

Sur l'ensemble des 20 classes contrôleur du projet, seules **deux** portent une forme d'autorisation :

1. `AuthController` — un seul endpoint (`GET /whoami`) avec `[Authorize]` au niveau méthode
2. `AdStatusController` — toute la classe protégée par `[Authorize(Roles = @"INTRRDM01\gg_rol_SystemAdministrator")]`

Tous les autres endpoints (factures, prestations, paiements, interprètes, adresses, langues, TVA, indisponibilités, calendrier, dashboard, helpdesk, inventory) sont **accessibles sans authentification applicative** :

| Contrôleur | `[Authorize]` | Vérification de rôle |
|---|---|---|
| `AuthController` | ? sur 1 endpoint (`[Authorize]` sur `WhoAmI`) | Non |
| `AdStatusController` | ? au niveau classe (`[Authorize(Roles = @"INTRRDM01\gg_rol_SystemAdministrator")]`) | ? Rôle AD SystemAdministrator |
| `InterpretesController` | ? | Non |
| `FacturesController` | ? | Non |
| `PrestationsController` | ? | Non |
| `PaiementsController` | ? | Non |
| `TolklinkController` | ? | Non |
| `AdressesController` | ? | Non |
| `LanguesController` | ? | Non |
| `TvaController` | ? | Non |
| `IndispoController` | ? | Non |
| `DashboardController` | ? | Non |
| `CalendarController` | ? | Non |
| `ReportsController` | ? | Non |
| `HdPrestationsController` | ? | Non |
| `InventoryController` | ? | Non |
| `UserController` | ? | Non |
| `WeatherForecastController` | ? | Non |
| `ValuesController` | ? | Non |
| `ValuesController1` | ? | Non |

**Mitigation actuelle** : l'application est déployée sur un serveur intranet IBZ (`rvv-ccesrv21`) derrière IIS avec Windows Authentication activée au niveau IIS. IIS refuse les requêtes non authentifiées avant qu'elles n'atteignent l'application. Cependant :
- En développement (Kestrel/localhost:4200), aucune authentification n'est requise
- Si IIS est mal configuré (ex : authentification anonyme activée par erreur), tous les endpoints deviennent publics
- Il n'existe pas de défense en profondeur côté applicatif

**Recommandation** : ajouter `[Authorize]` au niveau global (filtre dans `Program.cs` ou convention) et `[AllowAnonymous]` uniquement sur les endpoints qui le nécessitent.

### 4.3 Politique CORS

```csharp
policy.WithOrigins("http://localhost:4200", "http://rvv-ccesrv21")
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();
```

| Point | Évaluation |
|---|---|
| Origines restreintes | ? Correct — uniquement localhost (dev) et serveur production |
| `AllowAnyHeader` | ?? Permissif — autorise tout en-tête dans les requêtes CORS |
| `AllowAnyMethod` | ?? Permissif — autorise DELETE, PUT, PATCH sans restriction |
| `AllowCredentials` | ? Nécessaire pour le handshake NTLM |
| HTTPS | ? Absent — les deux origines sont en HTTP. Les credentials NTLM transitent en clair sur le réseau local |

### 4.4 Race Conditions Identifiées

**RC-1 : Génération de factures en double**

Si deux agents lancent simultanément `POST /api/factures/generer` pour le même mois :
1. Les deux requêtes lisent les paiements avec `IdFacture == null`
2. Les deux créent des factures pour les mêmes interprètes
3. Les deux affectent `IdFacture` sur les mêmes paiements ? le dernier `SaveChanges()` gagne

**Mitigation actuelle** : aucune. Pas de verrouillage `SELECT FOR UPDATE`, pas de contrainte d'unicité sur la combinaison (tolkcode, période).

**RC-2 : Transport compté deux fois**

La règle "transport payé une seule fois par jour" est vérifiée au moment de la création de la prestation :

```csharp
var dejaTransportJour = paiementsJour.Any(pa => pa.Transport > 0);
var transport = dejaTransportJour ? 0m : euroKm * kmAR;
```

Si deux prestations sont créées **simultanément** pour le même interprète le même jour (ex : deux agents encodent en même temps), les deux requêtes lisent `dejaTransportJour = false` et attribuent toutes deux le transport ? **double facturation du transport**.

**Mitigation actuelle** : aucune. La transaction englobe la création prestation + calcul paiement, mais le `SELECT` n'est pas verrouillant.

**RC-3 : Double assignation TOLKLINK**

La vérification de doublon avant insertion :

```csharp
var count = await _db.Tolklinks.Where(x => x.Tolkcode == tolkcode && ...).CountAsync();
if (count > 0) return Conflict("Lien déjà existant.");
```

N'est pas protégée par un verrou. Deux assignations simultanées du même interprète à la même audience peuvent passer toutes les deux la vérification.

### 4.5 Synthèse des Vulnérabilités

| ID | Catégorie | Sévérité | Description | Statut |
|---|---|---|---|---|
| SEC-01 | Secrets | ?? Critique | Mot de passe Oracle en clair dans `appsettings*.json` versionnés sur GitHub public | Non corrigé |
| SEC-02 | Secrets | ?? Critique | IP et SID du serveur Oracle de production exposés sur GitHub public | Non corrigé |
| SEC-03 | Secrets | ?? Haute | Dossiers `publish/` (avec DLL et configs) versionnés dans Git | Non corrigé |
| SEC-04 | Authz | ?? Haute | Absence de `[Authorize]` sur 18 des 20 contrôleurs (seuls `AuthController` et `AdStatusController` sont protégés) | Mitigé par IIS |
| SEC-05 | Transport | ?? Moyenne | HTTP uniquement (pas de HTTPS) — credentials NTLM en clair sur le réseau | Mitigé par réseau interne |
| SEC-06 | SQL | ?? Moyenne | Interpolation de chaîne dans `ExecuteSqlRawAsync` (clauses `IN`) | Risque faible (valeurs int) |
| SEC-07 | Concurrence | ?? Moyenne | Race condition sur la génération de factures (double génération possible) | Non corrigé |
| SEC-08 | Concurrence | ?? Moyenne | Race condition sur le transport (double facturation possible) | Non corrigé |
| SEC-09 | Concurrence | ?? Faible | Race condition sur l'assignation TOLKLINK (doublon possible) | Non corrigé |
| SEC-10 | Erreurs | ?? Faible | Pas de middleware de gestion d'erreurs global (stack trace en dev, 500 muet en prod) | Non corrigé |

---

*Document généré à partir de l'analyse exhaustive du code source du dépôt Dragoman (branche `main`).*
