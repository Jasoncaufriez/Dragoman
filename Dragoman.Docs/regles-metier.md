# Dragoman — Règles métier implémentées dans le code

---

## 1. Calcul du paiement — `CalculerEtMettreAJourPaiementAsync()`

Emplacement : `PrestationsController.cs`, méthode privée `CalculerEtMettreAJourPaiementAsync`.

Appelée automatiquement à chaque création de prestation (`POST /api/prestations`), à la fin de la transaction.

### 1.1 Pipeline de calcul

```
Entrées :
  - Prestation (Startheure, Endheure, DatePrestation, Tolkcode)
  - Table INDEXATION (barème actif à la date)
  - Table TOLKADRESSE (km de l'adresse active à la date)
  - Table TOLK_TVA (statut TVA actif à la date)

Sorties (écrites dans PAIEMENT) :
  - Montant    ? prestation HT
  - Transport  ? frais de déplacement HT
  - MontantTva ? TVA sur (Montant + Transport)
  - Total      ? Montant + Transport + MontantTva
```

### 1.2 Étape 1 — Résolution du barème d'indexation

```csharp
var idx = allIndexations
    .Where(i => prestation.DatePrestation >= i.Startdate
             && (!i.Enddate.HasValue || prestation.DatePrestation < i.Enddate.Value))
    .FirstOrDefault();
```

- Charge **toutes** les lignes d'INDEXATION en mémoire (`ToListAsync`)
- Filtre côté C# : `DatePrestation ? [Startdate, Enddate[`
- Si `Enddate == null` ? barème actif courant
- **Si aucun barème trouvé** ? `throw InvalidOperationException` (500 côté client)

Variables extraites :
```
euro75    = idx.Euro75min   (ex: 31.52 €)
euroHeure = idx.Euroheure   (ex: 25.21 €)
euroKm    = idx.Eurokm      (ex: 0.4170 €)
```

### 1.3 Étape 2 — Calcul de la durée

```csharp
var rawMinutes = (decimal)(prestation.Endheure - prestation.Startheure).TotalMinutes;
if (rawMinutes < 0) rawMinutes = 0;

// Arrondi au quart d'heure supérieur
var minutes = Math.Ceiling(rawMinutes / 15m) * 15m;
```

| Durée brute | Arrondi |
|---|---|
| 47 min | 60 min |
| 60 min | 60 min |
| 61 min | 75 min |
| 76 min | 90 min |
| 120 min | 120 min |

### 1.4 Étape 3 — Calcul du montant prestation

```csharp
decimal montant;
if (minutes <= 75m)
{
    montant = euro75;                        // forfait 75 min
}
else
{
    var surplus = minutes - 75m;
    montant = euro75 + surplus * (euroHeure / 60m);  // forfait + surplus au prorata minute
}
```

**Règle du minimum 75 minutes** :
- Toute prestation ? 75 min ? montant fixe = `EURO75MIN`
- Au-delà ? forfait 75 min + surplus facturé à `EUROHEURE / 60` par minute

**Exemples avec EURO75MIN=31.52, EUROHEURE=25.21** :

| Durée arrondie | Calcul | Montant |
|---|---|---|
| 15 min | 31.52 | 31.52 € |
| 75 min | 31.52 | 31.52 € |
| 90 min | 31.52 + 15 × (25.21/60) | 37.83 € |
| 120 min | 31.52 + 45 × (25.21/60) | 50.41 € |

### 1.5 Étape 4 — Montant arrondi

```csharp
paiement.Montant = Math.Round(montant, 2);
```

Arrondi bancaire à 2 décimales, appliqué uniquement à la fin (pas d'arrondi intermédiaire).

---

## 2. Règles de transport

### 2.1 Calcul des kilomètres

```csharp
// Adresse active (km) à la date de prestation
var adr = await _db.Tolkadresses.AsNoTracking()
    .Where(a => a.Tolkcode == tolkcodeInt.ToString()
                && a.Startdate <= date
                && (a.Enddate == null || date < a.Enddate))
    .OrderByDescending(a => a.Startdate)
    .FirstOrDefaultAsync(ct);

var km = (decimal)(adr?.Km ?? 0);
var kmAR = Math.Min(100m, 2m * km);
```

**Formule** :
```
KM aller-retour = min(100, 2 × KM_adresse)
```

| KM saisi | KM A/R calculé | Plafond appliqué |
|---|---|---|
| 20 | 40 | Non |
| 45 | 90 | Non |
| 50 | 100 | Oui (=plafond) |
| 80 | 100 | Oui (plafond 100) |

**Adresse sélectionnée** : celle dont `Startdate ? date < Enddate` (ou `Enddate == null`), triée par `Startdate DESC` ? la plus récente valide. Si aucune adresse ? KM = 0 ? transport = 0.

### 2.2 Montant transport

```csharp
var transport = dejaTransportJour ? 0m : euroKm * kmAR;
```

```
Transport = EUROKM × KM_aller_retour
```

### 2.3 Règle "une seule fois par jour"

```csharp
var prestationsJour = await _db.Prestations
    .Where(pr => pr.Tolkcode == prestation.Tolkcode
                 && pr.DatePrestation == date
                 && pr.IdPrestation != prestation.IdPrestation)
    .Select(pr => pr.IdPaiement)
    .ToListAsync(ct);

var dejaTransportJour = false;
if (prestationsJour.Any())
{
    var paiementsJour = await _db.Paiements
        .Where(pa => prestationsJour.Contains(pa.IdPaiement))
        .ToListAsync(ct);

    dejaTransportJour = paiementsJour.Any(pa => pa.Transport > 0);
}
```

**Règle** : si un paiement avec `Transport > 0` existe déjà pour le même interprète le même jour ? transport = 0 pour la prestation courante.

**?? Limite** : la vérification est faite au moment de la création. Si deux prestations sont créées simultanément, les deux pourraient obtenir le transport (pas de verrouillage optimiste).

---

## 3. Gestion TVA

### 3.1 Résolution du statut TVA

```csharp
var tvaStatut = await _db.TolkTvas.AsNoTracking()
    .Where(t => t.Tolkcode == tolkcodeInt
                && t.StartDate <= date
                && (t.EndDate == null || date < t.EndDate))
    .OrderByDescending(t => t.StartDate)
    .FirstOrDefaultAsync(ct);

var assujetti = (tvaStatut?.IdStatut ?? 0) == 1;
```

**Règle** : l'interprète est assujetti à la TVA si et seulement si son statut TVA actif à la date de prestation a `IdStatut == 1`.

- `IdStatut == 1` ? Assujetti ? TVA 21%
- `IdStatut != 1` (ex: 2 = Exonéré) ? Non assujetti ? TVA 0%
- Aucun enregistrement TOLK_TVA ? Non assujetti (défaut 0)

### 3.2 Calcul de la TVA

```csharp
private const decimal TVA_RATE = 0.21m;

var baseHT = montant + transport;
var tva = assujetti ? Math.Round(baseHT * TVA_RATE, 2) : 0m;
var total = baseHT + tva;
```

**Formule** :
```
Si assujetti :
    TVA = arrondi2( (Montant + Transport) × 0.21 )
    Total = Montant + Transport + TVA

Si non assujetti :
    TVA = 0
    Total = Montant + Transport
```

**Constantes codées en dur** :
- `TVA_RATE = 0.21m` — pas configurable, défini comme `const` dans `PrestationsController`
- `IdStatut == 1` — valeur magique pour "assujetti"

### 3.3 Historisation du statut TVA

La table `TOLK_TVA` est historisée avec des périodes `[StartDate, EndDate[`. Lors de l'ajout d'un nouveau statut via `POST /api/interpretes/{tolkcode}/tva`, le contrôleur clôture automatiquement l'ancien statut actif en renseignant `EndDate = nouveau.StartDate - 1 jour`.

---

## 4. Gestion de l'indexation

### 4.1 Table INDEXATION

```
INDEXATION (ID_INDEX, STARTDATE, ENDDATE, EURO75MIN, EUROHEURE, EUROKM)
```

| Champ | Signification |
|---|---|
| `EURO75MIN` | Montant forfaitaire pour toute prestation ? 75 minutes |
| `EUROHEURE` | Tarif horaire au-delà des 75 minutes (par heure, proratisé à la minute) |
| `EUROKM` | Tarif par kilomètre de déplacement |
| `STARTDATE` | Début de validité du barème |
| `ENDDATE` | Fin de validité (`null` = barème courant) |

### 4.2 Sélection du barème

Le barème applicable est celui valide à la **date de prestation** (`PRESTATION.DATE_PRESTATION`), pas à la date de saisie.

```
DatePrestation ? [STARTDATE, ENDDATE[
```

### 4.3 Utilisation en lecture seule

La table `INDEXATION` n'est **jamais modifiée** par l'application. Les barèmes sont gérés directement en base par un administrateur. L'API n'expose aucun endpoint pour les modifier.

### 4.4 Chargement complet en mémoire

```csharp
var allIndexations = await _db.Indexations.ToListAsync(ct);
```

**Toutes** les lignes d'indexation sont chargées en mémoire à chaque calcul de prestation, puis filtrées côté C#. Pas de filtre SQL.

---

## 5. Workflow facture

### 5.1 Diagramme d'états

```
  ????????????????
  ?              ?
  ?   (paiements ?
  ?  non facturés)?
  ?              ?
  ????????????????
         ? POST /api/factures/generer
         ?
  ????????????????
  ?   GENEREE    ?
  ????????????????
         ? PATCH /{id}/statut  { "APPROUVEE" }
         ?
  ????????????????   PATCH /{id}/transmettre
  ?  APPROUVEE   ????????????????????????????  (DateTransmission renseignée)
  ????????????????
         ? PATCH /{id}/statut  { "ANNULEE" }
         ?
  ????????????????       Création automatique
  ?   ANNULEE    ? ???????????????????????????  ??????????????????
  ????????????????                               ? NOTE DE CREDIT ?
                                                  ??????????????????
                                                          ? PATCH /{id}/statut { "APPROUVEE" }
                                                          ?
                                                  ??????????????????
                                                  ? CREDIT VALIDE  ?
                                                  ??????????????????
```

### 5.2 Génération — `POST /api/factures/generer`

**Entrée** : mois (`annee` + `mois`) OU période libre (`dateDebut` + `dateFin`).

**Algorithme** :
1. Chercher tous les paiements où `IdFacture == null` ET `DatePrestation ? [d0, d1[`
2. Grouper par `Tolkcode`
3. Pour chaque interprète :
   - Créer une `FACTURE` avec `StatutFacture = "GENEREE"` et `TotalTtc = ?(Total)`
   - Affecter `IdFacture` de la facture créée à chaque paiement du groupe
4. Retourner `{ created, linked }`

**TotalTtc** = somme des `Paiement.Total` (TTC, incluant la TVA si applicable).

**Contrainte** : un paiement déjà facturé (`IdFacture != null`) est ignoré — pas de double facturation.

### 5.3 Approbation — `PATCH /api/factures/{id}/statut` ? `APPROUVEE`

```csharp
facture.StatutFacture = "APPROUVEE";
facture.DateValidationFedcom = DateTime.Now;
```

Cas particulier — approbation d'une note de crédit :
```csharp
if (statut == "APPROUVEE" && facture.StatutFacture == "NOTE DE CREDIT")
{
    facture.StatutFacture = "CREDIT VALIDE";
    facture.DateValidationFedcom = DateTime.Now;
}
```

### 5.4 Transmission — `PATCH /api/factures/{id}/transmettre`

Enregistre `DateTransmission = DateTime.Now` sur la facture. Aucun changement de statut.

### 5.5 Transmission email — `GET /api/factures/{id}/eml`

Génère un fichier `.eml` (RFC 2822) contenant :
- **To** : email de l'interprète (`TOLKIDENTITY.EMAIL`)
- **Subject** : bilingue FR/NL selon `TAALROL` (ex: `"Votre facture RVV-CCE/42 — 2025-06"`)
- **Body** : texte bilingue
- **Pièce jointe** : PDF de la facture en base64
- **`X-Unsent: 1`** : indique à Outlook d'ouvrir le fichier comme brouillon (non envoyé)

Le fichier est téléchargé par l'agent qui l'ouvre dans Outlook et l'envoie manuellement.

---

## 6. Annulation et note de crédit

### 6.1 Pré-conditions

```csharp
// Empêcher d'annuler une note de crédit
if (statut == "ANNULEE" && (facture.StatutFacture == "NOTE DE CREDIT" || facture.StatutFacture == "CREDIT VALIDE"))
    return BadRequest("Impossible d'annuler une note de crédit.");

// On ne peut annuler que si la facture a été validée par Fedcom
if (statut == "ANNULEE" && facture.StatutFacture != "APPROUVEE")
    return BadRequest("Impossible d'annuler une facture qui n'a pas été validée par Fedcom.");
```

**Seules les factures `APPROUVEE` peuvent être annulées.** Les factures `GENEREE`, les notes de crédit et les crédits validés ne peuvent pas être annulés.

### 6.2 Algorithme d'annulation (9 étapes, dans une transaction)

```
Étape 1 : Récupérer les paiements liés à la facture
Étape 2 : Récupérer les prestations liées à ces paiements
Étape 3 : Libérer les TOLKLINK (IdPrestation = null) ? les audiences redeviennent "sans prestation"
Étape 4 : Créer la NOTE DE CREDIT (Facture enfant)
Étape 5 : Copier les paiements en négatif (SQL brut RETURNING INTO)
Étape 6 : Copier les prestations (SQL brut, heures identiques)
Étape 7 : Détacher les entités originales du change tracker EF
Étape 8 : Supprimer les prestations originales (SQL brut DELETE)
Étape 9 : Supprimer les paiements originaux (SQL brut DELETE)
```

### 6.3 Création de la note de crédit

```csharp
var noteDeCredit = new Facture
{
    Tolkcode = facture.Tolkcode,
    DateGeneration = DateTime.Now,
    StatutFacture = "NOTE DE CREDIT",
    TotalTtc = -facture.TotalTtc,           // montant négatif
    IdFactureOrigine = facture.IdFacture     // lien vers la facture annulée
};
```

**TotalTtc négatif** : la note de crédit porte le montant inverse de la facture annulée.

### 6.4 Copie des paiements en négatif

Pour chaque paiement de la facture annulée, un nouveau paiement est inséré via SQL brut :

```sql
INSERT INTO PAIEMENT (ID_PAIEMENT, TOLKCODE, DATE_PRESTATION, MONTANT, TRANSPORT, TOTAL, MONTANT_TVA, ID_FACTURE)
VALUES (NR_AUTO_PAIEMENT.NEXTVAL, :tk, :dp, :mt, :tr, :tot, :tva, :idf)
RETURNING ID_PAIEMENT INTO :newid
```

Les montants sont **inversés** :
```csharp
AddParam("mt", (object?)(-(p.Montant ?? 0m)), DbType.Decimal);     // -Montant
AddParam("tr", (object?)(-(p.Transport ?? 0m)), DbType.Decimal);   // -Transport
AddParam("tot", (object?)(-(p.Total ?? 0m)), DbType.Decimal);      // -Total
AddParam("tva", (object?)(-(p.MontantTva ?? 0m)), DbType.Decimal); // -TVA
```

**Pourquoi SQL brut ?** Le provider Oracle EF Core ne gère pas correctement `NEXTVAL` via `Add/SaveChanges` quand le change tracker contient d'autres entités avec la même PK (détachées entre-temps).

### 6.5 Copie des prestations

Les prestations sont copiées à l'identique (mêmes heures, même date) et rattachées aux nouveaux paiements négatifs. Elles servent à générer le PDF de la note de crédit avec le même détail que la facture originale.

### 6.6 Suppression des originaux

Les prestations et paiements **originaux** sont supprimés physiquement de la base (DELETE SQL brut). Les TOLKLINK sont libérés (IdPrestation = null) pour permettre un ré-encodage.

### 6.7 Marquage de la facture

```csharp
facture.StatutFacture = "ANNULEE";
// DateValidationFedcom conservée pour l'historique
```

### 6.8 Résultat final en base après annulation

```
FACTURE originale  :  StatutFacture = "ANNULEE",  TotalTtc = +X
FACTURE crédit     :  StatutFacture = "NOTE DE CREDIT",  TotalTtc = -X,  IdFactureOrigine = id_originale
PAIEMENT originaux :  supprimés
PAIEMENT crédit    :  montants négatifs, liés à la facture crédit
PRESTATION origin. :  supprimées
PRESTATION crédit  :  mêmes heures, liées aux paiements crédit
TOLKLINK           :  IdPrestation = null (audiences libérées)
```

---

## 7. Assignation et gestion des audiences

### 7.1 Assignation — `POST /api/interpretes/{tolkcode}/tolklink`

```csharp
// Vérification de doublon
var count = await _db.Tolklinks
    .Where(x => x.Tolkcode == tolkcode
                && x.NrAffAudience == dto.NrAffAudience
                && x.Datesupp == null)
    .CountAsync();

if (count > 0) return Conflict("Lien déjà existant.");
```

Crée un lien `TOLKLINK` entre l'interprète et l'audience. Unicité vérifiée : un interprète ne peut être assigné qu'une seule fois à la même audience (si le lien actif `Datesupp == null` existe).

### 7.2 Assignation bulk — `POST /api/interpretes/{tolkcode}/tolklink/bulk`

Insère plusieurs liens en une requête. Filtre les doublons avant insertion :
```csharp
var already = await _db.Tolklinks
    .Where(x => x.Tolkcode == tolkcode && x.Datesupp == null
                && ids.Contains((int)x.NrAffAudience.Value))
    .Select(x => x.NrAffAudience!.Value)
    .ToListAsync();

var toInsert = ids.Except(already).ToList();
```

### 7.3 Désassignation (soft delete)

```csharp
row.Datesupp = DateTime.Now;
row.Datemodif = DateTime.Now;
```

Le lien n'est pas supprimé physiquement. `Datesupp != null` = lien inactif.

### 7.4 Marquage absence — `POST /api/prestations/absence`

```csharp
foreach (var link in links)
    link.Datesupp = dto.DatePrestation.Date;
```

Même mécanisme que la désassignation : renseigne `Datesupp` sur les TOLKLINK de l'interprète pour les audiences spécifiées.

### 7.5 Remplacement — `POST /api/prestations/remplacement`

```csharp
link.Tolkcode = nouveauInt;
```

Change le `TOLKCODE` du TOLKLINK existant. **Pas de nouvelle prestation créée, pas de nouveau paiement**. Le lien est simplement transféré à un autre interprète.

**?? Limite documentée** : si l'ancien interprète avait déjà une prestation encodée pour cette audience, elle reste avec son ancien tolkcode. Le remplacement ne déclenche pas de recalcul.

---

## 8. Gestion des adresses — logique "Replace"

### 8.1 Endpoint `POST /api/interpretes/{tolkcode}/adresses/replace`

```csharp
// 1. Trouver l'adresse active courante
var active = await _db.Tolkadresses
    .Where(a => a.Tolkcode == sCode && a.Enddate == null)
    .OrderByDescending(a => a.Startdate)
    .FirstOrDefaultAsync();

// 2. Clôturer l'active (Enddate = veille de la nouvelle Startdate)
if (active != null)
{
    active.Enddate = body.Startdate.Date.AddDays(-1);
}

// 3. Créer la nouvelle adresse (Enddate = null ? devient l'active)
var newAdr = new Tolkadresse { ..., Enddate = null };
```

**Règle** : à tout moment, un seul enregistrement d'adresse a `Enddate == null` (adresse active). L'appel "replace" clôture automatiquement l'ancienne et crée la nouvelle.

### 8.2 Impact sur le calcul

Le champ `KM` de l'adresse active à la date de prestation détermine le montant du transport. Si l'adresse change en cours de mois, les prestations avant le changement utilisent l'ancien KM, celles après utilisent le nouveau.

---

## 9. Génération PDF — `FacturesBatchPdfDocument`

### 9.1 Format

- A4, marges 40px left/right, 50px top, 40px bottom
- 1 page par facture/note de crédit
- Police par défaut 10pt
- Culture `nl-BE` ou `fr-BE` selon `TAALROL`

### 9.2 Sections du PDF

| Section | Contenu |
|---|---|
| Titre | `FACTUUR` / `FACTURE` (ou `CREDITNOTA` / `NOTE DE CRÉDIT` en rouge) |
| Références | Ref `RVV-CCE/{id}`, N° facture (vide à remplir), N° entreprise `0308356862`, PO |
| Fournisseur | Nom, adresse (Rue + N° + Bte, CP + Commune) |
| Client | Account Payable IBZ (bilingue) |
| Bloc TVA/Bank | N° TVA, Kenmerk (=tolkcode), Bankrekening (BBAN formaté xxx-xxxxxxx-xx), Fedcom |
| Tableau | Date, Début, Fin, Durée, Km, € prestation, € déplacement |
| Totaux | Total prestation HT, Total déplacement HT, Total HT, TVA 21% (si applicable), Total TTC |
| Signature | "Date et signature" / "Datum en handtekening" |

### 9.3 Bilingue

La langue est déterminée par `TOLKIDENTITY.TAALROL` :
- `TAALROL == 1` ? NL
- `TAALROL != 1` (y compris null, 2) ? FR

### 9.4 Note de crédit dans le PDF

- Titre rouge `#991b1b` au lieu de bleu `#1e3a5f`
- Titre inclut la référence de la facture annulée : `"NOTE DE CRÉDIT de la facture RVV-CCE/42"`
- Montants affichés en négatif (car stockés en négatif dans les paiements)

---

## 10. Méthodes utilitaires sur le DbContext

### 10.1 Chaîne de filtrage des annulations

```csharp
// ApplicationDbContext.cs

GetFacturesAnnulees(ct)
    ? Factures WHERE StatutFacture IN ("ANNULEE", "NOTE DE CREDIT", "CREDIT VALIDE")

GetPaiementsAnnules(ct)
    ? Paiements WHERE IdFacture IN (GetFacturesAnnulees)

GetPrestationsAnnulees(ct)
    ? Prestations WHERE IdPaiement IN (GetPaiementsAnnules)
```

Ces méthodes sont utilisées par d'autres contrôleurs pour exclure les prestations/paiements liés à des factures annulées ou des notes de crédit des affichages et calculs courants.

---

## 11. Suppression d'un paiement — `DELETE /api/paiements/{id}`

### 11.1 Pré-condition

```csharp
if (paiement.IdFacture != null)
    return BadRequest("Impossible de supprimer un paiement déjà facturé.");
```

Seuls les paiements **non facturés** peuvent être supprimés.

### 11.2 Algorithme

```
1. Trouver les prestations liées au paiement
2. Libérer les TOLKLINK (IdPrestation = null)
3. Supprimer les prestations (EF Remove)
4. Supprimer le paiement (EF Remove)
```

---

## 12. Synthèse des constantes métier codées en dur

| Constante | Valeur | Emplacement | Configurable |
|---|---|---|---|
| Taux TVA | `0.21` (21%) | `PrestationsController.TVA_RATE` | ? `const` |
| Minimum facturable | 75 minutes | `CalculerEtMettreAJourPaiementAsync` | ? hardcodé |
| Plafond KM A/R | 100 km | `CalculerEtMettreAJourPaiementAsync` | ? hardcodé |
| Multiplicateur KM | × 2 (aller-retour) | `CalculerEtMettreAJourPaiementAsync` | ? hardcodé |
| Arrondi durée | quart d'heure supérieur (15 min) | `CalculerEtMettreAJourPaiementAsync` | ? hardcodé |
| IdStatut assujetti TVA | `1` | `CalculerEtMettreAJourPaiementAsync` | ? valeur magique |
| N° entreprise | `0308356862` | `FacturesController` | ? hardcodé |
| PO Fedcom par défaut | `4501133577` | Côté Angular uniquement | ? modifiable dans l'interface |
| Référence facture | `RVV-CCE/{IdFacture}` | `FacturesController` | ? hardcodé |
| TAALROL NL | `1` | Partout | ? valeur magique |
| TAALROL FR | `2` (ou tout sauf 1) | Partout | ? valeur magique |
| Adresse client (FR) | `1 Rue de Louvain, 1000 BRUXELLES` | `FacturesController`, `PaiementsController` | ? hardcodé |
| Adresse client (NL) | `Leuvenstraat 1, 1000 BRUSSEL` | `FacturesController`, `PaiementsController` | ? hardcodé |
