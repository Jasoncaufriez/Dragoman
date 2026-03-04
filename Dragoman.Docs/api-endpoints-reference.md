# Dragoman — Référence Complète des Endpoints API

> Document exhaustif de tous les endpoints exposés par l'API backend ASP.NET Core 8.
> Chaque endpoint est documenté avec : méthode HTTP, route, paramètres attendus, corps de requête, réponse, codes HTTP et usage métier.
>
> **État du document** : ? **100 % documentés en détail** — tous les 72 endpoints sont entièrement documentés.

---

## Table des matières

1. [Auth](#1-auth)
2. [Dashboard](#2-dashboard)
3. [Interprètes](#3-interprètes)
4. [Tolklink (assignation interprète ? audience)](#4-tolklink)
5. [Adresses](#5-adresses)
6. [Langues](#6-langues)
7. [TVA](#7-tva)
8. [Indisponibilités](#8-indisponibilités)
9. [Prestations](#9-prestations)
10. [Paiements](#10-paiements)
11. [Factures](#11-factures)
12. [Calendar](#12-calendar)
13. [Reports](#13-reports)
14. [User](#14-user)
15. [Helpdesk Prestations](#15-helpdesk-prestations)
16. [AD Status](#16-ad-status)
17. [Inventory](#17-inventory)
18. [WeatherForecast (scaffolding)](#18-weatherforecast)

---

## Légende

| Symbole | Signification |
|---|---|
| ?? | Endpoint protégé par `[Authorize]` |
| ?? | Endpoint sans autorisation applicative (protégé uniquement par IIS) |
| ?? | Retourne un fichier (PDF, Excel, Word, EML, CSV) |
| `QS` | Query String (paramètre dans l'URL `?param=value`) |
| `PATH` | Paramètre dans le chemin de la route `/api/resource/{id}` |
| `BODY` | Paramètre dans le corps JSON de la requête |

---

## 1. Auth

**Contrôleur** : `AuthController` — **Fichier** : `AuthController.cs`
**Route de base** : `/api/auth`
**Autorisation** : `[Authorize]` sur l'unique endpoint

---

### 1.1 `GET /api/auth/whoami` ??

**Rôle métier** : Identifier l'utilisateur Windows connecté. Utilisé au chargement de l'application Angular pour récupérer le login NTLM et l'afficher dans la barre de navigation. Sert également à déclencher le handshake NTLM initial entre le navigateur et IIS.

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps | Description |
|---|---|---|---|
| `200 OK` | `text/plain` | `DOMAIN\username` | Nom d'utilisateur Windows Identity (`User.Identity.Name`) ou fallback sur l'en-tête `X-Remote-User` |

**Réponse erreur** :

| Code | Description |
|---|---|
| `401 Unauthorized` | Challenge NTLM si `User.Identity.Name` et `X-Remote-User` sont tous les deux vides — force le navigateur à renvoyer le handshake |

**Composants Angular appelants** : `AuthentificationService` (appelé au démarrage de l'app)

**Exemple de réponse** :
```
INTRRDM01\jcaufriez
```

---

## 2. Dashboard

**Contrôleur** : `DashboardController` — **Fichier** : `DashboardController.cs`
**Route de base** : `/api/dashboard`
**Autorisation** : ?? Aucune

**Source de données** : Union des vues Oracle `VUE_CALENDAR_VRM_PC` et `VUE_CALENDAR_ANN` (audiences VRM et annulations), plus la vue `V_AUDIENCE_INTERPRETE_DETAIL`.

---

### 2.1 `GET /api/dashboard/audiences/today` ??

**Rôle métier** : Récupérer la liste distincte des audiences du jour (heure + salle) pour affichage sur le tableau de bord principal. Sert à montrer combien d'audiences ont lieu aujourd'hui et dans quelles salles.

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<AudienceTodayItem>` |

**Schéma `AudienceTodayItem`** (objet anonyme) :

| Champ | Type | Description |
|---|---|---|
| `dateAudience` | `DateTime` | Date de l'audience |
| `heureAudience` | `string` | Heure (ex: `"09:00"`) |
| `salleAudience` | `string` | Nom de la salle (ex: `"A12"`) |

**Logique** : Union des vues VRM + ANN filtrées sur `DateAudience >= today && < tomorrow`, projection sur `(DateAudience, HeureAudience, SalleAudience)`, `DISTINCT`, tri par heure puis salle.

**Composants Angular appelants** : `DashboardComponent`

**Exemple de réponse** :
```json
[
  { "dateAudience": "2025-07-14T00:00:00", "heureAudience": "09:00", "salleAudience": "A12" },
  { "dateAudience": "2025-07-14T00:00:00", "heureAudience": "09:00", "salleAudience": "B03" },
  { "dateAudience": "2025-07-14T00:00:00", "heureAudience": "14:00", "salleAudience": "A12" }
]
```

---

### 2.2 `GET /api/dashboard/audiences/detail-today` ??

**Rôle métier** : Récupérer le détail complet des audiences du jour avec les interprètes assignés, leurs langues, téléphones et salles. Utilisé par le tableau de bord pour afficher la vue détaillée « Qui est où, pour quelle langue ».

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<VAudienceInterpreteDetail>` (entité complète de la vue Oracle) |

**Schéma** : l'entité `VAudienceInterpreteDetail` telle que mappée par EF Core (toutes les colonnes de la vue `V_AUDIENCE_INTERPRETE_DETAIL`).

| Champ principal | Type | Description |
|---|---|---|
| `tolkcode` | `int?` | Code de l'interprète assigné |
| `nom` | `string?` | Nom de l'interprète |
| `prenom` | `string?` | Prénom |
| `gsm` | `string?` | GSM |
| `tel` | `string?` | Téléphone |
| `telbis` | `string?` | Téléphone bis |
| `taalrol` | `int?` | 1=NL, 2=FR |
| `heureAudience` | `string?` | Heure de l'audience |
| `salleAudience` | `string?` | Salle |
| `langueRequete` | `string?` | Langue demandée |
| `jour` | `DateTime?` | Date de l'audience |

**Attention** : cet endpoint retourne **toutes les lignes** de la vue sans filtre de date — le filtrage est appliqué directement dans la vue Oracle elle-même (ou non, selon la définition de la vue).

**Composants Angular appelants** : `DashboardComponent`

---

### 2.3 `GET /api/dashboard/audiences/count-today` ??

**Rôle métier** : Compteur du nombre d'audiences distinctes aujourd'hui. Affiché comme KPI dans une carte du tableau de bord (ex: « 12 audiences aujourd'hui »).

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `{ nbAudiences: number }` |

**Logique** : identique à `audiences/today` mais retourne un `COUNT(DISTINCT (DateAudience, HeureAudience, SalleAudience))` au lieu des lignes.

**Composants Angular appelants** : `DashboardComponent`

**Exemple de réponse** :
```json
{ "nbAudiences": 12 }
```

---

### 2.4 `GET /api/dashboard/interpretes/count-today` ??

**Rôle métier** : Compteur du nombre d'interprètes distincts mobilisés aujourd'hui. Affiché comme KPI dans une carte du tableau de bord (ex: « 8 interprètes mobilisés »).

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `{ nbInterpretes: number }` |

**Logique** : Concaténation des `Tolkcode` des vues VRM + ANN filtrées sur aujourd'hui, exclusion des null, `DISTINCT`, `COUNT`.

**Composants Angular appelants** : `DashboardComponent`

**Exemple de réponse** :
```json
{ "nbInterpretes": 8 }
```

---

### 2.5 `GET /api/dashboard/langues/today` ??

**Rôle métier** : Classement des langues demandées aujourd'hui avec le nombre de demandes par langue. Affiché dans un graphique ou une liste triée sur le tableau de bord (ex: « Arabe: 5, Dari: 3, Pashto: 2 »).

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<LangueTodayItem>` |

**Schéma `LangueTodayItem`** (objet anonyme) :

| Champ | Type | Description |
|---|---|---|
| `langue` | `string` | Libellé français de la langue |
| `nb` | `int` | Nombre de demandes |

**Logique** : Concaténation des `LangueRequete` des vues VRM + ANN, exclusion de `null` et de `"*Aucun interprète demandé"`, `GROUP BY langue`, tri par `nb DESC`.

**Composants Angular appelants** : `DashboardComponent`

**Exemple de réponse** :
```json
[
  { "langue": "Arabe", "nb": 5 },
  { "langue": "Dari", "nb": 3 },
  { "langue": "Pashto", "nb": 2 },
  { "langue": "Tigrinya", "nb": 1 }
]
```

---

### 2.6 `GET /api/dashboard/audiences-supprimees/today` ??

**Rôle métier** : Liste des audiences supprimées ou modifiées du jour. Permet de suivre les annulations de dernière minute.

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<AudienceSupprimeeItem>` |

**Schéma `AudienceSupprimeeItem`** (objet anonyme) :

| Champ | Type | Description |
|---|---|---|
| `dateAudience` | `DateTime` | Date de l'audience |
| `heureAudience` | `string` | Heure |
| `salleAudience` | `string` | Salle |
| `nroRoleGen` | `decimal` | Numéro de rôle général |
| `langueRequete` | `string` | Langue demandée |

**Logique** : Concaténation (sans `DISTINCT`) des vues VRM + ANN filtrées sur aujourd'hui, avec projection étendue incluant `NroRoleGen` et `LangueRequete`.

**Composants Angular appelants** : `DashboardComponent`

---

## 3. Interprètes

**Contrôleur** : `InterpretesController` — **Fichier** : `InterpretesController.cs`
**Route de base** : `/api/interpretes`
**Autorisation** : ?? Aucune
**Source de données** : Table `TOLKIDENTITY` (clé primaire `TOLKCODE`, ~30 colonnes), jointures avec `LANGUE_SOURCE`, `LANGUE_DESTINATION`, `LANGUE`, `TOLKADRESSE`, `TOLKINDISPO`, vues `VUE_CALENDAR_VRM_PC` et `VUE_CALENDAR_ANN`.

---

### 3.1 `GET /api/interpretes/{tolkcode}` ??

**Rôle métier** : Récupérer la fiche complète d'un interprète par son tolkcode. Utilisé par le composant `InterpreteDetailComponent` pour afficher le formulaire de la fiche identité (accordéon 6 sections). Sert aussi au `NavbarInterComponent` pour afficher le nom dans la barre de navigation contextuelle.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? | Identifiant unique de l'interprète (PK de `TOLKIDENTITY`) |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Tolkidentity` (entité complète) |

**Schéma `Tolkidentity`** (champs principaux) :

| Champ | Type | Colonne Oracle | Description |
|---|---|---|---|
| `tolkcode` | `int` | `TOLKCODE` | PK — identifiant séquentiel |
| `nom` | `string?` | `NOM` | Nom de famille (max 50 car.) |
| `prenom` | `string?` | `PRENOM` | Prénom (max 50 car.) |
| `email` | `string?` | `EMAIL` | Adresse email (max 80 car.) |
| `tel` | `string?` | `TEL` | Téléphone fixe |
| `telbis` | `string?` | `TELBIS` | Téléphone secondaire |
| `gsm` | `string?` | `GSM` | GSM |
| `fax` | `string?` | `FAX` | Fax (historique) |
| `taalrol` | `int?` | `TAALROL` | 1=NL, 2=FR |
| `beedigd` | `int?` | `BEEDIGD` | 1=assermenté, 0=non |
| `dateNaissance` | `DateTime?` | `DATE_NAISSANCE` | Date de naissance |
| `nationaliteit` | `string?` | `NATIONALITEIT` | Nationalité |
| `rijksregisternr` | `string?` | `RIJKSREGISTERNR` | Numéro de registre national |
| `herkomst` | `string?` | `HERKOMST` | Origine |
| `genre` | `string?` | `GENRE` | Genre (1 car.) |
| `beroepscode` | `int?` | `BEROEPSCODE` | Code profession |
| `btwNr` | `int?` | `BTW_NR` | N° TVA (int) |
| `bankrekening` | `string?` | `BANKREKENING` | Compte bancaire BBAN belge (12 chiffres) |
| `iban` | `string?` | `IBAN` | IBAN (max 34 car.) |
| `tva` | `string?` | `TVA` | N° TVA belge (ex: `BE0123456789`) |
| `remarque` | `string?` | `REMARQUE` | Remarque libre (max 250 car.) |
| `evaluatiecode` | `int?` | `EVALUATIECODE` | Code d'évaluation |
| `ba` | `string?` | `BA` | Bureau d'aide (max 11 car.) |
| `fedcom` | `int?` | `FEDCOM` | Indicateur Fedcom |
| `ondernemingsnummer` | `int?` | `ONDERNEMINGSNUMMER` | Numéro d'entreprise |
| `vestigingsnummer` | `string?` | `VESTIGINGSNUMMER` | Numéro d'établissement |
| `fedcomnummer` | `int?` | `FEDCOMNUMMER` | Numéro Fedcom |
| `iscce` | `string?` | `ISCCE` | Indicateur CCE (1 car.) |

**Réponse erreur** :

| Code | Description |
|---|---|
| `404 Not Found` | Aucun interprète trouvé pour ce tolkcode |

**Composants Angular appelants** : `InterpretesService.getIdentite()` ? `InterpreteDetailComponent`, `NavbarInterComponent`, `ConvocationComponent`

**Exemple de réponse** :
```json
{
  "tolkcode": 1055,
  "nom": "DUPONT",
  "prenom": "Marie",
  "email": "marie.dupont@example.be",
  "tel": "02 123 45 67",
  "gsm": "0478 12 34 56",
  "taalrol": 2,
  "beedigd": 1,
  "tva": "BE0123456789",
  "iban": "BE68539007547034",
  "fedcomnummer": 42
}
```

---

### 3.2 `GET /api/interpretes` ??

**Rôle métier** : Lister les interprètes avec pagination serveur. Utilisé principalement pour le parcours séquentiel et le débogage. En pratique, le composant `InterpreteListComponent` utilise plutôt l'endpoint `/search`.

**Paramètres** :

| Paramètre | Source | Type | Requis | Défaut | Description |
|---|---|---|---|---|---|
| `skip` | `QS` | `int` | ? | `0` | Nombre d'éléments à sauter. Corrigé à 0 si négatif |
| `take` | `QS` | `int` | ? | `50` | Nombre d'éléments à retourner. Borné entre 1 et 200 via `Math.Clamp()` |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<Tolkidentity>` (entités complètes, triées par `tolkcode ASC`) |

**Logique** : `SELECT * FROM TOLKIDENTITY ORDER BY TOLKCODE OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY`. Pas de total count retourné (pas de pagination complète côté client).

**Composants Angular appelants** : aucun usage direct courant (le listing passe par `/search`)

**Exemple d'appel** : `GET /api/interpretes?skip=100&take=25`

---

### 3.3 `POST /api/interpretes` ??

**Rôle métier** : Créer un nouvel interprète dans le système. L'ID (`tolkcode`) est généré automatiquement via la séquence Oracle `NR_TOLK`. Le nom est normalisé en majuscules. Les téléphones et le numéro TVA sont validés côté serveur selon les formats belges.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| corps | `BODY` | `NewInterpreteDto` | ? |

**Schéma `NewInterpreteDto`** :

| Champ | Type | Requis | Validation | Description |
|---|---|---|---|---|
| `nom` | `string` | ? | Non vide | Nom — converti en `UPPER` et `Trim()` |
| `prenom` | `string?` | ? | — | Prénom — `Trim()` |
| `email` | `string?` | ? | — | Email — `Trim()` |
| `tel` | `string?` | ? | Format belge : `+32XXXXXXXXX` ou `0XXXXXXXXX` (9-12 chiffres après nettoyage `espaces . - /`) | Téléphone fixe |
| `telbis` | `string?` | ? | Idem `tel` | Téléphone secondaire |
| `gsm` | `string?` | ? | Idem `tel` | GSM |
| `tva` | `string?` | ? | Format : `BE` + 10 chiffres (après nettoyage espaces et points) | N° TVA belge — converti en `UPPER` |
| `iban` | `string?` | ? | — | IBAN — `Trim()` |
| `bankrekening` | `string?` | ? | — | Compte bancaire — `Trim()` |
| `taalrol` | `int?` | ? | Normalisé : 1 ou 2 sinon `null` | 1=NL, 2=FR |
| `beedigd` | `int?` | ? | Normalisé : 1 ? 1, sinon ? 0 | Assermenté |
| `genre` | `string?` | ? | — | Genre — `Trim()` |

**Réponse succès** :

| Code | Type | Corps | Headers |
|---|---|---|---|
| `201 Created` | `application/json` | `{ tolkcode: int, nom: string, prenom: string }` | `Location: /api/interpretes/{tolkcode}` |

**Réponse erreur** :

| Code | Corps | Condition |
|---|---|---|
| `400 Bad Request` | `{ errors: string[] }` | Un ou plusieurs champs téléphone/TVA invalides |
| `400 Bad Request` | `"Le nom est requis."` | `nom` vide ou null |
| `400 Bad Request` | `"Payload manquant."` | Corps JSON absent |

**Logique séquence** : `SELECT NR_TOLK.NEXTVAL FROM DUAL` via `DbCommand` brut (pas via EF Core `HasDefaultValueSql`).

**Composants Angular appelants** : `InterpretesService.create()` ? `InterpreteListComponent` (formulaire de création rapide)

**Exemple de requête** :
```json
{
  "nom": "Dupont",
  "prenom": "Marie",
  "gsm": "+32478123456",
  "tva": "BE 0123.456.789",
  "taalrol": 2
}
```

**Exemple de réponse** :
```json
{ "tolkcode": 2048, "nom": "DUPONT", "prenom": "Marie" }
```

---

### 3.4 `PUT /api/interpretes/{tolkcode}` ??

**Rôle métier** : Mettre à jour la fiche identité d'un interprète existant. C'est le endpoint de sauvegarde du formulaire `InterpreteDetailComponent`. Seuls les champs autorisés sont copiés (pas de remplacement aveugle de l'entité) — les champs `Rue`, `Adresnr`, `Postid` (anciens champs adresse) ne sont pas modifiables via cet endpoint.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? | Doit correspondre au `tolkcode` dans le corps |
| corps | `BODY` | `Tolkidentity` | ? | Entité complète. Le `tolkcode` dans le corps doit être identique au path |

**Champs mis à jour** : `nom`, `prenom`, `email`, `fax`, `taalrol` (normalisé 1/2/null), `beedigd` (normalisé 0/1), `dateNaissance`, `nationaliteit`, `rijksregisternr`, `herkomst`, `genre`, `beroepscode`, `btwNr`, `bankrekening`, `iban`, `tva`, `remarque`, `evaluatiecode`, `ba`, `fedcom`, `ondernemingsnummer`, `vestigingsnummer`, `fedcomnummer`, `iscce`, `gsm`, `tel`, `telbis`.

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Mise à jour effectuée |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | `tolkcode` du path ? `tolkcode` du corps, ou corps null |
| `404 Not Found` | Interprète inexistant |

**Composants Angular appelants** : `InterpretesService.saveIdentite()` ? `InterpreteDetailComponent`

---

### 3.5 `DELETE /api/interpretes/{tolkcode}` ??

**Rôle métier** : Supprimer physiquement un interprète de la table `TOLKIDENTITY`. Opération irréversible. Les enregistrements liés (`TOLKADRESSE`, `TOLKINDISPO`, `LANGUE_SOURCE`, `LANGUE_DESTINATION`, `TOLKLINK`) ne sont **pas** supprimés en cascade (pas de FK physiques) — ils deviennent orphelins.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? |

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Suppression effectuée |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `404 Not Found` | Interprète inexistant |

**Composants Angular appelants** : aucun composant Angular n'appelle directement cet endpoint dans le code actuel (pas de bouton supprimer dans l'UI)

---

### 3.6 `GET /api/interpretes/search` ??

**Rôle métier** : Recherche d'interprètes par tolkcode ou par nom, enrichie avec les langues source et destination de chaque résultat. C'est le endpoint principal du composant `InterpreteListComponent` — le formulaire de recherche rapide affiché sur la page de listing des interprètes.

**Paramètres** :

| Paramètre | Source | Type | Requis | Valeurs | Description |
|---|---|---|---|---|---|
| `mode` | `QS` | `string` | ? | `"tolkcode"`, `"nom"` | Champ sur lequel chercher |
| `q` | `QS` | `string` | ? | texte libre | Terme de recherche (converti en `UPPER` côté serveur) |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<InterpreteSearchDto>` (max 200 résultats) |

**Schéma `InterpreteSearchDto`** :

| Champ | Type | Description |
|---|---|---|
| `tolkcode` | `string` | Identifiant (sérialisé en string) |
| `nom` | `string?` | Nom |
| `prenom` | `string?` | Prénom |
| `languesDestination` | `string[]` | Libellés FR des langues destination (jointure `LANGUE_DESTINATION` ? `LANGUE`) |
| `languesSource` | `string[]` | Libellés FR des langues source (jointure `LANGUE_SOURCE` ? `LANGUE`) |

**Logique** :
1. Filtre `TOLKIDENTITY` selon le mode : `CONTAINS` sur `TOLKCODE.ToString()` ou `NOM.UPPER()`
2. Limite à 200 résultats, tri par `TOLKCODE ASC`
3. Charge en batch les langues destination et source pour tous les tolkcode trouvés (2 requêtes séparées avec `JOIN` sur `LANGUE`)
4. Assemble en mémoire via dictionnaires

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | `mode` ou `q` vide, ou `mode` invalide |

**Composants Angular appelants** : `InterpretesService.search()` ? `InterpreteListComponent`

**Exemple d'appel** : `GET /api/interpretes/search?mode=nom&q=dup`

**Exemple de réponse** :
```json
[
  {
    "tolkcode": "1055",
    "nom": "DUPONT",
    "prenom": "Marie",
    "languesDestination": ["Français", "Néerlandais"],
    "languesSource": ["Arabe", "Dari"]
  }
]
```

---

### 3.7 `GET /api/interpretes/match` ??

**Rôle métier** : Trouver les interprètes disponibles pour une paire de langues (source ? destination) à une date donnée. Les résultats sont triés par distance kilométrique (adresse active, la plus proche en premier). Utilisé quand l'utilisateur clique sur « Rechercher un interprète » depuis le calendrier pour une audience spécifique.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `langSrc` | `QS` | `int` | ? | `IDLANGUE` de la langue source (ex: 6 = Arabe) |
| `langDst` | `QS` | `int` | ? | `IDLANGUE` de la langue destination (ex: 36 = Français) |
| `date` | `QS` | `DateOnly` | ? | Date souhaitée (`YYYY-MM-DD`) — sert à exclure les indisponibles |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<InterpreteMatchDto>` (max 300 résultats) |

**Schéma `InterpreteMatchDto`** :

| Champ | Type | Description |
|---|---|---|
| `tolkcode` | `int` | Identifiant |
| `nom` | `string?` | Nom |
| `prenom` | `string?` | Prénom |
| `tel` | `string?` | Téléphone fixe |
| `telbis` | `string?` | Téléphone secondaire |
| `gsm` | `string?` | GSM |
| `languesDestination` | `string[]` | Toutes les langues destination de l'interprète (pas uniquement celle demandée) |
| `distanceKm` | `double?` | Distance en km depuis l'adresse active (`TOLKADRESSE.ENDDATE IS NULL`). `null` si pas d'adresse active |

**Logique** :
1. Filtre les interprètes ayant `langSrc` dans `LANGUE_SOURCE` ET `langDst` dans `LANGUE_DESTINATION`
2. Exclut ceux ayant une indisponibilité couvrant la `date` (`TOLKINDISPO.STARTINDISPO <= date && (ENDINDISPO IS NULL || ENDINDISPO > date)`)
3. Charge toutes les adresses actives (`ENDDATE IS NULL`) en mémoire pour résoudre le km
4. Tri : `distanceKm ASC` (nulls en dernier), puis `nom`, puis `prenom`. Limite à 300

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | `langSrc` ou `langDst` ? 0 |

**Composants Angular appelants** : `InterpretesService.match()` ? `InterpreteListComponent` (onglet recherche avancée)

**Exemple d'appel** : `GET /api/interpretes/match?langSrc=6&langDst=36&date=2025-09-19`

**Exemple de réponse** :
```json
[
  {
    "tolkcode": 1055,
    "nom": "DUPONT",
    "prenom": "Marie",
    "tel": null,
    "gsm": "0478123456",
    "languesDestination": ["Français", "Néerlandais"],
    "distanceKm": 12.0
  },
  {
    "tolkcode": 1102,
    "nom": "YILMAZ",
    "prenom": "Ahmet",
    "tel": "02 987 65 43",
    "gsm": null,
    "languesDestination": ["Français"],
    "distanceKm": 35.0
  }
]
```

---

### 3.8 `GET /api/interpretes/{tolkcode}/audiences-exact` ??

**Rôle métier** : Trouver toutes les audiences futures **non assignées** (`Tolkcode IS NULL`) qui correspondent **exactement** aux compétences linguistiques de l'interprète. La correspondance est déterminée par :
- La langue de la requête (`LangueRequete`) doit être dans les langues **source** de l'interprète
- La langue du rôle (`LangueRole` = F ? Français ID 36, N ? Néerlandais ID 77) doit être dans les langues **destination** de l'interprète
- L'interprète ne doit pas être indisponible ce jour-là

Utilisé dans le composant de convocation pour montrer les audiences proposables à un interprète.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<AudienceDto>` (dédupliqué par `(DateAudience, Nom, HeureAudience, LangueRequete, SalleAudience)`) |

**Logique** :
1. Charge les audiences VRM futures non assignées
2. Jointure `LangueRequete` ? `LANGUE.LibelleFr` ? `IDLANGUE` pour obtenir l'ID de la langue source
3. Jointure `LANGUE_SOURCE` et `LANGUE_DESTINATION` pour vérifier que l'interprète a les deux compétences
4. Exclusion des jours d'indisponibilité
5. Dédoublonnage en mémoire par `GroupBy` + `First()`

**Composants Angular appelants** : `InterpretesService.audiencesExact()` ? `ConvocationComponent`

---

### 3.9 `GET /api/interpretes/{tolkcode}/convocations` ??

**Rôle métier** : Récupérer les audiences **déjà assignées** à un interprète à partir d'aujourd'hui. Ce sont les audiences où `TOLKLINK` existe déjà (le `Tolkcode` est renseigné dans la vue VRM). Utilisé pour construire le tableau « Audiences confirmées » dans l'email de convocation.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<AudienceDto>` (dédupliqué, trié par date puis heure) |

**Schéma `AudienceDto`** (partagé avec 3.8) :

| Champ | Type | Description |
|---|---|---|
| `nroRoleGen` | `decimal` | Numéro de rôle général |
| `langueRole` | `string` | `"F"` (francophone) ou `"N"` (néerlandophone) |
| `proc` | `string` | Type de procédure |
| `dateAudience` | `DateTime` | Date de l'audience |
| `nom` | `string` | Nom du magistrat |
| `salleAudience` | `string` | Salle d'audience |
| `heureAudience` | `string` | Heure (ex: `"09:00"`) |
| `langueRequete` | `string` | Langue demandée (ex: `"Arabe"`) |
| `libelleFr` | `string` | Libellé FR de la langue CGOE |
| `langueCgoe` | `string` | Code langue CGOE |
| `idAffAudience` | `decimal` | ID technique affaire-audience |
| `tolkcode` | `decimal?` | Code interprète assigné |

**Logique** : `SELECT * FROM VUE_CALENDAR_VRM_PC WHERE TOLKCODE = {tolkcode} AND DATE_AUDIENCE >= TODAY`, dédupliqué en mémoire par `(DateAudience, Nom, HeureAudience, LangueRequete, SalleAudience)`.

**Composants Angular appelants** : `InterpretesService.convocations()` ? `ConvocationComponent`

**Exemple de réponse** :
```json
[
  {
    "nroRoleGen": 123456,
    "langueRole": "F",
    "proc": "ANNUL",
    "dateAudience": "2025-09-22T00:00:00",
    "nom": "JANSSENS",
    "salleAudience": "A12",
    "heureAudience": "09:00",
    "langueRequete": "Arabe",
    "libelleFr": "Arabe",
    "langueCgoe": "ARA",
    "idAffAudience": 789012,
    "tolkcode": 1055
  }
]
```

---

### 3.10 `GET /api/interpretes/tolkcodes` ??

**Rôle métier** : Charger la liste légère de tous les interprètes (tolkcode + nom + prénom uniquement). Utilisé pour alimenter le dropdown de la modal d'assignation dans le calendrier. Chargé une seule fois côté client lors de la première ouverture de la modal, puis réutilisé.

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<TolkcodeItem>` |

**Schéma `TolkcodeItem`** (objet anonyme) :

| Champ | Type | Description |
|---|---|---|
| `tolkcode` | `int` | Identifiant |
| `nom` | `string` | Nom |
| `prenom` | `string` | Prénom |

**Logique** : `SELECT TOLKCODE, NOM, PRENOM FROM TOLKIDENTITY ORDER BY TOLKCODE`. Charge la totalité de la table (pas de pagination).

**Composants Angular appelants** : `InterpretesService.listAllTolkcodes()` ? `CalendarComponent.openAssignModal()`

**Exemple de réponse** :
```json
[
  { "tolkcode": 1001, "nom": "AHMED", "prenom": "Hassan" },
  { "tolkcode": 1002, "nom": "BERGER", "prenom": "Claude" },
  { "tolkcode": 1055, "nom": "DUPONT", "prenom": "Marie" }
]
```

---

## 4. Tolklink

**Contrôleur** : `TolklinkController` — **Fichier** : `TolklinkController.cs`
**Route de base** : `/api/interpretes/{tolkcode}/tolklink`
**Autorisation** : ?? Aucune
**Source de données** : Table `TOLKLINK`. Lien N:N entre un interprète et une affaire-audience. Le soft-delete utilise la colonne `DATE_SUPP` (non null = supprimé).

---

### 4.1 `POST /api/interpretes/{tolkcode}/tolklink` ??

**Rôle métier** : Assigner un interprète à une audience. Crée un enregistrement dans la table `TOLKLINK` avec `DATECREATE = NOW` et `USERCREATE = "api"`. La vue calendrier reflètera immédiatement cette assignation. Utilisé dans la modal d'assignation du calendrier.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? | Identifiant de l'interprète à assigner |
| corps | `BODY` | `NewTolklinkDto` | ? | `{ nrAffAudience: int }` — ID de l'affaire-audience cible |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `{ id: int }` — `IdTolklink` de l'enregistrement créé |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | `nrAffAudience` ? 0 ou corps null |
| `409 Conflict` | Un lien actif existe déjà pour ce couple `(tolkcode, nrAffAudience)` (vérifié via `COUNT` Oracle-safe) |

**Logique anti-doublon** : `SELECT COUNT(*) FROM TOLKLINK WHERE TOLKCODE = :tk AND NR_AFF_AUDIENCE = :aff AND DATE_SUPP IS NULL`. Utilise `COUNT` au lieu de `Any()` pour contourner un problème Oracle avec les prédicats booléens.

**Composants Angular appelants** : `InterpretesService.addTolklink()` ? `CalendarComponent.assignTolk()`

**Exemple de requête** :
```json
{ "nrAffAudience": 789012 }
```

**Exemple de réponse** :
```json
{ "id": 45678 }
```

---

### 4.2 `DELETE /api/interpretes/{tolkcode}/tolklink/{idAffAudience}` ??

**Rôle métier** : Désassigner un interprète d'une audience. Il s'agit d'un **soft-delete** : la colonne `DATE_SUPP` est mise à `DateTime.Now` (l'enregistrement n'est pas physiquement supprimé). La vue calendrier ne montrera plus l'assignation. Utilisé quand l'utilisateur clique sur « Supprimer l'affectation » dans le calendrier.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? | Interprète |
| `idAffAudience` | `PATH` | `int` | ? | `NR_AFF_AUDIENCE` du lien à supprimer |

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Soft-delete effectué (`DATE_SUPP` et `DATEMODIF` mis à `DateTime.Now`) |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `404 Not Found` | Aucun lien actif trouvé pour ce couple `(tolkcode, nrAffAudience)` avec `DATE_SUPP IS NULL` |

**Composants Angular appelants** : `InterpretesService.removeTolklink()` ? `CalendarComponent.removeAssignment()`

---

### 4.3 `POST /api/interpretes/{tolkcode}/tolklink/bulk` ??

**Rôle métier** : Assigner un interprète à **plusieurs audiences** en une seule requête. Les IDs déjà assignés sont silencieusement ignorés (pas d'erreur). Utilisé dans le workflow de convocation quand l'utilisateur sélectionne plusieurs audiences puis confirme.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? | Interprète |
| corps | `BODY` | `BulkNewTolklinkDto` | ? | `{ ids: int[] }` — tableau d'IDs affaire-audience |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `{ inserted: int, skipped: int }` |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | `ids` null ou tableau vide |

**Logique** :
1. Dédoublonne les IDs fournis (`Distinct()`)
2. Charge les IDs déjà assignés activement (`DATE_SUPP IS NULL`)
3. Insère uniquement les nouveaux (différence ensembliste `Except`)
4. Chaque insertion : `DATECREATE = NOW`, `USERCREATE = "api"`
5. `SaveChangesAsync()` unique pour toutes les insertions

**Composants Angular appelants** : aucun usage direct dans le code Angular actuel (l'assignation passe par l'endpoint unitaire 4.1)

**Exemple de requête** :
```json
{ "ids": [789012, 789013, 789014] }
```

**Exemple de réponse** :
```json
{ "inserted": 2, "skipped": 1 }
```

---

## 5. Adresses

**Contrôleur** : `AdressesController` — **Fichier** : `AdressesController.cs`
**Route de base** : `/api/interpretes/{tolkcode}/adresses` et `/api/adresses/{id}`
**Autorisation** : ?? Aucune
**Source de données** : Table `TOLKADRESSE`. `TOLKCODE` est un `VARCHAR2(5)` (FK logique vers `TOLKIDENTITY.TOLKCODE` qui est un `NUMBER`). La séquence `NR_AUTO_ADRESSE` est utilisée manuellement pour générer `ID_ADRESSE`.

**Concept « adresse active »** : une adresse est considérée comme active si `ENDDATE IS NULL`. Un interprète ne devrait avoir qu'une seule adresse active à la fois (la plus récente).

---

### 5.1 `GET /api/interpretes/{tolkcode}/adresses` ??

**Rôle métier** : Lister les adresses d'un interprète, avec option de ne retourner que l'adresse active. Utilisé dans le panneau « Adresse » de la fiche interprète pour afficher l'historique des adresses.

**Paramètres** :

| Paramètre | Source | Type | Requis | Défaut | Description |
|---|---|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? | — | Interprète (converti en string pour la comparaison avec `TOLKADRESSE.TOLKCODE`) |
| `onlyActive` | `QS` | `bool` | ? | `false` | Si `true`, filtre sur `ENDDATE IS NULL` |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<Tolkadresse>` (triées par `STARTDATE DESC`) |

**Schéma `Tolkadresse`** :

| Champ | Type | Colonne Oracle | Description |
|---|---|---|---|
| `idAdresse` | `int` | `ID_ADRESSE` | PK |
| `tolkcode` | `string` | `TOLKCODE` | FK logique (VARCHAR2) |
| `land` | `string` | `LAND` | Code pays ISO 2 lettres (ex: `"BE"`) |
| `cp` | `string` | `CP` | Code postal |
| `commune` | `string` | `COMMUNE` | Commune |
| `rue` | `string?` | `RUE` | Rue |
| `numero` | `string?` | `NUMERO` | Numéro |
| `boite` | `string?` | `BOITE` | Boîte |
| `km` | `byte?` | `KM` | Distance en km jusqu'au tribunal |
| `startdate` | `DateTime` | `STARTDATE` | Date de début de validité |
| `enddate` | `DateTime?` | `ENDDATE` | Date de fin (`null` = adresse active) |
| `datecreate` | `DateTime` | `DATECREATE` | Date de création (audit) |
| `usercreate` | `string?` | `USERCREATE` | Créateur (audit) |
| `datemodif` | `DateTime?` | `DATEMODIF` | Date de modification (audit) |
| `usermodif` | `string?` | `USERMODIF` | Modificateur (audit) |

**Composants Angular appelants** : `AdressesService.list()` ? `InterpreteDetailComponent` (section Adresse)

---

### 5.2 `POST /api/interpretes/{tolkcode}/adresses` ??

**Rôle métier** : Ajouter une nouvelle adresse à un interprète sans clôturer l'adresse existante. Utile pour ajouter une adresse avec une date de début future ou un historique.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? | Interprète |
| corps | `BODY` | `Tolkadresse` | ? | Adresse à créer |

**Champs auto-remplis** : `tolkcode` (depuis le path), `datecreate` (UTC now), `usercreate` (`User.Identity.Name` ou `"system"`), `idAdresse` (séquence `NR_AUTO_ADRESSE.NEXTVAL` si non fourni ou 0).

**Réponse succès** :

| Code | Type | Corps | Headers |
|---|---|---|---|
| `201 Created` | `application/json` | `Tolkadresse` (entité complète créée) | `Location: /api/adresses/{id}` |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | Corps null, ou `LAND` absent ou ? 2 caractères |
| `404 Not Found` | Interprète inexistant dans `TOLKIDENTITY` |

**Composants Angular appelants** : `AdressesService.create()` ? `InterpreteDetailComponent`

---

### 5.3 `POST /api/interpretes/{tolkcode}/adresses/replace` ??

**Rôle métier** : Remplacer l'adresse active par une nouvelle, dans une **transaction**. L'adresse active précédente est clôturée (`ENDDATE = startdate - 1 jour`) puis la nouvelle est créée avec `ENDDATE = NULL`. C'est l'opération principale de changement d'adresse dans l'UI.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? | Interprète |
| corps | `BODY` | `Tolkadresse` | ? | Nouvelle adresse. `startdate` est requis (détermine la date de clôture de l'ancienne) |

**Réponse succès** :

| Code | Type | Corps | Headers |
|---|---|---|---|
| `201 Created` | `application/json` | `Tolkadresse` (nouvelle adresse créée) | `Location: /api/adresses/{id}` |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | Corps null, `startdate` par défaut, ou `LAND` invalide |
| `404 Not Found` | Interprète inexistant |

**Logique transactionnelle** :
1. `BEGIN TRANSACTION`
2. Cherche l'adresse active (`ENDDATE IS NULL`, plus récente `STARTDATE`)
3. Si trouvée : `ENDDATE = nouvelle.Startdate - 1 jour`, `DATEMODIF = NOW`, `USERMODIF = user`
4. `SaveChanges()`
5. Crée la nouvelle adresse avec `ENDDATE = NULL`, `ID_ADRESSE = NR_AUTO_ADRESSE.NEXTVAL`
6. `SaveChanges()`
7. `COMMIT`

**Composants Angular appelants** : `AdressesService.replace()` ? `InterpreteDetailComponent`

**Exemple de requête** :
```json
{
  "land": "BE",
  "cp": "1030",
  "commune": "SCHAERBEEK",
  "rue": "Rue Gaucheret",
  "numero": "92",
  "km": 15,
  "startdate": "2025-10-01"
}
```

---

### 5.4 `GET /api/adresses/{id}` ??

**Rôle métier** : Récupérer une adresse par son ID technique. Endpoint utilisé comme cible du header `Location` retourné par les endpoints de création (5.2 et 5.3). Rarement appelé directement par le frontend.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `id` | `PATH` | `int` | ? |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Tolkadresse` |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `404 Not Found` | Adresse inexistante |

**Composants Angular appelants** : `AdressesService.getOne()` (rarement utilisé)

---

### 5.5 `PUT /api/adresses/{id}` ??

**Rôle métier** : Modifier une adresse existante. Met à jour les champs d'adresse et les dates de validité. Les champs d'audit (`datemodif`, `usermodif`) sont automatiquement renseignés.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `id` | `PATH` | `int` | ? |
| corps | `BODY` | `Tolkadresse` | ? |

**Champs modifiables** : `land` (validé 2 car., converti en UPPER), `cp`, `commune`, `rue`, `numero`, `boite`, `km`, `startdate` (si non default), `enddate`.

**Champs auto-remplis** : `datemodif` (UTC now), `usermodif` (`User.Identity.Name` ou `"system"`).

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Modification effectuée |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | Corps null, ou `LAND` fourni mais ? 2 caractères |
| `404 Not Found` | Adresse inexistante |

**Composants Angular appelants** : `AdressesService.update()` ? `InterpreteDetailComponent`

---

### 5.6 `DELETE /api/adresses/{id}` ??

**Rôle métier** : Supprimer physiquement une adresse. Opération irréversible. L'enregistrement est retiré de la table `TOLKADRESSE`.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `id` | `PATH` | `int` | ? |

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Suppression effectuée |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `404 Not Found` | Adresse inexistante |

**Composants Angular appelants** : `AdressesService.remove()` ? `InterpreteDetailComponent`

**Note** : le contrôleur expose aussi `GET /api/adresses/{id}` (5.4), `PUT /api/adresses/{id}` (5.5) et `DELETE /api/adresses/{id}` (5.6) avec la route de base `/api/adresses/` (sans tolkcode) car ces opérations n'ont besoin que de l'ID technique de l'adresse.

---

## 6. Langues

**Contrôleur** : `LanguesController` — **Fichier** : `LanguesController.cs`
**Route de base** : `/api` (routes multiples : `/api/langues`, `/api/interpretes/{tolkcode}/langues/...`)
**Autorisation** : ?? Aucune
**Source de données** : Tables `LANGUE` (référentiel ~100 langues), `LANGUE_SOURCE` (langues maîtrisées par l'interprète), `LANGUE_DESTINATION` (langues cibles — FR/NL principalement). Les séquences Oracle `NR_AUTO_LANGUE_SOURCE` et `NR_AUTO_DESTINATION` sont utilisées manuellement pour générer les PK.

---

### 6.1 `GET /api/langues` ??

**Rôle métier** : Charger le référentiel complet des langues pour alimenter les `<select>` côté Angular (ajout de langue source ou destination). L'option `destOnly` filtre uniquement les langues ayant `ISLANGUE_DESTINATION IS NOT NULL` — utilisé dans le formulaire d'ajout de langue destination.

**Paramètres** :

| Paramètre | Source | Type | Requis | Défaut | Description |
|---|---|---|---|---|---|
| `destOnly` | `QS` | `bool` | ? | `false` | Si `true`, ne retourne que les langues marquées comme destination |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<LangueDto>` (trié par `LibelleFr ASC`) |

**Schéma `LangueDto`** :

| Champ | Type | Description |
|---|---|---|
| `idlangue` | `byte?` | PK — identifiant de la langue |
| `codeIso` | `string?` | Code ISO (ex: `"ARA"`, `"FRA"`) |
| `libelleFr` | `string?` | Libellé en français (ex: `"Arabe"`) |
| `libelleNl` | `string?` | Libellé en néerlandais (ex: `"Arabisch"`) |

**Composants Angular appelants** : `LanguesService` ? `InterpreteDetailComponent` (dropdown ajout langue)

**Exemple de réponse** :
```json
[
  { "idlangue": 6, "codeIso": "ARA", "libelleFr": "Arabe", "libelleNl": "Arabisch" },
  { "idlangue": 36, "codeIso": "FRA", "libelleFr": "Français", "libelleNl": "Frans" },
  { "idlangue": 77, "codeIso": "NLD", "libelleFr": "Néerlandais", "libelleNl": "Nederlands" }
]
```

---

### 6.2 `GET /api/interpretes/{tolkcode}/langues/sources` ??

**Rôle métier** : Lister les langues source maîtrisées par un interprète (les langues qu'il comprend/traduit depuis). Utilisé dans la fiche interprète, section « Langues ».

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<LangueSourceDto>` (trié par `LibelleFr`, `LibelleNl`) |

**Schéma `LangueSourceDto`** :

| Champ | Type | Description |
|---|---|---|
| `idLangueSource` | `int` | PK (`ID_LANGUESOURCE`) |
| `tolkcode` | `int` | FK interprète |
| `nrLangue` | `int` | FK vers `LANGUE.IDLANGUE` |
| `libelleFr` | `string?` | Libellé FR (via `LEFT JOIN` sur `LANGUE`) |
| `libelleNl` | `string?` | Libellé NL |

**Logique** : `LEFT JOIN` entre `LANGUE_SOURCE` et `LANGUE` sur `NR_LANGUE = IDLANGUE` pour enrichir avec les libellés.

**Composants Angular appelants** : `InterpretesService.listLangSource()` ? `InterpreteDetailComponent`

---

### 6.3 `POST /api/interpretes/{tolkcode}/langues/source` ??

**Rôle métier** : Ajouter une langue source à un interprète. Vérifie l'existence de l'interprète et de la langue, et empêche les doublons. L'ID est généré via la séquence Oracle `NR_AUTO_LANGUE_SOURCE`.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? | Interprète |
| corps | `BODY` | `AddLangueDto` | ? | `{ nrLangue: int }` — `IDLANGUE` de la langue à ajouter |

**Réponse succès** :

| Code | Type | Corps | Headers |
|---|---|---|---|
| `201 Created` | `application/json` | `{ id: int }` — PK créée | `Location: /api/interpretes/{tolkcode}/langues/sources` |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | `nrLangue` null ou ? 0, ou langue inconnue dans `LANGUE` |
| `404 Not Found` | Interprète inexistant dans `TOLKIDENTITY` |
| `409 Conflict` | Cette langue source existe déjà pour cet interprète |

**Composants Angular appelants** : `LanguesService` ? `InterpreteDetailComponent`

---

### 6.4 `DELETE /api/interpretes/{tolkcode}/langues/source/{id}` ??

**Rôle métier** : Supprimer physiquement une langue source d'un interprète.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? | Interprète |
| `id` | `PATH` | `int` | ? | `ID_LANGUESOURCE` (PK) |

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Suppression effectuée |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `404 Not Found` | Enregistrement introuvable (vérification conjointe `id` + `tolkcode`) |

**Composants Angular appelants** : `LanguesService` ? `InterpreteDetailComponent`

---

### 6.5 `GET /api/interpretes/{tolkcode}/langues/destination` ??

**Rôle métier** : Lister les langues destination d'un interprète (les langues vers lesquelles il traduit — typiquement Français et/ou Néerlandais). Utilisé dans la fiche interprète et pour le matching d'audiences.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<LangueDestinationDto>` (trié par `LibelleFr ASC`) |

**Schéma `LangueDestinationDto`** :

| Champ | Type | Description |
|---|---|---|
| `idLanguedestination` | `int` | PK |
| `tolkcode` | `int` | FK interprète |
| `nrLangue` | `int` | FK vers `LANGUE.IDLANGUE` |
| `codeIso` | `string?` | Code ISO de la langue |
| `libelleFr` | `string?` | Libellé FR |
| `libelleNl` | `string?` | Libellé NL |

**Logique** : `INNER JOIN` entre `LANGUE_DESTINATION` et `LANGUE` sur `NR_LANGUE = IDLANGUE` (cast en `int` des deux côtés pour compatibilité Oracle `byte`/`int`).

**Composants Angular appelants** : `InterpretesService.listLangDest()` ? `InterpreteDetailComponent`

---

### 6.6 `POST /api/interpretes/{tolkcode}/langues/destination` ??

**Rôle métier** : Ajouter une langue destination à un interprète. Mêmes vérifications que 6.3 mais sur la table `LANGUE_DESTINATION`. L'ID est généré via la séquence Oracle `NR_AUTO_DESTINATION`.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? | Interprète |
| corps | `BODY` | `AddLangueDto` | ? | `{ nrLangue: int }` |

**Réponse succès** :

| Code | Type | Corps | Headers |
|---|---|---|---|
| `201 Created` | `application/json` | `{ id: int }` | `Location: /api/interpretes/{tolkcode}/langues/destination` |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | `nrLangue` invalide ou langue inconnue |
| `404 Not Found` | Interprète inexistant |
| `409 Conflict` | Doublon |

**Composants Angular appelants** : `LanguesService` ? `InterpreteDetailComponent`

---

### 6.7 `DELETE /api/interpretes/{tolkcode}/langues/destination/{id}` ??

**Rôle métier** : Supprimer physiquement une langue destination d'un interprète.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? | Interprète |
| `id` | `PATH` | `int` | ? | `ID_LANGUEDESTINATION` (PK) |

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Suppression effectuée |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `404 Not Found` | Enregistrement introuvable |

**Composants Angular appelants** : `LanguesService` ? `InterpreteDetailComponent`

---

## 7. TVA

**Contrôleur** : `TvaController` — **Fichier** : `TvaController.cs`
**Route de base** : `/api/interpretes/{tolkcode}/tva` et `/api/tva`
**Autorisation** : ?? Aucune
**Source de données** : Table `TOLK_TVA` (historique des statuts TVA par interprète), table `STATUT` (référentiel des statuts : assujetti, non-assujetti, exempté…). Mapping AutoMapper via `ProjectTo<TvaRowDto>`.

---

### 7.1 `GET /api/interpretes/{tolkcode}/tva` ??

**Rôle métier** : Récupérer l'historique complet des statuts TVA d'un interprète, avec le libellé textuel de chaque statut. Affiché dans la section « TVA » de la fiche interprète. Le statut actif est celui dont `EndDate IS NULL`.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<TvaRowDto>` (trié par `StartDate ASC`) |

**Schéma `TvaRowDto`** :

| Champ | Type | Description |
|---|---|---|
| `idTva` | `int` | PK (`ID_TOLK_TVA`) |
| `tolkcode` | `int` | FK interprète |
| `idStatut` | `byte` | FK vers `STATUT.ID_STATUT` |
| `statut` | `string` | Libellé du statut (ex: `"Assujetti"`, `"Non-assujetti"`) — enrichi en mémoire via dictionnaire |
| `startdate` | `DateTime?` | Date de début du statut |
| `enddate` | `DateTime?` | Date de fin (`null` = statut actif) |

**Logique** :
1. `ProjectTo<TvaRowDto>` via AutoMapper (sans le libellé)
2. Charge tous les statuts en dictionnaire `IdStatut ? TypeStatut`
3. Complète la propriété `Statut` en mémoire

**Composants Angular appelants** : `InterpretesService.getTva()` ? `InterpreteDetailComponent`

**Exemple de réponse** :
```json
[
  { "idTva": 1, "tolkcode": 1055, "idStatut": 2, "statut": "Non-assujetti", "startdate": "2020-01-01T00:00:00", "enddate": "2024-12-31T00:00:00" },
  { "idTva": 5, "tolkcode": 1055, "idStatut": 1, "statut": "Assujetti", "startdate": "2025-01-01T00:00:00", "enddate": null }
]
```

---

### 7.2 `POST /api/interpretes/{tolkcode}/tva` ??

**Rôle métier** : Ajouter un nouveau statut TVA pour un interprète. Clôture automatiquement le statut précédent (celui dont `EndDate IS NULL`) en mettant `EndDate = Startdate(nouveau) - 1 jour`. Si la date de fin calculée est antérieure à la date de début du statut précédent, elle est forcée à cette date de début.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? | Interprète |
| corps | `BODY` | `NewTvaDto` | ? | Nouveau statut |

**Schéma `NewTvaDto`** :

| Champ | Type | Requis | Description |
|---|---|---|---|
| `idStatut` | `byte` | ? | FK vers `STATUT.ID_STATUT` (ex: 1=Assujetti, 2=Non-assujetti) |
| `startdate` | `DateTime` | ? | Date de début du nouveau statut |

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Statut ajouté, ancien clôturé |

**Logique** :
1. Cherche le statut ouvert (`EndDate IS NULL`, le plus récent)
2. Si trouvé : `EndDate = max(StartDate, Startdate(nouveau) - 1 jour)`
3. Insère le nouveau avec `EndDate = NULL`

**Composants Angular appelants** : `InterpretesService.saveTva()` ? `InterpreteDetailComponent`

---

### 7.3 `GET /api/tva/statuts` ??

**Rôle métier** : Charger le référentiel des statuts TVA pour alimenter le `<select>` dans le formulaire d'ajout de statut TVA.

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<StatutDto>` (trié par `IdStatut ASC`) |

**Schéma `StatutDto`** :

| Champ | Type | Description |
|---|---|---|
| `idStatut` | `byte` | PK |
| `typeStatut` | `string` | Libellé (ex: `"Assujetti"`, `"Non-assujetti"`, `"Exempté"`) |

**Composants Angular appelants** : `TvaService` ? `InterpreteDetailComponent`

**Exemple de réponse** :
```json
[
  { "idStatut": 1, "typeStatut": "Assujetti" },
  { "idStatut": 2, "typeStatut": "Non-assujetti" },
  { "idStatut": 3, "typeStatut": "Exempté" }
]
```

---

## 8. Indisponibilités

**Contrôleur** : `IndispoController` — **Fichier** : `IndispoController.cs`
**Route de base** : `/api/interpretes/{tolkcode}/indispo`
**Autorisation** : ?? Aucune
**Source de données** : Table `TOLKINDISPO`. Le `TOLKCODE` est stocké en `VARCHAR2(5)` (même particularité que `TOLKADRESSE`). Mapping AutoMapper via `ProjectTo<IndispoDto>`. Les colonnes `DATECREATE`/`USERCREATE` sont remplies automatiquement.

---

### 8.1 `GET /api/interpretes/{tolkcode}/indispo` ??

**Rôle métier** : Lister toutes les périodes d'indisponibilité d'un interprète, triées chronologiquement. La période ouverte (sans `Endindispo`) représente l'indisponibilité en cours. Affiché dans la section « Indisponibilités » de la fiche interprète.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<IndispoDto>` (trié par `Startindispo ASC`) |

**Schéma `IndispoDto`** :

| Champ | Type | Description |
|---|---|---|
| `idIndispo` | `short` | PK (`ID_INDISPO`) |
| `tolkcode` | `string` | FK interprète (VARCHAR2) |
| `startindispo` | `DateTime` | Date de début |
| `endindispo` | `DateTime?` | Date de fin (`null` = en cours) |
| `motifindispo` | `string?` | Motif |
| `commentaire` | `string?` | Commentaire libre |

**Composants Angular appelants** : `InterpretesService.listIndispos()` ? `InterpreteDetailComponent`

**Exemple de réponse** :
```json
[
  { "idIndispo": 10, "tolkcode": "1055", "startindispo": "2025-07-01T00:00:00", "endindispo": "2025-07-15T00:00:00", "motifindispo": "Vacances", "commentaire": null },
  { "idIndispo": 15, "tolkcode": "1055", "startindispo": "2025-12-20T00:00:00", "endindispo": null, "motifindispo": "Maladie", "commentaire": "Certificat médical reçu" }
]
```

---

### 8.2 `POST /api/interpretes/{tolkcode}/indispo` ??

**Rôle métier** : Ajouter une période d'indisponibilité pour un interprète. Intègre deux mécanismes de sécurité :
1. **Clôture automatique** : si une période ouverte (`Endindispo IS NULL`) existe, elle est clôturée à `Startdate(nouveau) - 1 jour`
2. **Anti-chevauchement** : la nouvelle période est vérifiée en mémoire contre toutes les périodes existantes. Si chevauchement détecté ? `409 Conflict`

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? | Interprète |
| corps | `BODY` | `NewIndispoDto` | ? | Période à ajouter |

**Schéma `NewIndispoDto`** :

| Champ | Type | Requis | Description |
|---|---|---|---|
| `startindispo` | `DateTime` | ? | Date de début |
| `endindispo` | `DateTime?` | ? | Date de fin (`null` = indisponibilité indéfinie) |
| `motifindispo` | `string?` | ? | Motif |
| `commentaire` | `string?` | ? | Commentaire libre |

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Période ajoutée |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | `Endindispo < Startindispo` |
| `409 Conflict` | Chevauchement détecté avec une période existante (vérification en mémoire : `start < existingEnd && end > existingStart`) |

**Champs auto-remplis** : `tolkcode` (converti en string depuis le path), `datecreate` = `DateTime.Now`, `usercreate` = `"api"`.

**Composants Angular appelants** : `InterpretesService.addIndispo()` ? `InterpreteDetailComponent`

---

### 8.3 `DELETE /api/interpretes/{tolkcode}/indispo/{id}` ??

**Rôle métier** : Supprimer physiquement une période d'indisponibilité. Opération irréversible.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? |
| `id` | `PATH` | `int` | ? |

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Suppression effectuée |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `404 Not Found` | Enregistrement introuvable (vérification conjointe `id` + `tolkcode` en string) |

**Composants Angular appelants** : `IndispoService` ? `InterpreteDetailComponent`

---

## 9. Prestations

**Contrôleur** : `PrestationsController` — **Fichier** : `PrestationsController.cs`
**Route de base** : `/api/prestations`
**Autorisation** : ?? Aucune
**Source de données** : Tables `PRESTATION`, `PAIEMENT`, `TOLKLINK`, `INDEXATION` (barèmes), vues `VUE_CALENDAR_VRM_PC` et `VUE_CALENDAR_ANN`. Les séquences Oracle `NR_AUTO_PAIEMENT` et `ID_PRESTATION_AUTO` sont utilisées manuellement.

**Constante** : `TVA_RATE = 0.21m` (21 % de TVA belge).

---

### 9.1 `GET /api/prestations/jour` ??

**Rôle métier** : Récupérer la liste des interprètes mobilisés pour un jour donné avec leurs audiences liées, l'heure suggérée et l'état de prestation (déjà encodée ou non). C'est le endpoint principal de la page « Prestations du jour » qui permet à l'opérateur d'encoder les heures de début/fin.

**Paramètres** :

| Paramètre | Source | Type | Requis | Défaut | Description |
|---|---|---|---|---|---|
| `date` | `QS` | `DateOnly` | ? | — | Date du jour à consulter |
| `includeAbsents` | `QS` | `bool` | ? | `false` | Inclure les interprètes marqués absents ce jour |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<PrestationJourRowDto>` (trié par `Nom`, `Prenom`) |

**Schéma `PrestationJourRowDto`** :

| Champ | Type | Description |
|---|---|---|
| `tolkcode` | `string` | Identifiant interprète |
| `nom` | `string` | Nom |
| `prenom` | `string` | Prénom |
| `telephone` | `string` | Concaténation GSM / Tel / Telbis (séparés par ` / `) |
| `idAffAudiences` | `int[]` | IDs affaire-audience liés (via `TOLKLINK`) |
| `heureAudienceSuggee` | `string?` | Heure la plus tôt des audiences du jour (ex: `"09:00"`) |
| `hasPrestation` | `bool` | `true` si au moins un `TOLKLINK.IdPrestation` est renseigné |
| `prestations` | `int[]` | IDs des prestations existantes |
| `isAbsent` | `bool` | `true` si `TOLKLINK.Datesupp` est ce jour (marqué absent) |

**Logique** :
1. Union VRM + ANN : récupère les `TOLKLINK` actifs ce jour via `JOIN` sur `IdAffAudience`
2. Groupement par `Tolkcode` avec agrégation des audiences, prestations et heure min
3. Fallback : si les vues calendrier sont vides, charge les `PRESTATION` directement pour l'historique
4. Fusion calendrier + fallback sans doublons
5. Enrichissement avec `TOLKIDENTITY` pour nom/prénom/téléphone

**Composants Angular appelants** : `PrestationsService` ? `PrestationsJourComponent`

---

### 9.2 `POST /api/prestations` ??

**Rôle métier** : Encoder une prestation pour un interprète. Crée dans une **transaction** :
1. Un `PAIEMENT` (montant calculé automatiquement)
2. Une `PRESTATION` (heures début/fin)
3. Lie les `TOLKLINK` correspondants à la prestation
4. Calcule le montant selon les règles métier (barème indexé, arrondi 15 min, minimum 75 min, transport 1×/jour, TVA si assujetti)

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| corps | `BODY` | `NewPrestationDto` | ? |

**Schéma `NewPrestationDto`** :

| Champ | Type | Requis | Description |
|---|---|---|---|
| `tolkcode` | `string` | ? | Identifiant interprète |
| `datePrestation` | `DateTime` | ? | Date du jour |
| `startheure` | `DateTime` | ? | Heure de début (seule la partie heure est utilisée) |
| `endheure` | `DateTime` | ? | Heure de fin (doit être > startheure) |
| `idAffAudiences` | `int[]` | ? | IDs affaire-audience à lier |

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Prestation créée, paiement calculé |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | Payload null, ou `endheure ? startheure` |
| `404 Not Found` | Interprète inexistant |

**Règles de calcul du montant** (méthode `CalculerEtMettreAJourPaiementAsync`) :
1. **Durée** : `endheure - startheure` en minutes, arrondie au quart d'heure supérieur (`Math.Ceiling(raw / 15) × 15`)
2. **Montant** : si durée ? 75 min ? forfait `INDEXATION.EURO75MIN` ; sinon ? forfait + surplus × `EUROHEURE / 60`
3. **Transport** : `EUROKM × min(100, 2 × KM)` — payé **1 seule fois par jour** (vérifie si une autre prestation ce jour a déjà un transport > 0)
4. **TVA** : si `TOLK_TVA.IdStatut == 1` (assujetti) à cette date ? `baseHT × 21 %`
5. **Total** : `montant + transport + tva`

**Composants Angular appelants** : `PrestationsService` ? `PrestationsJourComponent`

---

### 9.3 `POST /api/prestations/absence` ??

**Rôle métier** : Marquer un interprète comme absent pour une ou plusieurs audiences d'un jour. Met `TOLKLINK.DATESUPP = datePrestation` (soft-delete daté). L'interprète ne sera plus considéré comme mobilisé pour ces audiences.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| corps | `BODY` | `AbsenceDto` | ? |

**Schéma `AbsenceDto`** :

| Champ | Type | Requis | Description |
|---|---|---|---|
| `tolkcode` | `string` | ? | Interprète |
| `idAffAudiences` | `int[]` | ? | IDs affaire-audience pour lesquels marquer l'absence |
| `datePrestation` | `DateTime` | ? | Date (valeur mise dans `DATESUPP`) |

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Absence enregistrée |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | Payload null ou aucune audience spécifiée |
| `404 Not Found` | Tolkcode invalide ou lien TOLKLINK introuvable/déjà supprimé |

**Composants Angular appelants** : `PrestationsService` ? `PrestationsJourComponent`

---

### 9.4 `POST /api/prestations/remplacement` ??

**Rôle métier** : Remplacer l'interprète assigné à une audience par un autre. Met à jour `TOLKLINK.TOLKCODE` de l'ancien vers le nouveau interprète. Le nouveau doit exister dans `TOLKIDENTITY`.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| corps | `BODY` | `RemplacementDto` | ? |

**Schéma `RemplacementDto`** :

| Champ | Type | Requis | Description |
|---|---|---|---|
| `idAffAudience` | `int` | ? | ID affaire-audience cible |
| `ancienTolkcode` | `string` | ? | Tolkcode de l'interprète actuel |
| `nouveauTolkcode` | `string` | ? | Tolkcode du remplaçant |

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Remplacement effectué |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | Payload null |
| `404 Not Found` | Ancien ou nouveau tolkcode invalide, nouveau interprète inexistant, ou lien TOLKLINK introuvable/déjà supprimé |

**Composants Angular appelants** : `PrestationsService` ? `PrestationsJourComponent`

---

## 10. Paiements

**Contrôleur** : `PaiementsController` — **Fichier** : `PaiementsController.cs`
**Route de base** : `/api/paiements`
**Autorisation** : ?? Aucune
**Source de données** : Tables `PAIEMENT` (montants), `PRESTATION` (heures), `TOLKIDENTITY` (identités), `TOLKADRESSE` (km pour le calcul transport). Génération PDF via QuestPDF (`FacturesBatchPdfDocument`).

---

### 10.1 `GET /api/paiements/mois` ??

**Rôle métier** : Récapitulatif mensuel des paiements groupés par interprète. Affiche un tableau avec le nombre de prestations, le total montant, transport, TVA et total pour chaque interprète ayant été payé ce mois. Utilisé dans la page « Paiements » pour la vue d'ensemble mensuelle.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `month` | `QS` | `string` | ? | Format `YYYY-MM` (ex: `2025-12`). Parsé via `TryParseMonth` ? `d0=1er du mois`, `d1=1er du mois suivant` |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<PaiementMoisInterpreteRowDto>` (trié par `Nom`, `Prenom`) |

**Schéma `PaiementMoisInterpreteRowDto`** :

| Champ | Type | Description |
|---|---|---|
| `tolkcode` | `string` | Identifiant |
| `nom` | `string` | Nom |
| `prenom` | `string` | Prénom |
| `taalrol` | `int?` | 1=FR, 2=NL |
| `nbPrestations` | `int` | Nombre de prestations du mois |
| `montant` | `decimal` | Total prestation (arrondi 2 déc.) |
| `transport` | `decimal` | Total transport |
| `montantTva` | `decimal` | Total TVA |
| `total` | `decimal` | Total TTC |
| `idFacture` | `int?` | ID facture si déjà facturé |

**Logique** : `JOIN PAIEMENT ? PRESTATION` filtré sur `DatePrestation ? [d0, d1[`, groupé par `Tolkcode`, enrichi avec `TOLKIDENTITY`.

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | Format `month` invalide |

**Composants Angular appelants** : `PaiementsService` ? `PaiementsComponent`

**Exemple d'appel** : `GET /api/paiements/mois?month=2025-12`

---

### 10.2 `GET /api/paiements/mois/{tolkcode}` ??

**Rôle métier** : Détail des paiements d'un interprète pour un mois donné — chaque ligne de prestation avec heures, durée, km, montant et transport. Inclut les totaux. Utilisé quand on clique sur un interprète dans le récapitulatif mensuel.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `tolkcode` | `PATH` | `int` | ? | Interprète (route constraint `:int` pour éviter que `"pdf"` soit capturé) |
| `month` | `QS` | `string` | ? | Format `YYYY-MM` |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `PaiementMoisDetailDto` |

**Schéma `PaiementMoisDetailDto`** :

| Champ | Type | Description |
|---|---|---|
| `tolkcode` | `string` | Identifiant |
| `nom` | `string` | Nom |
| `prenom` | `string` | Prénom |
| `taalrol` | `int?` | 1=FR, 2=NL |
| `rows` | `Array<PaiementMoisDetailRowDto>` | Lignes de détail |
| `totaux` | `PaiementMoisTotauxDto` | Totaux |

**Schéma `PaiementMoisDetailRowDto`** :

| Champ | Type | Description |
|---|---|---|
| `idPaiement` | `int` | PK du paiement |
| `date` | `DateTime` | Date de prestation |
| `debut` | `string` | Heure début (ex: `"09:00"`) |
| `fin` | `string` | Heure fin (ex: `"12:15"`) |
| `duree` | `int` | Durée en minutes |
| `km` | `decimal` | Distance km aller (adresse active à cette date) |
| `montant` | `decimal` | Montant prestation |
| `transport` | `decimal` | Transport |
| `idFacture` | `int?` | ID facture (null si pas encore facturé) |

**Schéma `PaiementMoisTotauxDto`** :

| Champ | Type | Description |
|---|---|---|
| `montant` | `decimal` | Total prestation |
| `transport` | `decimal` | Total transport |
| `baseHt` | `decimal` | Base HT (montant + transport) |
| `montantTva` | `decimal` | Total TVA |
| `total` | `decimal` | Total TTC |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | Format `month` invalide |
| `404 Not Found` | Aucun paiement pour cet interprète sur ce mois |

**Composants Angular appelants** : `PaiementsService` ? `PaiementsDetailComponent`

---

### 10.3 `GET /api/paiements/mois/pdf` ?? ??

**Rôle métier** : Générer un document PDF batch contenant **toutes les factures** (une par interprète) du mois. Chaque page de facture est bilingue FR/NL (selon `TAALROL`), avec en-tête fournisseur (nom, adresse, TVA, IBAN), tableau des prestations (date, heures, durée, km, montant, transport) et totaux (HT, TVA, TTC). Le PDF est généré via `QuestPDF` avec le modèle `FacturesBatchPdfDocument`.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `month` | `QS` | `string` | ? | Format `YYYY-MM` |

**Réponse succès** :

| Code | Type | Corps | Nom du fichier |
|---|---|---|---|
| `200 OK` | `application/pdf` | Fichier PDF | `Factures_{month}.pdf` |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | Format `month` invalide |
| `404 Not Found` | Aucun paiement sur ce mois |

**Logique PDF** :
1. Charge tous les paiements du mois avec prestations (heures)
2. Charge identités, adresses et les groupe par interprète
3. Pour chaque interprète : construit un `FactureModel` (record) avec en-tête, lignes et totaux
4. Génère le PDF batch via `FacturesBatchPdfDocument.GeneratePdf()`

**Composants Angular appelants** : `PaiementsService` ? `PaiementsComponent` (bouton « Télécharger PDF »)

---

### 10.4 `DELETE /api/paiements/{id}` ??

**Rôle métier** : Supprimer un paiement et ses prestations associées. Interdit si le paiement est déjà lié à une facture (`IdFacture != null`). Libère les liens `TOLKLINK` en remettant `IdPrestation = null` pour permettre un ré-encodage.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `id` | `PATH` | `int` | ? |

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Paiement + prestations supprimés, TOLKLINK libérés |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | `"Impossible de supprimer un paiement déjà facturé."` (IdFacture != null) |
| `404 Not Found` | Paiement inexistant |

**Logique** :
1. Vérifie `IdFacture == null`
2. Charge les `PRESTATION` liées via `IdPaiement`
3. Remet `TOLKLINK.IdPrestation = null` pour les prestations concernées
4. Supprime les prestations
5. Supprime le paiement
6. `SaveChangesAsync()`

**Composants Angular appelants** : `PaiementsService` ? `PaiementsDetailComponent` (bouton supprimer par ligne)

---

## 11. Factures

**Contrôleur** : `FacturesController` — **Fichier** : `FacturesController.cs`
**Route de base** : `/api/factures`
**Autorisation** : ?? Aucune

---

### 11.1 `GET /api/factures` ??

**Rôle métier** : Lister les factures avec filtres optionnels. C'est l'endpoint principal de la page « Factures » qui affiche le tableau de suivi de facturation. Les factures annulées et notes de crédit sont incluses si leur `DateGeneration` tombe dans la période (car leurs paiements originaux ont été supprimés lors de l'annulation).

**Paramètres** :

| Paramètre | Source | Type | Requis | Défaut | Description |
|---|---|---|---|---|---|
| `month` | `QS` | `string?` | ? | — | Mois de prestation au format `YYYY-MM`. Filtre par `PAIEMENT.DatePrestation` + inclut les factures annulées/NC par `DateGeneration` |
| `statut` | `QS` | `string?` | ? | — | Filtre exact sur `FACTURE.StatutFacture` |
| `tolkcode` | `QS` | `string?` | ? | — | Filtre exact sur `FACTURE.Tolkcode` |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<FactureListItemDto>` (trié par `IdFacture ASC`) |

**Schéma `FactureListItemDto`** :

| Champ | Type | Description |
|---|---|---|
| `idFacture` | `int` | PK |
| `reference` | `string` | `"RVV-CCE/{idFacture}"` — référence métier |
| `tolkcode` | `string` | Code interprète |
| `nom` | `string` | Nom (enrichi via `TOLKIDENTITY`) |
| `prenom` | `string` | Prénom |
| `dateGeneration` | `DateTime` | Date de création de la facture |
| `dateValidationFedcom` | `DateTime?` | Date de validation Fedcom (null si pas encore validée) |
| `dateTransmission` | `DateTime?` | Date de transmission (null si pas transmise) |
| `statutFacture` | `string` | Statut : `GENEREE`, `TRANSMISE`, `APPROUVEE`, `ANNULEE`, `NOTE DE CREDIT`, `CREDIT VALIDE` |
| `totalTtc` | `decimal` | Total TTC (négatif pour les notes de crédit) |
| `nbPaiements` | `int?` | Nombre de paiements liés |

**Statuts possibles** et leur signification :

| Statut | Description |
|---|---|
| `GENEREE` | Facture créée, pas encore transmise |
| `TRANSMISE` | Envoyée à l'interprète (via .eml ou autre) |
| `APPROUVEE` | Validée par Fedcom |
| `ANNULEE` | Annulée après validation (une note de crédit a été créée) |
| `NOTE DE CREDIT` | Note de crédit générée automatiquement lors de l'annulation |
| `CREDIT VALIDE` | Note de crédit validée par Fedcom |

**Composants Angular appelants** : `FacturesService` ? `FacturesComponent`, `GenerationFacturesComponent`

---

### 11.2 `POST /api/factures/generer` ??

**Rôle métier** : Générer les factures pour un mois (ou une période). Regroupe tous les paiements non encore facturés (`IdFacture IS NULL`) par interprète, crée une `FACTURE` par interprète avec `StatutFacture = "GENEREE"`, et lie les paiements. Opération transactionnelle.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| corps | `BODY` | `GenererFacturesRequestDto` | ? |

**Schéma `GenererFacturesRequestDto`** — 2 modes mutuellement exclusifs :

| Champ | Type | Mode | Description |
|---|---|---|---|
| `annee` | `int` | Mois | Année (2000-2100) |
| `mois` | `int` | Mois | Mois (1-12) |
| `dateDebut` | `string?` | Période | `YYYY-MM-DD` — date de début (inclusif) |
| `dateFin` | `string?` | Période | `YYYY-MM-DD` — date de fin (inclusif, +1 jour en interne) |

Le mode « Période » est prioritaire si `dateDebut` et `dateFin` sont renseignés.

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `{ created: int, linked: int }` |

| Champ | Description |
|---|---|
| `created` | Nombre de factures créées |
| `linked` | Nombre total de paiements liés |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | Mois/année invalide, ou `dateDebut >= dateFin` |

**Logique** :
1. Charge tous les `PAIEMENT` avec `IdFacture IS NULL` et `DatePrestation ? [d0, d1[`
2. Si aucun ? `{ created: 0, linked: 0 }`
3. Dans une transaction : groupe par `Tolkcode`, crée une `FACTURE` par groupe
4. `FACTURE.TotalTtc = SUM(Paiement.Total)`, `StatutFacture = "GENEREE"`, `DateGeneration = NOW`
5. Lie chaque paiement via `Paiement.IdFacture = Facture.IdFacture`

**Composants Angular appelants** : `FacturesService` ? `GenerationFacturesComponent`

---

### 11.3 `PATCH /api/factures/{id}/statut` ??

**Rôle métier** : Changer le statut d'une facture. Deux transitions possibles : `APPROUVEE` (validation Fedcom) et `ANNULEE` (annulation avec création automatique d'une note de crédit). L'annulation est un processus complexe en **9 étapes SQL** dans une transaction.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `id` | `PATH` | `int` | ? | ID de la facture |
| corps | `BODY` | `UpdateStatutDto` | ? | `{ statutFacture: "APPROUVEE" \| "ANNULEE" }` |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `{ idFacture, reference, statutFacture, dateValidationFedcom }` |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | Statut invalide, ou transitions interdites : annuler une NC, annuler une facture non approuvée, approuver une facture non transmise |
| `404 Not Found` | Facture inexistante |

**Règles de transition** :

| Depuis | Vers | Condition |
|---|---|---|
| `TRANSMISE` | `APPROUVEE` | ? `DateValidationFedcom = NOW` |
| `NOTE DE CREDIT` | `CREDIT VALIDE` | ? (approbation d'une NC) |
| `APPROUVEE` | `ANNULEE` | ? déclenche les 9 étapes |
| `GENEREE` | `APPROUVEE` | ? interdit (doit d'abord être transmise) |
| `NOTE DE CREDIT` | `ANNULEE` | ? interdit |
| `CREDIT VALIDE` | `ANNULEE` | ? interdit |

**Logique d'annulation (9 étapes dans une transaction)** :
1. Récupère les paiements liés à la facture
2. Récupère les prestations liées à ces paiements
3. Remet `TOLKLINK.IdPrestation = NULL` pour libérer les audiences
4. Crée une `NOTE DE CREDIT` avec `TotalTtc = -TotalTtc(original)` et `IdFactureOrigine = IdFacture`
5. Copie les paiements vers la NC (montants négatifs) via SQL brut avec `RETURNING INTO` pour récupérer les IDs
6. Copie les prestations liées (heures identiques) via SQL brut
7. Détache les entités originales du tracking EF
8. Supprime les prestations originales par SQL brut
9. Supprime les paiements originaux par SQL brut

**Composants Angular appelants** : `FacturesService` ? `FacturesComponent`

---

### 11.4 `PATCH /api/factures/{id}/transmettre` ??

**Rôle métier** : Marquer une facture comme transmise à l'interprète. Met `StatutFacture = "TRANSMISE"` et `DateTransmission = NOW`. Prérequis à la validation Fedcom.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `id` | `PATH` | `int` | ? |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `{ idFacture, reference, statutFacture, dateTransmission }` |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `404 Not Found` | Facture inexistante |

**Composants Angular appelants** : `FacturesService` ? `FacturesComponent`

---

### 11.5 `GET /api/factures/pdf` ?? ??

**Rôle métier** : Générer un document PDF batch contenant toutes les factures du mois (hors annulées), une page par interprète. Chaque facture inclut : en-tête fournisseur (nom, adresse, TVA, BBAN), en-tête client (Account Payable IBZ/IBN bilingue), tableau des prestations, totaux (HT, TVA, TTC), référence `RVV-CCE/{id}`, numéro PO et numéro d'entreprise `0308356862`. Le PDF est généré via QuestPDF.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `month` | `QS` | `string` | ? | Format `YYYY-MM` |
| `po` | `QS` | `string?` | ? | Numéro Purchase Order (affiché sur la facture si fourni) |

**Réponse succès** :

| Code | Type | Corps | Nom du fichier |
|---|---|---|---|
| `200 OK` | `application/pdf` | Fichier PDF | `Factures_{month}.pdf` |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | Format `month` invalide |
| `404 Not Found` | Aucune facture sur ce mois |

**Logique** :
1. Récupère les IDs de factures ayant des paiements sur le mois (`Paiement.DatePrestation ? [d0, d1[`)
2. Filtre `StatutFacture != "ANNULEE"`
3. Charge paiements, prestations, identités, adresses
4. Pour chaque facture : construit un `FactureModel` (record) avec référence, PO, numéro entreprise
5. Adresse du client : `Account Payable / Leuvenstraat 1 / 1000 BRUSSEL` (NL) ou `1 Rue de Louvain / 1000 BRUXELLES` (FR)
6. Génère le PDF batch via `FacturesBatchPdfDocument`

**Composants Angular appelants** : `FacturesService` ? `FacturesComponent` (bouton « Télécharger PDF »)

---

### 11.6 `GET /api/factures/{id}/eml` ?? ??

**Rôle métier** : Générer un fichier `.eml` (RFC 2822 MIME, `X-Unsent: 1`) qui ouvre Outlook en mode brouillon avec :
- **Destinataire** : email de l'interprète (`TOLKIDENTITY.EMAIL`)
- **Sujet** : `"Votre facture RVV-CCE/{id} — {month}"` (FR) ou `"Uw factuur RVV-CCE/{id} — {month}"` (NL)
- **Corps** : message de courtoisie + notice Peppol trilingue (NL/FR/EN) annonçant l'obligation e-facturation 2026
- **Pièce jointe** : PDF de la facture individuelle (base64)

Pour les notes de crédit, le sujet et le corps mentionnent la note de crédit et la référence de la facture originale.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `id` | `PATH` | `int` | ? | ID de la facture |
| `po` | `QS` | `string?` | ? | Numéro PO (inclus dans le PDF joint) |

**Réponse succès** :

| Code | Type | Corps | Nom du fichier |
|---|---|---|---|
| `200 OK` | `message/rfc822` | Fichier `.eml` | `Facture_RVV-CCE-{id}_{month}.eml` ou `NoteDeCredit_RVV-CCE-{id}_{month}.eml` |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | Tolkcode invalide ou interprète sans adresse email |
| `404 Not Found` | Facture ou interprète introuvable |

**Structure MIME** :
```
To: interprete@example.be
Subject: Votre facture RVV-CCE/123 — 2025-06
X-Unsent: 1
MIME-Version: 1.0
Content-Type: multipart/mixed; boundary="..."

--boundary
Content-Type: text/plain; charset=utf-8
[Corps du message + notice Peppol]

--boundary
Content-Type: application/pdf; name="Facture_RVV-CCE-123_2025-06.pdf"
Content-Transfer-Encoding: base64
[PDF en base64, lignes de 76 caractères]
--boundary--
```

**Composants Angular appelants** : `FacturesService` ? `FacturesComponent` (bouton « Envoyer par email »)

---

## 12. Calendar

**Contrôleur** : `CalendarController` — **Fichier** : `ValuesController.cs`
**Route de base** : `/api/calendar`
**Autorisation** : ?? Aucune
**Source de données** : Vue Oracle `VUE_CALENDAR_VRM_PC` mappée par EF Core. Ce contrôleur inclut aussi un `ValuesController` vide (pas d'endpoints).

---

### 12.1 `GET /api/calendar` ??

**Rôle métier** : Charger l'intégralité de la vue calendrier `VUE_CALENDAR_VRM_PC`. Utilisé par le composant calendrier Angular pour afficher la grille des audiences avec les interprètes assignés. **Attention** : aucun filtre de date n'est appliqué — toute la vue est chargée en une requête.

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<VueCalendarVrmPc>` (entité complète de la vue Oracle, toutes les colonnes) |

**Schéma principal** (champs les plus utilisés) :

| Champ | Type | Description |
|---|---|---|
| `idAffAudience` | `decimal?` | PK technique affaire-audience |
| `dateAudience` | `DateTime?` | Date de l'audience |
| `heureAudience` | `string?` | Heure (ex: `"09:00"`) |
| `salleAudience` | `string?` | Salle |
| `nom` | `string?` | Magistrat |
| `tolkcode` | `decimal?` | Code interprète assigné (null si pas encore assigné) |
| `langueRequete` | `string?` | Langue demandée |
| `nroRoleGen` | `decimal?` | Numéro de rôle général |
| `langueRole` | `string?` | `"F"` ou `"N"` |

**Composants Angular appelants** : `CalendarService` ? `CalendarComponent`

---

### 12.2 `GET /api/calendar/test` ??

**Rôle métier** : Endpoint de santé basique (health check). Utilisé pour vérifier que l'API est accessible depuis le proxy Apache. Ne fait aucun accès base de données.

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `{ message: "API OK" }` |

**Composants Angular appelants** : aucun (usage diagnostic uniquement)

---

### 12.3 `GET /api/calendar/headers` ??

**Rôle métier** : Endpoint de débogage réseau. Retourne la totalité des en-têtes HTTP de la requête reçue par l'API, ainsi que la valeur de l'en-tête `X-Remote-User` injectée par le proxy Apache. Utilisé pour diagnostiquer les problèmes d'authentification NTLM dans la chaîne Navigateur ? Apache ? IIS ? API.

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `{ remoteUser: string?, allHeaders: { [key: string]: string }, message: string }` |

**Exemple de réponse** :
```json
{
  "remoteUser": "INTRRDM01\\jcaufriez",
  "allHeaders": {
    "Host": "dragoman.ibz.be",
    "X-Remote-User": "INTRRDM01\\jcaufriez",
    "X-Forwarded-For": "10.0.0.5"
  },
  "message": "Test réussi depuis Apache vers IIS"
}
```

**Composants Angular appelants** : aucun (usage diagnostic uniquement)

---

## 13. Reports

**Contrôleur** : `ReportsController` — **Fichier** : `ReportsController.cs`
**Route de base** : `/api/reports`
**Autorisation** : ?? Aucune
**Source de données** : Vue Oracle `V_AUDIENCE_INTERPRETE_DETAIL` (source principale) avec fallback sur `PRESTATION` + `TOLKLINK` + vues calendrier si la vue est vide pour la date demandée. Enrichissement avec les noms de magistrats depuis `VUE_CALENDAR_VRM_PC`.
**Bibliothèques** : ClosedXML (Excel), OpenXml SDK (Word), QuestPDF (PDF).

Tous les endpoints partagent la même méthode `GetData(DateOnly)` qui construit la structure `InterpretePresenceDto` — la différence est uniquement le format de sortie.

---

### 13.1 `GET /api/reports/interpretes` ??

**Rôle métier** : Récupérer les données de présence des interprètes pour une date donnée, au format JSON. Utilisé pour l'aperçu écran avant export. Les données sont regroupées par interprète, avec pour chacun : ses audiences (heure, salle, langue, magistrat, nombre d'affaires), ses téléphones et sa langue administrative.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `date` | `QS` | `DateOnly` | ? | Date du jour à consulter |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<InterpretePresenceDto>` |

**Schéma `InterpretePresenceDto`** :

| Champ | Type | Description |
|---|---|---|
| `tolkcode` | `int?` | Identifiant |
| `nom` | `string` | Nom |
| `prenom` | `string` | Prénom |
| `telephones` | `string[]` | Liste GSM/Tel/Telbis (non vides) |
| `frNl` | `string?` | `"FR"`, `"NL"` ou `""` (dérivé de `TAALROL`) |
| `nbAffaires` | `int` | Total des affaires toutes audiences confondues |
| `audiences` | `Array<InterpreteAudienceDto>` | Audiences consolidées |

**Schéma `InterpreteAudienceDto`** :

| Champ | Type | Description |
|---|---|---|
| `heure` | `string?` | Heure de l'audience |
| `salle` | `string?` | Salle |
| `langue` | `string?` | Langues demandées (jointes par `, ` si multiples) |
| `magistrat` | `string?` | Nom(s) du/des magistrat(s) |
| `nbAffaires` | `int` | Nombre d'affaires pour cette audience |

**Logique `GetData`** :
1. Source principale : `V_AUDIENCE_INTERPRETE_DETAIL` filtrée sur la date
2. Enrichissement magistrats via `VUE_CALENDAR_VRM_PC` (même date, même tolkcode)
3. Consolidation par interprète : dédoublonnage audiences par `(Heure, Salle)`, comptage `NbAffaires`
4. Fallback (si vue vide) : charge les `PRESTATION` du jour, résout les audiences via `TOLKLINK` ? vues calendrier
5. Tri final : heure ? salle ? nom ? prénom

**Composants Angular appelants** : `ReportsService` ? `ReportsComponent`

---

### 13.2 `GET /api/reports/interpretes/excel` ?? ??

**Rôle métier** : Exporter la fiche de présence journalière au format Excel (.xlsx). Le fichier contient un titre centré, un tableau avec colonnes Présent (vide, à cocher manuellement), Heure, Salle, Interprète (#code, * si multi-salle), Téléphone, Langue, Aff., FR/NL, Remarque. Les interprètes sans audience sont listés à la fin.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `date` | `QS` | `DateOnly` | ? |

**Réponse succès** :

| Code | Type | Corps | Nom du fichier |
|---|---|---|---|
| `200 OK` | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` | Fichier Excel | `Presence_Interpretes_{date}.xlsx` |

**Bibliothèque** : ClosedXML

**Composants Angular appelants** : `ReportsService` ? `ReportsComponent` (bouton « Excel »)

---

### 13.3 `GET /api/reports/interpretes/word` ?? ??

**Rôle métier** : Exporter la fiche de présence au format Word (.docx) en **paysage**. Le document contient :
1. **1 page par audience** (regroupée par Heure + Salle) : titre, sous-titre magistrat, tableau des interprètes avec colonnes Présent, #, Interprète, Téléphone, Langue, Aud., FR/NL, Remarque
2. **1 page synthèse** : tableau global de toutes les lignes interprète × audience, trié par heure/salle/nom

Les sauts de page sont gérés par des `SectionProperties` OpenXml.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `date` | `QS` | `DateOnly` | ? |

**Réponse succès** :

| Code | Type | Corps | Nom du fichier |
|---|---|---|---|
| `200 OK` | `application/vnd.openxmlformats-officedocument.wordprocessingml.document` | Fichier Word | `Presence_Interpretes_{date}.docx` |

**Bibliothèque** : DocumentFormat.OpenXml (OpenXml SDK)

**Composants Angular appelants** : `ReportsService` ? `ReportsComponent` (bouton « Word »)

---

### 13.4 `GET /api/reports/interpretes/pdf` ?? ??

**Rôle métier** : Exporter la fiche de présence au format PDF A4 paysage. Le document contient un tableau unique avec les mêmes colonnes que l'Excel (Présent ?, Heure, Salle, Interprète, Téléphone, Langue, Aff., FR/NL, Remarque). Les interprètes multi-salles sont marqués d'un astérisque. Note de bas de page : `* Interprète présent dans plusieurs salles.`

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `date` | `QS` | `DateOnly` | ? |

**Réponse succès** :

| Code | Type | Corps | Nom du fichier |
|---|---|---|---|
| `200 OK` | `application/pdf` | Fichier PDF | `Presence_Interpretes_{date}.pdf` |

**Bibliothèque** : QuestPDF (licence Community)

**Composants Angular appelants** : `ReportsService` ? `ReportsComponent` (bouton « PDF »)

---

## 14. User

**Contrôleur** : `UserController` — **Fichier** : `UserController.cs`
**Route de base** : `/api/user`
**Autorisation** : ?? Aucune

---

### 14.1 `GET /api/user/current` ??

**Rôle métier** : Récupérer le nom d'utilisateur Windows courant. Lit l'en-tête `X-Remote-User` injectée par le proxy Apache (qui extrait l'identité NTLM). Utilisé par l'application Angular pour afficher le nom de l'utilisateur connecté et pour marquer les actions d'audit.

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `{ username: string }` |

**Logique** : `Request.Headers["X-Remote-User"]` ? si vide, fallback sur `"anonymous"`.

**Composants Angular appelants** : `AuthentificationService` ? `NavbarComponent`

**Exemple de réponse** :
```json
{ "username": "INTRRDM01\\jcaufriez" }
```

---

### 14.2 `POST /api/user/addUser` ??

**Rôle métier** : ?? **Code inachevé / dead code**. Tentative d'ajout d'un utilisateur dans une table `Test`. Récupère `Environment.UserName`, génère un ID incrémental (toujours `1` car `maxId` est hardcodé à `0`), crée une entité `Test` mais **ne l'ajoute jamais au `DbContext`** (`_context.Tests.Add(...)` est absent). L'appel `SaveChangesAsync()` est un no-op.

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `{ message: "Utilisateur {name} ajouté avec l'ID {id}." }` |

**?? Note** : cet endpoint ne persiste rien en base. Il retourne toujours `200 OK` avec un message trompeur. À ne pas utiliser en production.

**Composants Angular appelants** : aucun

---

## 15. Helpdesk Prestations

**Contrôleur** : `HdPrestationsController` — **Fichier** : `HdPrestationsController.cs`
**Route de base** : `/api/hd-prestations`
**Autorisation** : ?? Aucune
**Stockage** : Fichiers JSON sur disque local — `{AppRoot}/data/hd-prestations/{user}/{semaineISO}/{user}_{semaineISO}_{date}.json`. Pas de base de données Oracle. Les noms de fichiers sont sanitisés via `Path.GetInvalidFileNameChars()`.

Ce module est un outil interne de suivi des prestations de l'équipe helpdesk IT (tickets, autres tâches, régime de travail, garde). Totalement indépendant du système interprètes.

---

### 15.1 `POST /api/hd-prestations/jour` ??

**Rôle métier** : Enregistrer la fiche de prestation helpdesk pour un jour donné. Crée/écrase le fichier JSON correspondant sur disque.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| corps | `BODY` | `HDPrestationJourDto` | ? |

**Schéma `HDPrestationJourDto`** :

| Champ | Type | Requis | Description |
|---|---|---|---|
| `hdUser` | `string` | ? | Identifiant utilisateur (ex: `"INTRRDM01\\jcaufriez"`) |
| `hdDate` | `string` | ? | Date au format `YYYY-MM-DD` |
| `hdSemaineISO` | `string` | ? | Semaine ISO (ex: `"2025-W29"`) |
| `hdRegimeTravail` | `string?` | ? | Régime de travail (ex: `"Bureau"`, `"Télétravail"`) |
| `hdGarde` | `string?` | ? | Garde (ex: `"oui — standby"`, `"non"`) |
| `hdTickets` | `Array<HDTicketDto>` | ? | Liste des tickets du jour |
| `hdAutresTaches` | `Array<HDAutreTacheDto>` | ? | Liste des autres tâches |
| `hdRemarquesCollaborateur` | `string?` | ? | Remarques libres |

**Schéma `HDTicketDto`** : `{ date?, heure?, numero?, type?, dureeMin? }`

**Schéma `HDAutreTacheDto`** : `{ denomination?, date?, dureeMin? }`

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `{ ok: true, chemin: string }` (chemin relatif du fichier créé) |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | `hdUser`, `hdDate` ou `hdSemaineISO` manquant |

**Composants Angular appelants** : `HdPrestationsService` ? `HdPrestationsComponent`

---

### 15.2 `GET /api/hd-prestations/jour` ??

**Rôle métier** : Lire la fiche de prestation helpdesk d'un jour donné. Recherche dans le dossier de l'utilisateur avec fallback : d'abord par chemin direct (`{user}/{semaineISO}/{user}_{semaineISO}_{date}.json`), puis par nom `DOMAIN\user` ? `user` seulement, puis par glob `*_{date}.json` en profondeur.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `hdUser` | `QS` | `string` | ? | Identifiant utilisateur |
| `hdDate` | `QS` | `string` | ? | Date `YYYY-MM-DD` |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `HDPrestationJourDto` ou `null` (si pas de fichier trouvé) |

**Composants Angular appelants** : `HdPrestationsService` ? `HdPrestationsComponent`

---

### 15.3 `GET /api/hd-prestations/semaine` ??

**Rôle métier** : Lire toutes les fiches d'une semaine ISO pour un utilisateur. Charge tous les fichiers JSON du dossier `{user}/{semaineISO}/`, avec fallback par dates de la semaine si le dossier n'existe pas sous ce nom.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `hdUser` | `QS` | `string` | ? | Identifiant utilisateur |
| `hdSemaineISO` | `QS` | `string` | ? | Semaine ISO (ex: `"2025-W29"`) |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<HDPrestationJourDto>` (trié par `hdDate ASC`, dédupliqué par date) |

**Composants Angular appelants** : `HdPrestationsService` ? `HdPrestationsComponent`

---

### 15.4 `PUT /api/hd-prestations/semaine` ??

**Rôle métier** : Enregistrer tous les jours d'une semaine en une seule requête. Chaque jour est validé individuellement puis sauvé comme fichier JSON séparé.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| corps | `BODY` | `HDPrestationSemaineDto` | ? |

**Schéma `HDPrestationSemaineDto`** :

| Champ | Type | Requis | Description |
|---|---|---|---|
| `hdUser` | `string` | ? | Identifiant utilisateur |
| `hdSemaineISO` | `string` | ? | Semaine ISO |
| `jours` | `Array<HDPrestationJourDto>` | ? | Les 5 à 7 jours à sauvegarder |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `{ ok: true, chemin: string }` |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | `hdUser` ou `hdSemaineISO` manquant, ou un jour invalide (détail dans `message`) |

**Composants Angular appelants** : `HdPrestationsService` ? `HdPrestationsComponent`

---

### 15.5 `GET /api/hd-prestations/semaine/export/word` ?? ??

**Rôle métier** : Exporter la fiche hebdomadaire helpdesk au format Word (.docx). Le document contient :
1. **Résumé semaine** : tableau avec régime de travail (dominant ou `"mixte"`), garde (oui/non avec détails), nombre de tickets, minutes tickets, nombre autres tâches, minutes autres, total général
2. **Détail par jour** : pour chaque jour ayant du contenu, un tableau tickets (Heure, N°, Type, Durée) et un tableau autres tâches (Dénomination, Durée), plus les remarques
3. **Zone de signature** : tableau avec cases « Validé par le responsable » et « Signature du collaborateur »

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| `hdUser` | `QS` | `string` | ? |
| `hdSemaineISO` | `QS` | `string` | ? |

**Réponse succès** :

| Code | Type | Corps | Nom du fichier |
|---|---|---|---|
| `200 OK` | `application/vnd.openxmlformats-officedocument.wordprocessingml.document` | Fichier Word | `FicheHelpdesk_{user}_{semaineISO}.docx` |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `404 Not Found` | Aucune donnée trouvée pour cet utilisateur et cette semaine |

**Bibliothèque** : DocumentFormat.OpenXml (OpenXml SDK)

**Composants Angular appelants** : `HdPrestationsService` ? `HdPrestationsComponent` (bouton « Export Word »)

---

## 16. AD Status

**Contrôleur** : `AdStatusController` — **Fichier** : `AdStatusController.cs`
**Route de base** : `/api/adstatus`
**Autorisation** : ?? `[Authorize(Roles = @"INTRRDM01\gg_rol_SystemAdministrator")]` au niveau classe — **tous les endpoints sont protégés**
**Source de données** :
- **CSV** : fichier `D:\Dragoman\Data\AD_Users.csv` généré par un script PowerShell (export AD), séparateur `;`, encodage UTF-8 (avec BOM potentiel)
- **JSON de persistance** : `adstatus_persistence.json` (dans le même dossier) — stocke les commentaires et le flag `IsNormal` par `SamAccountName`

Le CSV est lu en lecture partagée (`FileShare.ReadWrite`) pour permettre sa mise à jour pendant que l'API tourne.

---

### 16.1 `GET /api/adstatus` ??

**Rôle métier** : Charger le tableau de bord Active Directory pour les administrateurs système. Parse le CSV PowerShell ligne par ligne, calcule les statuts de mot de passe et d'inactivité, puis fusionne avec les données de persistance JSON (commentaires, flag `IsNormal`).

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<AdUserStatusDto>` |

**Schéma `AdUserStatusDto`** :

| Champ | Type | Description |
|---|---|---|
| `samAccountName` | `string` | Identifiant AD |
| `displayName` | `string` | Nom complet |
| `ou` | `string` | Unité organisationnelle |
| `passwordLastSet` | `string?` | Date dernier changement (format `dd-MM-yyyy HH:mm:ss`) |
| `passwordExpiresOn` | `string?` | Date d'expiration du mot de passe |
| `passwordStatus` | `string` | `"OK"`, `"Expired"`, `"NeverExpires"`, `"Expires15d"`, `"Expires7d"`, `"Expires24h"` |
| `lastLogonDate` | `string?` | Dernière connexion |
| `inactivityStatus` | `string` | `"Active"`, `"InactiveSoon"`, `"Inactive90Plus"` |
| `daysUntilExpiration` | `int?` | Jours restants avant expiration du mot de passe (peut être négatif) |
| `isNormal` | `bool` | Flag "situation normale" (issu de la persistance JSON) |
| `comment` | `string?` | Commentaire libre (issu de la persistance JSON) |

**Parsing des dates** : multi-format (`dd-MM-yy HH:mm:ss`, `dd-MM-yyyy HH:mm:ss`, `yyyy-MM-dd HH:mm:ss`, ISO), culture `fr-BE`.

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | Colonne `SamAccountName` introuvable (CSV invalide) |
| `404 Not Found` | Fichier CSV introuvable sur le disque |

**Composants Angular appelants** : `AdStatusService` ? `AdStatusComponent`

---

### 16.2 `POST /api/adstatus/comment` ??

**Rôle métier** : Sauvegarder un commentaire libre pour un utilisateur AD. Le commentaire est persisté dans le fichier JSON local et sera fusionné lors du prochain `GET`.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| corps | `BODY` | `AdUserCommentDto` | ? |

**Schéma `AdUserCommentDto`** :

| Champ | Type | Requis | Description |
|---|---|---|---|
| `samAccountName` | `string` | ? | Identifiant AD de l'utilisateur |
| `comment` | `string` | ? | Texte du commentaire |

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Commentaire sauvegardé |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | `samAccountName` vide |

**Composants Angular appelants** : `AdStatusService` ? `AdStatusComponent`

---

### 16.3 `POST /api/adstatus/normalstatus` ??

**Rôle métier** : Marquer ou démarquer un utilisateur AD comme étant en « situation normale » (ex: un compte inactif qui est normal car l'employé est en congé longue durée). Ce flag permet de filtrer les vrais problèmes dans le tableau de bord.

**Paramètres** :

| Paramètre | Source | Type | Requis |
|---|---|---|---|
| corps | `BODY` | `AdUserNormalStatusDto` | ? |

**Schéma `AdUserNormalStatusDto`** :

| Champ | Type | Requis | Description |
|---|---|---|---|
| `samAccountName` | `string` | ? | Identifiant AD |
| `isNormal` | `bool` | ? | `true` = situation normale, `false` = à investiguer |

**Réponse succès** :

| Code | Description |
|---|---|
| `204 No Content` | Statut sauvegardé |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | `samAccountName` vide |

**Composants Angular appelants** : `AdStatusService` ? `AdStatusComponent`

---

## 17. Inventory

**Contrôleur** : `InventoryController` — **Fichier** : `InventoryController.cs`
**Route de base** : `/api/inventory`
**Autorisation** : ?? Aucune
**Stockage** : Fichier JSON local `{AppRoot}/Data/GlobalProtectInventory.json` (état maître). Le CSV importé est aussi sauvé comme `LastUpload.csv` pour référence.

Ce module gère l'inventaire des machines du réseau et leur statut GlobalProtect (VPN). Indépendant du système interprètes.

---

### 17.1 `GET /api/inventory` ??

**Rôle métier** : Charger la liste complète des machines avec leur dernier état connu (IP, localisation, version GlobalProtect, statut en ligne). Le tri met en avant les machines au bureau et en ligne, puis celles avec une version GP, puis par version numérique décroissante, puis par nom.

**Paramètres** : aucun

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<MachineRecord>` (trié par pertinence) |

**Schéma `MachineRecord`** :

| Champ | Type | Description |
|---|---|---|
| `computerName` | `string` | Nom de la machine (PK logique) |
| `description` | `string?` | Description AD |
| `dnsHostName` | `string?` | Nom DNS |
| `operatingSystem` | `string?` | OS (ex: `"Windows 11 Enterprise"`) |
| `lastIPAddress` | `string?` | Dernière IP connue |
| `lastLocalisation` | `string?` | `"Bureau"`, `"Bureau (autre site)"`, `"Domicile"`, etc. |
| `globalProtectVersion` | `string?` | Version GP (ex: `"6.3.3"`) |
| `globalProtectStatus` | `string?` | Statut GP |
| `lastEnLigne` | `bool` | Machine en ligne lors du dernier scan |
| `lastScanDateUtc` | `DateTime` | Date/heure UTC du dernier scan |
| `verifiedByTeam` | `bool` | Flag vérifié par l'équipe (annotation manuelle) |
| `remark` | `string?` | Remarque libre (annotation manuelle) |

**Règle de tri** :
1. `LastEnLigne == true && Localisation ? {"Bureau", "Bureau (autre site)"}` en premier
2. Machines ayant une `GlobalProtectVersion` non vide
3. `GlobalProtectVersion` décroissante (parsing `Version`) 
4. `ComputerName` alphabétique

**Composants Angular appelants** : `InventoryService` ? `InventoryComponent`

---

### 17.2 `POST /api/inventory/import` ??

**Rôle métier** : Importer un CSV PowerShell contenant l'état courant des machines. Les données sont **fusionnées** avec le JSON maître existant : les nouvelles machines sont ajoutées, les machines existantes voient leurs champs techniques mis à jour, mais les annotations manuelles (`VerifiedByTeam`, `Remark`) sont **préservées**.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `file` | `form-data` | `IFormFile` | ? | Fichier CSV (séparateur `;` ou `,`, auto-détecté) |

**Colonnes CSV attendues** : `ComputerName`, `DNSHostName`, `Description`, `OperatingSystem`, `IPAddress`, `Localisation`, `GlobalProtectVersion`, `GlobalProtectStatus`, `EnLigne`.

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `Array<MachineRecord>` (état fusionné complet, trié) |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `400 Bad Request` | Fichier manquant ou vide |

**Logique de merge** :
1. Charge le JSON maître en dictionnaire par `ComputerName` (case-insensitive)
2. Pour chaque ligne CSV : si la machine existe ? met à jour les champs techniques ; sinon ? crée
3. `LastScanDateUtc = DateTime.UtcNow` pour chaque ligne traitée
4. Sauvegarde le JSON maître mis à jour

**Composants Angular appelants** : `InventoryService` ? `InventoryComponent` (bouton « Importer CSV »)

---

### 17.3 `PUT /api/inventory/{computerName}` ??

**Rôle métier** : Mettre à jour les annotations manuelles (`VerifiedByTeam` et `Remark`) pour une machine. Utilisé quand un membre de l'équipe vérifie physiquement une machine et note ses observations.

**Paramètres** :

| Paramètre | Source | Type | Requis | Description |
|---|---|---|---|---|
| `computerName` | `PATH` | `string` | ? | Nom de la machine (comparaison case-insensitive) |
| corps | `BODY` | `MachineUpdateDto` | ? | Annotations |

**Schéma `MachineUpdateDto`** :

| Champ | Type | Requis | Description |
|---|---|---|---|
| `verifiedByTeam` | `bool` | ? | `true` = vérifié |
| `remark` | `string?` | ? | Remarque libre |

**Réponse succès** :

| Code | Type | Corps |
|---|---|---|
| `200 OK` | `application/json` | `MachineRecord` (entité mise à jour) |

**Réponse erreur** :

| Code | Condition |
|---|---|
| `404 Not Found` | Machine inconnue dans l'inventaire |

**Composants Angular appelants** : `InventoryService` ? `InventoryComponent`

---

## 18. WeatherForecast

**Contrôleur** : `WeatherForecastController` — **Fichier** : `WeatherForecastController.cs`
**Route de base** : `/api/weatherforecast`
**Autorisation** : ?? Aucune

---

### 18.1 `GET /api/weatherforecast` ??

**Rôle métier** : ?? **Code commenté / désactivé**. Il s'agit du contrôleur scaffolding par défaut généré par le template ASP.NET Core. L'intégralité du code est commentée dans le fichier source (`/* ... */`). L'endpoint n'est **pas accessible** en production.

**Paramètres** : aucun

**Réponse** : N/A — le code est commenté, l'endpoint ne répond pas.

**Note** : ce fichier peut être supprimé sans impact. Il est conservé uniquement comme artefact du scaffolding initial du projet.

---

## Annexe — Index par méthode HTTP

| Méthode | Nombre | Endpoints |
|---|---|---|
| `GET` | ~43 | Auth (1), Dashboard (6), Interprètes (6), Adresses (2), Langues (3), TVA (2), Indispo (1), Prestations (1), Paiements (3), Factures (3), Calendar (3), Reports (4), User (1), HD (3), AD (1), Inventory (1), Weather (1) |
| `POST` | ~16 | Interprètes (1), Tolklink (2), Adresses (2), Langues (2), TVA (1), Indispo (1), Prestations (3), Factures (1), User (1), HD (1), AD (2) |
| `PUT` | ~4 | Interprètes (1), Adresses (1), HD (1), Inventory (1) |
| `PATCH` | ~2 | Factures (2) |
| `DELETE` | ~7 | Interprètes (1), Tolklink (1), Adresses (1), Langues (2), Indispo (1), Paiements (1) |
| **Total** | **~72** | |

---

## Annexe — Index par domaine fonctionnel

| Domaine | Endpoints | Contrôleur(s) |
|---|---|---|
| Authentification | 1 | Auth |
| Tableau de bord | 6 | Dashboard |
| Gestion des interprètes | 10 | Interpretes |
| Assignation audiences | 3 | Tolklink |
| Adresses | 6 | Adresses |
| Langues | 7 | Langues |
| TVA | 3 | Tva |
| Indisponibilités | 3 | Indispo |
| Prestations | 4 | Prestations |
| Paiements | 4 | Paiements |
| Facturation | 6 | Factures |
| Calendrier | 3 | Calendar |
| Rapports de présence | 4 | Reports |
| Utilisateur | 2 | User |
| Helpdesk IT | 5 | HdPrestations |
| Active Directory | 3 | AdStatus |
| Inventaire machines | 3 | Inventory |
| Scaffolding | 1 | WeatherForecast |
| **Total** | **~72** | |

---

*Document complet — tous les 72 endpoints sont documentés en détail. Dernière mise à jour : juillet 2025.*
