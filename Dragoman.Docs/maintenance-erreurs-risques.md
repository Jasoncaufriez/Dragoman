# Dragoman — Logs, gestion erreurs, points critiques et procédure de maintenance

---

## 1. Exceptions non gérées

### 1.1 Absence de middleware global d'erreur

`Program.cs` ne contient **aucun** middleware de gestion d'erreur :

```
? app.UseExceptionHandler(...)    ? absent
? app.UseStatusCodePages(...)     ? absent
? app.UseDeveloperExceptionPage() ? absent (même en dev)
```

**Conséquence** : toute exception non attrapée dans un contrôleur produit une réponse HTTP 500 avec le corps par défaut d'ASP.NET Core :
- En production : corps vide ou page d'erreur IIS générique
- En développement : stacktrace complète dans la réponse HTTP (fuite d'information si Swagger est accidentellement exposé)

### 1.2 Exception explicite critique — `CalculerEtMettreAJourPaiementAsync`

```csharp
if (idx == null)
    throw new InvalidOperationException("Aucune ligne d'indexation active pour cette date.");
```

| Scénario | Impact |
|---|---|
| Table INDEXATION vide | **500 sur chaque création de prestation** — toute la fonctionnalité de saisie est bloquée |
| Trou de dates entre deux lignes d'indexation | 500 pour les prestations dont la date tombe dans le trou |
| Nouvelle indexation créée sans clôturer l'ancienne | Double indexation, le `FirstOrDefault()` retourne un résultat indéterministe |

### 1.3 Retours silencieux — données manquantes sans erreur

```csharp
// CalculerEtMettreAJourPaiementAsync
var paiement = await _db.Paiements.FirstOrDefaultAsync(...);
if (paiement == null) return;   // ? retour silencieux, pas de log

if (!int.TryParse(prestation.Tolkcode, out var tolkcodeInt))
    return;                      // ? retour silencieux, pas de log
```

Si le paiement ou le tolkcode est invalide, la méthode retourne sans erreur. Le paiement restera avec `Montant=0, Transport=0, Total=0`. **Aucun log n'est émis** — le problème est invisible.

### 1.4 Connexion Oracle non gérée dans `NextValAsync`

```csharp
private async Task<int> NextValAsync(string sequenceName, CancellationToken ct)
{
    var conn = _db.Database.GetDbConnection();
    var wasClosed = conn.State != ConnectionState.Open;
    if (wasClosed) await conn.OpenAsync(ct);
    try
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT {sequenceName}.NEXTVAL FROM DUAL";
        var obj = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(obj);
    }
    finally
    {
        if (wasClosed) await conn.CloseAsync();
    }
}
```

| Risque | Détail |
|---|---|
| **SQL injection** | `sequenceName` est interpolé directement dans le SQL. Pas de risque en pratique (valeurs hardcodées), mais pattern dangereux |
| **Connexion orpheline** | Si `OpenAsync` réussit mais `ExecuteScalarAsync` lève une exception Oracle (ex: séquence inexistante), le `finally` ferme la connexion que EF Core a peut-être réouverte entre-temps |
| **Conflit avec le pool EF Core** | Ouvrir/fermer manuellement la connexion du DbContext peut interférer avec la gestion de connexion d'EF Core |

### 1.5 Annulation de facture — SQL brut sans paramétrage

```csharp
// FacturesController.UpdateStatut — étape 8
var prestaIdList = string.Join(",", prestationIds);
await _db.Database.ExecuteSqlRawAsync(
    $"DELETE FROM PRESTATION WHERE ID_PRESTATION IN ({prestaIdList})", ct);

var paiIdList = string.Join(",", paiementIds);
await _db.Database.ExecuteSqlRawAsync(
    $"DELETE FROM PAIEMENT WHERE ID_PAIEMENT IN ({paiIdList})", ct);
```

| Risque | Détail |
|---|---|
| **SQL injection** | Les IDs sont des `int` extraits d'EF Core donc le risque est théorique, mais le pattern `ExecuteSqlRawAsync` avec interpolation de string est un anti-pattern documenté par Microsoft |
| **Limite IN clause** | Oracle a une limite de 1000 éléments dans un `IN(...)`. Une facture avec >1000 paiements provoquera une `ORA-01795` |
| **Pas de vérification** | Aucun compteur de lignes affectées n'est vérifié — si le DELETE échoue partiellement, la transaction est quand même commitée |

---

## 2. Logs existants

### 2.1 Logs ASP.NET Core — configuration

```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

| Source | Niveau | Ce qui est loggé |
|---|---|---|
| ASP.NET Core framework | Warning+ | Erreurs de routing, binding model failures, auth failures |
| EF Core | Information+ | Requêtes SQL (si `Microsoft.EntityFrameworkCore` n'est pas filtré) |
| Application (contrôleurs) | **Rien** | Aucun `ILogger` injecté dans aucun contrôleur |

### 2.2 stdout IIS — `web.config`

```xml
<aspNetCore ... stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
```

Les logs stdout sont activés en production. Ils capturent :
- Les logs `Console.WriteLine` (utilisé dans `AdStatusController`)
- Les exceptions non gérées (stacktrace complète)
- Les messages de démarrage/arrêt du processus

**Problème** : aucune rotation de logs n'est configurée. Les fichiers `logs\stdout_YYYYMMDDHHMMSS_XXXXX.log` s'accumulent indéfiniment.

### 2.3 Aucun logger applicatif

Aucun contrôleur n'injecte `ILogger<T>`. Le seul log explicite dans l'application est :

```csharp
// AdStatusController
Console.WriteLine($"Erreur lors du chargement du fichier de persistance: {ex.Message}");
```

`Console.WriteLine` en prod IIS in-process ? capturé dans les logs stdout, mais :
- Pas de niveau de sévérité
- Pas de structured logging
- Pas de corrélation avec les requêtes HTTP

### 2.4 Côté Angular

```typescript
// app.component.ts
error: (error) => console.error(error)

// main.ts
.catch((err) => console.error(err));

// authentification.service.ts — warmup()
catchError(() => {
  this.loginSubject.next(null);
  return of('');
})
```

- Toutes les erreurs sont `console.error` ? uniquement dans la console du navigateur
- Aucun service centralisé d'erreur (`ErrorHandler` Angular non override)
- L'échec du handshake NTLM est silencieusement ignoré

---

## 3. Points sensibles Oracle

### 3.1 Mismatch types — Tolkcode `int` vs `string`

Le champ `TOLKCODE` est stocké comme `NUMBER` dans certaines tables et `VARCHAR2` dans d'autres :

| Table | Type C# | Type Oracle |
|---|---|---|
| `TOLKIDENTITY.TOLKCODE` | `int` | `NUMBER` |
| `TOLKADRESSE.TOLKCODE` | `string` | `VARCHAR2` |
| `PRESTATION.TOLKCODE` | `string` | `VARCHAR2` |
| `PAIEMENT.TOLKCODE` | `string` | `VARCHAR2` |
| `TOLKLINK.TOLKCODE` | `int` | `NUMBER` |
| `TOLK_TVA.TOLKCODE` | `int` | `NUMBER` |

**Conséquence** : les jointures entre tables exigent des conversions `int.ToString()` / `int.TryParse()` à chaque requête, avec risques de :
- `ORA-01722: invalid number` si un TOLKCODE non numérique existe dans les tables VARCHAR2
- Scan complet de table (full table scan) si Oracle ne peut pas utiliser l'index à cause de la conversion implicite

### 3.2 ORA-01722 — documenté dans le code

```csharp
// PaiementsController — commentaire existant
// ? IMPORTANT : évite ORA-01722 + évite que "pdf" soit capturé ici
[HttpGet("mois/{tolkcode:int}")]
```

La contrainte de route `{tolkcode:int}` est le seul garde-fou. Si un appel arrive avec un tolkcode non numérique via un autre chemin, l'erreur Oracle se propage comme une 500.

### 3.3 Vues Oracle en lecture seule

```csharp
// VUE_CALENDAR_ALL, VUE_CALENDAR_ANN, V_AUDIENCE_INTERPRETE_DETAIL, V_INTERPRETES_AUDIENCES_JOUR
```

Ces vues sont mappées en `keyless` et `AsNoTracking()`. Elles dépendent du schéma Oracle sous-jacent :
- Si une colonne est renommée/supprimée dans la vue Oracle ? `OracleException` au runtime (pas détecté à la compilation)
- Si la vue retourne des résultats inattendus (ex: doublons) ? données incohérentes sans erreur

### 3.4 Oracle NUMBER ? decimal/int — perte de précision

```csharp
// VueCalendarVrmPc
e.Property(x => x.IdAffAudience).HasColumnName("ID_AFF_AUDIENCE"); // decimal
```

Certains ID sont mappés en `decimal` côté C# (Oracle `NUMBER` sans précision). Les jointures castent en `(int)`:

```csharp
on tl.NrAffAudience equals (int)vrm.IdAffAudience!
```

Si `ID_AFF_AUDIENCE` > `int.MaxValue` (2 147 483 647) ? `OverflowException` au runtime.

### 3.5 Connexions Oracle — pool exhaustion

Le `DbContext` est enregistré en mode **Scoped** (une instance par requête HTTP). Chaque requête HTTP ouvre une connexion au pool Oracle. Pas de configuration de pool visible :

```csharp
options.UseOracle(builder.Configuration.GetConnectionString("DefaultConnection"))
// ? pas de "Min Pool Size", "Max Pool Size", "Connection Timeout" dans la connection string
```

Le pool par défaut d'Oracle Managed Data Access est de **100 connexions**. Sous charge, les requêtes en attente de connexion bloqueront jusqu'au timeout Oracle (15 secondes par défaut).

---

## 4. Séquences Oracle

### 4.1 Séquences déclarées

```csharp
modelBuilder.HasSequence<int>("ID_PRESTATION_AUTO");
modelBuilder.HasSequence<int>("NR_AUTO_PAIEMENT");
modelBuilder.HasSequence<int>("NR_AUTO_TOLKLINK");
modelBuilder.HasSequence<int>("NR_AUTO_ADRESSE");
// Implicite (dans Facture mapping) :
// NR_AUTO_FACTURE
```

### 4.2 Mécanismes d'obtention des IDs

| Table | Mécanisme | Risque |
|---|---|---|
| `PRESTATION` | `NextValAsync("ID_PRESTATION_AUTO")` — appel SQL brut `SELECT ... FROM DUAL` | Connexion manuelle, conflit pool EF |
| `PAIEMENT` | `NextValAsync("NR_AUTO_PAIEMENT")` — idem | Idem |
| `TOLKLINK` | `ValueGeneratedOnAdd().HasDefaultValueSql("NR_AUTO_TOLKLINK.NEXTVAL")` — géré par EF/Oracle | ? Correct |
| `TOLKADRESSE` | `ValueGeneratedOnAdd().HasDefaultValueSql("NR_AUTO_ADRESSE.NEXTVAL")` — géré par EF/Oracle **MAIS** aussi `NextIdAdresseAsync()` dans AdressesController | Double mécanisme, risque de conflit |
| `FACTURE` | `ValueGeneratedOnAdd().HasDefaultValueSql("NR_AUTO_FACTURE.NEXTVAL")` — géré par EF/Oracle | ? Correct |

### 4.3 Incohérence AdressesController

```csharp
// AdressesController.Create()
if (body.IdAdresse == null || Convert.ToDecimal(body.IdAdresse) == 0)
    body.IdAdresse = (int)await NextIdAdresseAsync();

// AdressesController.ReplaceOrCreate()
IdAdresse = (int)await NextIdAdresseAsync(),
```

L'adresse a `ValueGeneratedOnAdd` dans le `DbContext` (EF Core demanderait automatiquement le NEXTVAL), **mais** le contrôleur appelle manuellement la séquence avant `SaveChanges`. Si les deux mécanismes sont actifs, un appel EF Core pur consommerait la séquence 2 fois.

### 4.4 Séquences dans l'annulation — SQL brut

```sql
-- FacturesController.UpdateStatut (annulation)
INSERT INTO PAIEMENT (ID_PAIEMENT, ...) VALUES (NR_AUTO_PAIEMENT.NEXTVAL, ...)
INSERT INTO PRESTATION (ID_PRESTATION, ...) VALUES (ID_PRESTATION_AUTO.NEXTVAL, ...)
```

Les inserts SQL bruts utilisent directement `.NEXTVAL` — pas de conflit avec EF Core car les entités sont détachées du change tracker avant l'insert.

### 4.5 Trous de séquence

En cas de rollback de transaction, les valeurs de séquence consommées sont **perdues** (comportement normal Oracle). Les IDs ne sont pas nécessairement contigus. Ce n'est pas un bug, mais peut surprendre lors des audits.

---

## 5. Gestion de la concurrence

### 5.1 Aucun mécanisme de concurrence optimiste

```
? [ConcurrencyCheck]     ? absent sur toutes les entités
? [Timestamp]            ? absent
? RowVersion             ? absent
? IsRowVersion()         ? absent dans OnModelCreating
```

**Conséquence** : si deux utilisateurs modifient la même entité simultanément, le dernier `SaveChanges` écrase silencieusement les modifications du premier ("last writer wins").

### 5.2 Transport — race condition

```csharp
// CalculerEtMettreAJourPaiementAsync
var dejaTransportJour = paiementsJour.Any(pa => pa.Transport > 0);
var transport = dejaTransportJour ? 0m : euroKm * kmAR;
```

**Scénario** : deux prestations créées simultanément pour le même interprète le même jour.
1. Requête A vérifie : aucun transport payé ? `transport = euroKm * kmAR`
2. Requête B vérifie (avant que A ait committé) : aucun transport payé ? `transport = euroKm * kmAR`
3. Les deux transactions committent ? **transport payé deux fois**.

Pas de verrouillage pessimiste (`SELECT ... FOR UPDATE`) ni de vérification post-commit.

### 5.3 Génération de factures — double exécution

```csharp
// FacturesController.Generer
var paiements = await _db.Paiements
    .Where(p => p.IdFacture == null && ...)
    .ToListAsync(ct);
// ... boucle de création
foreach (var p in g)
    p.IdFacture = facture.IdFacture;
```

Si deux utilisateurs cliquent "Générer" pour le même mois simultanément :
1. Les deux requêtes lisent les mêmes paiements avec `IdFacture == null`
2. Les deux créent des factures pour le même interprète
3. Le second `SaveChanges` écrase le `IdFacture` du premier ? **paiements déplacés** vers une facture inattendue, **première facture vide** mais existante en base

### 5.4 AdStatusController — file-level concurrence

```csharp
private Dictionary<string, AdUserPersistence> _persistenceData = new();
private readonly object _lock = new object();

private void SavePersistenceData()
{
    lock (_lock)
    {
        var jsonString = JsonSerializer.Serialize(...);
        System.IO.File.WriteAllText(_persistencePath, jsonString);
    }
}
```

| Problème | Détail |
|---|---|
| **Instance per-request** | `AdStatusController` est créé par le DI à chaque requête. Le `_lock` est une instance locale ? **le verrou ne protège rien** entre les requêtes |
| **`_persistenceData` rechargé à chaque requête** | `LoadPersistenceData()` est appelé dans le constructeur ? deux POST concurrents peuvent écraser le fichier JSON mutuellement |
| **Pas de FileShare.Write** | `File.ReadAllText` et `File.WriteAllText` ne spécifient pas de mode de partage ? `IOException` si le script PowerShell écrit `AD_Users.csv` au même moment |

### 5.5 Transactions — couverture inégale

| Opération | Transaction explicite | Risque si crash |
|---|---|---|
| `POST /api/prestations` (Create) | ? `BeginTransactionAsync` | ? Rollback automatique |
| `POST /api/factures/generer` | ? `BeginTransactionAsync` | ? Rollback automatique |
| `PATCH /api/factures/{id}/statut` (annulation) | ? `BeginTransactionAsync` | ? Rollback automatique |
| `DELETE /api/paiements/{id}` | ? Pas de transaction | ?? Si le `Remove(paiement)` échoue après le `RemoveRange(prestations)`, les prestations sont supprimées mais pas le paiement |
| `POST /api/prestations/absence` | ? Pas de transaction | ?? Si `SaveChanges` échoue au milieu de la boucle, certains links ont `Datesupp` et d'autres non |
| `POST /api/prestations/remplacement` | ? Pas de transaction | Faible risque (un seul `SaveChanges`) |
| `POST /api/interpretes/{tolkcode}/adresses/replace` | ? `BeginTransactionAsync` | ? Rollback automatique |

---

## 6. Risques techniques

### 6.1 Matrice de risques

| # | Risque | Sévérité | Probabilité | Composant | Description |
|---|---|---|---|---|---|
| R1 | **Table INDEXATION vide** | ?? Critique | Faible | PrestationsController | `InvalidOperationException` ? aucune prestation ne peut être créée. Pas de fallback |
| R2 | **Double transport** | ?? Moyen | Moyenne | PrestationsController | Race condition sur la vérification transport/jour. Impact financier |
| R3 | **Double facture** | ?? Critique | Moyenne | FacturesController | Deux clics "Générer" ? factures dupliquées. Correction manuelle nécessaire |
| R4 | **Connexion Oracle saturée** | ?? Moyen | Faible | Global | Pool 100 connexions par défaut, pas de health check |
| R5 | **Séquence Oracle désynchronisée** | ?? Moyen | Faible | AdressesController | Double mécanisme NextVal + ValueGeneratedOnAdd |
| R6 | **Perte données AdStatus** | ?? Moyen | Moyenne | AdStatusController | Concurrence fichier JSON, lock inutile (instance par requête) |
| R7 | **SQL injection** | ?? Faible | Très faible | FacturesController | `ExecuteSqlRawAsync` avec interpolation d'IDs entiers |
| R8 | **Oracle IN > 1000** | ?? Moyen | Faible | FacturesController | Annulation facture avec >1000 paiements ? `ORA-01795` |
| R9 | **Logs inexistants** | ?? Moyen | Constante | Tous contrôleurs | Aucun ILogger, aucun structured logging. Diagnostic d'incident impossible |
| R10 | **Secrets dans Git** | ?? Critique | Certaine | appsettings*.json | Mot de passe Oracle + IP prod publiés sur GitHub public |
| R11 | **Pas de healthcheck** | ?? Moyen | Constante | Program.cs | Pas de `/health` endpoint pour monitoring. Si Oracle est down, pas de signal proactif |
| R12 | **Interceptor NTLM non branché** | ?? Moyen | Constante | Angular | `CredentialsInterceptor` existe mais n'est pas enregistré dans `app.module.ts` `HTTP_INTERCEPTORS` |
| R13 | **DELETE paiement sans transaction** | ?? Moyen | Faible | PaiementsController | Suppression partielle possible en cas d'erreur |

---

## 7. Procédure de maintenance

### 7.1 Maintenance quotidienne

#### 7.1.1 Vérifier les logs stdout IIS

```powershell
# Sur le serveur rvv-ccesrv21
$logDir = "C:\inetpub\Dragoman\logs"

# Dernières erreurs (dernières 24h)
Get-ChildItem $logDir -Filter "stdout_*.log" |
    Where-Object { $_.LastWriteTime -gt (Get-Date).AddDays(-1) } |
    ForEach-Object { Select-String -Path $_.FullName -Pattern "fail|error|exception|ORA-" -CaseSensitive:$false } |
    Select-Object -Last 50
```

#### 7.1.2 Vérifier la taille des logs

```powershell
# Taille totale des logs
$total = (Get-ChildItem $logDir -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "Logs: $([math]::Round($total, 1)) MB"

# Alerte si > 500 MB
if ($total -gt 500) { Write-Warning "?? Logs stdout dépassent 500 MB. Purger les anciens." }
```

#### 7.1.3 Vérifier la connexion Oracle

```powershell
# Test rapide via l'API
$response = Invoke-WebRequest -Uri "http://rvv-ccesrv21/api/interpretes?take=1" -UseDefaultCredentials -TimeoutSec 10
if ($response.StatusCode -eq 200) {
    Write-Host "? Connexion Oracle OK"
} else {
    Write-Warning "?? API retourne $($response.StatusCode)"
}
```

### 7.2 Maintenance hebdomadaire

#### 7.2.1 Purge des logs stdout (> 30 jours)

```powershell
$logDir = "C:\inetpub\Dragoman\logs"
$cutoff = (Get-Date).AddDays(-30)

Get-ChildItem $logDir -Filter "stdout_*.log" |
    Where-Object { $_.LastWriteTime -lt $cutoff } |
    Remove-Item -Verbose
```

#### 7.2.2 Vérifier les séquences Oracle

```sql
-- Vérifier que les séquences ne sont pas proches de MAXVALUE
SELECT SEQUENCE_NAME, LAST_NUMBER, MAX_VALUE, INCREMENT_BY
FROM USER_SEQUENCES
WHERE SEQUENCE_NAME IN (
    'ID_PRESTATION_AUTO',
    'NR_AUTO_PAIEMENT',
    'NR_AUTO_TOLKLINK',
    'NR_AUTO_ADRESSE',
    'NR_AUTO_FACTURE'
);

-- Vérifier la cohérence séquence vs max ID en table
SELECT 'PRESTATION' AS T,
       (SELECT MAX(ID_PRESTATION) FROM PRESTATION) AS MAX_TABLE,
       (SELECT LAST_NUMBER FROM USER_SEQUENCES WHERE SEQUENCE_NAME = 'ID_PRESTATION_AUTO') AS SEQ_LAST
FROM DUAL
UNION ALL
SELECT 'PAIEMENT',
       (SELECT MAX(ID_PAIEMENT) FROM PAIEMENT),
       (SELECT LAST_NUMBER FROM USER_SEQUENCES WHERE SEQUENCE_NAME = 'NR_AUTO_PAIEMENT')
FROM DUAL
UNION ALL
SELECT 'FACTURE',
       (SELECT MAX(ID_FACTURE) FROM FACTURE),
       (SELECT LAST_NUMBER FROM USER_SEQUENCES WHERE SEQUENCE_NAME = 'NR_AUTO_FACTURE')
FROM DUAL;
```

Si `MAX_TABLE > SEQ_LAST` ? la séquence est désynchronisée. Résoudre :
```sql
-- Exemple : avancer la séquence NR_AUTO_PAIEMENT
ALTER SEQUENCE NR_AUTO_PAIEMENT INCREMENT BY 1000;
SELECT NR_AUTO_PAIEMENT.NEXTVAL FROM DUAL;
ALTER SEQUENCE NR_AUTO_PAIEMENT INCREMENT BY 1;
```

#### 7.2.3 Vérifier les factures orphelines

```sql
-- Factures GENEREE sans paiements liés (possible après annulation partielle)
SELECT f.ID_FACTURE, f.TOLKCODE, f.STATUT_FACTURE, f.TOTAL_TTC
FROM FACTURE f
WHERE f.STATUT_FACTURE = 'GENEREE'
  AND NOT EXISTS (SELECT 1 FROM PAIEMENT p WHERE p.ID_FACTURE = f.ID_FACTURE);
```

#### 7.2.4 Vérifier les paiements à Montant=0

```sql
-- Paiements avec montant 0 (calcul échoué silencieusement)
SELECT p.ID_PAIEMENT, p.TOLKCODE, p.DATE_PRESTATION, p.MONTANT, p.TRANSPORT, p.TOTAL
FROM PAIEMENT p
WHERE p.MONTANT = 0 AND p.TOTAL = 0
  AND p.ID_FACTURE IS NULL;
```

### 7.3 Maintenance mensuelle

#### 7.3.1 Vérifier la table INDEXATION

```sql
-- Barème actif courant
SELECT * FROM INDEXATION WHERE ENDDATE IS NULL;

-- Trous de dates (jours non couverts par un barème)
SELECT a.ENDDATE AS "Fin barème A", b.STARTDATE AS "Début barème B",
       b.STARTDATE - a.ENDDATE AS "Trou (jours)"
FROM INDEXATION a
JOIN INDEXATION b ON b.STARTDATE > a.STARTDATE
WHERE a.ENDDATE IS NOT NULL
  AND b.STARTDATE > a.ENDDATE
ORDER BY a.ENDDATE;
```

#### 7.3.2 Vérifier les interprètes sans adresse active

```sql
-- Interprètes avec des prestations récentes mais sans adresse active
SELECT DISTINCT p.TOLKCODE
FROM PRESTATION p
WHERE p.DATE_PRESTATION >= ADD_MONTHS(SYSDATE, -1)
  AND NOT EXISTS (
    SELECT 1 FROM TOLKADRESSE a
    WHERE a.TOLKCODE = p.TOLKCODE
      AND a.ENDDATE IS NULL
  );
```

#### 7.3.3 Vérifier la cohérence TVA

```sql
-- Interprètes avec TVA facturée mais pas de statut TVA actif
SELECT DISTINCT pa.TOLKCODE
FROM PAIEMENT pa
WHERE pa.MONTANT_TVA > 0
  AND pa.DATE_PRESTATION >= ADD_MONTHS(SYSDATE, -3)
  AND NOT EXISTS (
    SELECT 1 FROM TOLK_TVA t
    WHERE t.TOLKCODE = TO_NUMBER(pa.TOLKCODE)
      AND t.ID_STATUT = 1
      AND t.START_DATE <= pa.DATE_PRESTATION
      AND (t.END_DATE IS NULL OR pa.DATE_PRESTATION < t.END_DATE)
  );
```

#### 7.3.4 Espace disque — fichier AD_Users.csv

```powershell
$csvPath = "D:\Dragoman\Data\AD_Users.csv"
$persistPath = "D:\Dragoman\Data\adstatus_persistence.json"

Get-Item $csvPath | Select-Object Name, Length, LastWriteTime
Get-Item $persistPath | Select-Object Name, Length, LastWriteTime

# Vérifier que le CSV est récent (script PowerShell AD tourne quotidiennement)
$lastWrite = (Get-Item $csvPath).LastWriteTime
if ($lastWrite -lt (Get-Date).AddDays(-2)) {
    Write-Warning "?? AD_Users.csv n'a pas été mis à jour depuis $lastWrite"
}
```

### 7.4 Maintenance trimestrielle

#### 7.4.1 Mise à jour du .NET Runtime

```powershell
# Vérifier la version actuelle
dotnet --list-runtimes | Select-String "Microsoft.NETCore.App 8"
dotnet --list-runtimes | Select-String "Microsoft.AspNetCore.App 8"

# Installer la dernière version patch 8.0.x du Hosting Bundle
# Télécharger depuis https://dotnet.microsoft.com/download/dotnet/8.0
```

#### 7.4.2 Mise à jour des packages NuGet

```powershell
cd Dragoman.Server
dotnet list package --outdated
```

Packages critiques à surveiller :
- `Oracle.EntityFrameworkCore` — mises à jour de compatibilité Oracle
- `Oracle.ManagedDataAccess.Core` — patchs de sécurité driver
- `QuestPDF` — licence Community et breaking changes

#### 7.4.3 Rotation du mot de passe Oracle

Le mot de passe `InterTolk` est en clair dans :
1. `appsettings.json`
2. `appsettings.Development.json`
3. `appsettings.Production.json`
4. `publish/appsettings.json`
5. GitHub (historique Git)

Procédure de rotation :
1. Changer le mot de passe Oracle côté DB
2. Mettre à jour `appsettings.Production.json` sur le serveur (pas dans Git)
3. Recycler le pool d'application IIS

### 7.5 Procédure d'urgence — restauration

#### 7.5.1 Erreur 500 généralisée

```powershell
# 1. Vérifier les logs stdout
Get-Content "C:\inetpub\Dragoman\logs\stdout_*.log" -Tail 100

# 2. Vérifier la connectivité Oracle
Test-NetConnection -ComputerName 10.4.4.22 -Port 1529

# 3. Recycler le pool d'application IIS
Restart-WebAppPool -Name "Dragoman"

# 4. Si le problème persiste, redémarrer le site
Stop-IISSite -Name "Dragoman" -Confirm:$false
Start-IISSite -Name "Dragoman"
```

#### 7.5.2 Facture générée en double

```sql
-- Identifier les doublons
SELECT TOLKCODE, DATE_GENERATION, COUNT(*) AS NB
FROM FACTURE
WHERE STATUT_FACTURE = 'GENEREE'
GROUP BY TOLKCODE, DATE_GENERATION
HAVING COUNT(*) > 1;

-- Supprimer la facture vide (sans paiements liés)
DELETE FROM FACTURE
WHERE ID_FACTURE = :id_facture_vide
  AND NOT EXISTS (SELECT 1 FROM PAIEMENT WHERE ID_FACTURE = :id_facture_vide);
```

#### 7.5.3 Séquence désynchronisée — symptôme ORA-00001 (unique constraint)

```sql
-- 1. Trouver le max actuel
SELECT MAX(ID_PAIEMENT) FROM PAIEMENT;  -- ex: 15432

-- 2. Vérifier la séquence
SELECT NR_AUTO_PAIEMENT.CURRVAL FROM DUAL;  -- ex: 15200

-- 3. Avancer la séquence
DECLARE
  v_max NUMBER;
  v_seq NUMBER;
BEGIN
  SELECT MAX(ID_PAIEMENT) INTO v_max FROM PAIEMENT;
  SELECT NR_AUTO_PAIEMENT.NEXTVAL INTO v_seq FROM DUAL;
  IF v_seq <= v_max THEN
    EXECUTE IMMEDIATE 'ALTER SEQUENCE NR_AUTO_PAIEMENT INCREMENT BY ' || (v_max - v_seq + 10);
    SELECT NR_AUTO_PAIEMENT.NEXTVAL INTO v_seq FROM DUAL;
    EXECUTE IMMEDIATE 'ALTER SEQUENCE NR_AUTO_PAIEMENT INCREMENT BY 1';
  END IF;
END;
/
```

#### 7.5.4 Rollback d'un déploiement

```powershell
# Le profil FolderProfile utilise DeleteExistingFiles=true
# Conserver toujours une copie du déploiement précédent

# Avant chaque déploiement :
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
Copy-Item "C:\inetpub\Dragoman" "C:\inetpub\Dragoman_backup_$timestamp" -Recurse

# Rollback :
Stop-IISSite -Name "Dragoman" -Confirm:$false
Remove-Item "C:\inetpub\Dragoman\*" -Recurse -Force
Copy-Item "C:\inetpub\Dragoman_backup_$timestamp\*" "C:\inetpub\Dragoman\" -Recurse
Start-IISSite -Name "Dragoman"
```

---

## 8. Checklist de monitoring recommandée

| Check | Fréquence | Outil | Seuil d'alerte |
|---|---|---|---|
| Site HTTP répond | 5 min | Ping/curl IIS | Timeout > 5s ou status ? 200 |
| Connexion Oracle | 15 min | `GET /api/interpretes?take=1` | Status ? 200 |
| Taille logs stdout | Quotidien | Script PS | > 500 MB |
| Fraîcheur AD_Users.csv | Quotidien | Script PS | LastWrite > 48h |
| Factures orphelines | Hebdo | Requête SQL | Count > 0 |
| Paiements à montant 0 | Hebdo | Requête SQL | Count > 0 |
| Barème indexation actif | Mensuel | Requête SQL | Count = 0 |
| Séquences vs max ID | Mensuel | Requête SQL | SEQ < MAX |
| Espace disque serveur | Hebdo | Script PS | < 5 GB libre |
| Certificats SSL | Trimestriel | N/A actuellement | HTTP uniquement |
