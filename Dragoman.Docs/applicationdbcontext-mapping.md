# ApplicationDbContext — Analyse complète du mapping EF Core

---

## 1. Vue d'ensemble

| Catégorie | Nombre | Noms |
|---|---|---|
| Tables (avec PK) | 13 | TOLKIDENTITY, TOLKADRESSE, LANGUE, LANGUE_SOURCE, LANGUE_DESTINATION, TOLK_TVA, STATUT, TOLKINDISPO, TOLKLINK, PRESTATION, PAIEMENT, FACTURE, INDEXATION |
| Vues (keyless) | 4 | VUE_CALENDAR_ALL, VUE_CALENDAR_ANN, V_INTERPRETES_AUDIENCES_JOUR, V_AUDIENCE_INTERPRETE_DETAIL |
| Relations FK configurées | 3 | TOLKLINK?PRESTATION, PRESTATION?PAIEMENT, FACTURE?PAIEMENT |
| Séquences Oracle déclarées | 4 | ID_PRESTATION_AUTO, NR_AUTO_PAIEMENT, NR_AUTO_TOLKLINK, NR_AUTO_ADRESSE |
| Séquences Oracle utilisées non déclarées | 1 | NR_AUTO_FACTURE (dans `HasDefaultValueSql` mais pas dans `HasSequence`) |

---

## 2. Tables — Détail complet

### 2.1 TOLKIDENTITY

**Entité C#** : `Tolkidentity` | **Table Oracle** : `TOLKIDENTITY`

| Colonne Oracle | Propriété C# | Type C# | Type Oracle | PK | Nullable | Notes |
|---|---|---|---|---|---|---|
| `TOLKCODE` | `Tolkcode` | `int` | `NUMBER(10)` | ? | ? | Clé naturelle — aucune séquence. Valeur allouée via `NR_TOLK.NEXTVAL` en SQL brut dans le controller |
| `TAALROL` | `Taalrol` | `int?` | `NUMBER(10)` | | ? | 1=FR, 2=NL — détermine la langue de facturation |
| `NOM` | `Nom` | `string?` | `VARCHAR2(50)` | | ? | |
| `PRENOM` | `Prenom` | `string?` | `VARCHAR2(50)` | | ? | |
| `TEL` | `Tel` | `string?` | `VARCHAR2(50)` | | ? | |
| `TELBIS` | `Telbis` | `string?` | `VARCHAR2(50)` | | ? | |
| `GSM` | `Gsm` | `string?` | `VARCHAR2(50)` | | ? | |
| `FAX` | `Fax` | `string?` | `VARCHAR2(50)` | | ? | |
| `EMAIL` | `Email` | `string?` | `VARCHAR2(80)` | | ? | |
| `BEEDIGD` | `Beedigd` | `int?` | `NUMBER(10)` | | ? | 1=assermenté, 0=non assermenté |
| `DATE_NAISSANCE` | `DateNaissance` | `DateTime?` | `DATE` | | ? | |
| `NATIONALITEIT` | `Nationaliteit` | `string?` | `VARCHAR2(50)` | | ? | |
| `RIJKSREGISTERNR` | `Rijksregisternr` | `string?` | `VARCHAR2(50)` | | ? | Numéro de registre national belge |
| `GENRE` | `Genre` | `string?` | `VARCHAR2(1)` | | ? | |
| `BANKREKENING` | `Bankrekening` | `string?` | `VARCHAR2(20)` | | ? | Compte bancaire belge formaté |
| `IBAN` | `Iban` | `string?` | `VARCHAR2(34)` | | ? | Utilisé dans les factures |
| `TVA` | `Tva` | `string?` | `VARCHAR2(20)` | | ? | Numéro TVA texte (ex: BE0xxx) |
| `BTW_NR` | `BtwNr` | `int?` | `NUMBER(10)` | | ? | Doublon numérique de TVA |
| `FEDCOMNUMMER` | `Fedcomnummer` | `int?` | `NUMBER(10)` | | ? | Référence paiement Fedcom IBZ |
| `FEDCOM` | `Fedcom` | `int?` | `NUMBER(10)` | | ? | |
| `ONDERNEMINGSNUMMER` | `Ondernemingsnummer` | `int?` | `NUMBER(10)` | | ? | Numéro d'entreprise belge |
| `VESTIGINGSNUMMER` | `Vestigingsnummer` | `string?` | `VARCHAR2(10)` | | ? | |
| `HERKOMST` | `Herkomst` | `string?` | `VARCHAR2(20)` | | ? | |
| `BEROEPSCODE` | `Beroepscode` | `int?` | `NUMBER(10)` | | ? | |
| `EVALUATIECODE` | `Evaluatiecode` | `int?` | `NUMBER(10)` | | ? | |
| `REMARQUE` | `Remarque` | `string?` | `VARCHAR2(250)` | | ? | |
| `BA` | `Ba` | `string?` | `VARCHAR2(11)` | | ? | |
| `ISCCE` | `Iscce` | `string?` | `VARCHAR2(1)` | | ? | Default `0` côté Oracle |
| `RUE` / `ADRESNR` / `POSTID` | `Rue`, `Adresnr`, `Postid` | `string?` | — | | ? | Adresse legacy directe sur TOLKIDENTITY — non utilisée dans l'UI (remplacée par TOLKADRESSE) |

**?? PK sans séquence EF** : `Tolkcode` est déclaré `ValueGeneratedNever()` dans `ModelContext` mais la configuration dans `ApplicationDbContext` ne précise pas ce flag explicitement. La valeur est fournie manuellement via `SELECT NR_TOLK.NEXTVAL FROM DUAL` dans `InterpretesController.Create()`.

**?? Mapping partiel** : seules 7 colonnes sur ~40 sont mappées dans `OnModelCreating` (`HasColumnName`). Les autres sont mappées par convention de nommage EF ou via les attributs `[Column]` sur le modèle.

---

### 2.2 TOLKADRESSE

**Entité C#** : `Tolkadresse` | **Table Oracle** : `TOLKADRESSE`

| Colonne Oracle | Propriété C# | Type C# | Type Oracle | PK | Nullable | Notes |
|---|---|---|---|---|---|---|
| `ID_ADRESSE` | `IdAdresse` | `int` | `NUMBER(5)` | ? | ? | Séquence `NR_AUTO_ADRESSE.NEXTVAL` |
| `TOLKCODE` | `Tolkcode` | `string` | `VARCHAR2(5)` | | ? | **?? TYPE STRING** — alors que `TOLKIDENTITY.TOLKCODE` est `int`. Jointure manuelle nécessaire |
| `LAND` | `Land` | `string` | `VARCHAR2(2)` | | ? | Code pays (ex: BE) |
| `CP` | `Cp` | `string` | `VARCHAR2(7)` | | ? | Code postal |
| `COMMUNE` | `Commune` | `string` | `VARCHAR2(44)` | | ? | |
| `RUE` | `Rue` | `string?` | `VARCHAR2(29)` | | ? | |
| `NUMERO` | `Numero` | `string?` | `VARCHAR2(10)` | | ? | |
| `BOITE` | `Boite` | `string?` | `VARCHAR2(10)` | | ? | |
| `KM` | `Km` | `byte?` | `NUMBER(3)` | | ? | **Distance en km** — saisie manuelle. Détermine le montant transport dans la facture. Max 255 km (byte) |
| `STARTDATE` | `Startdate` | `DateTime` | `DATE` | | ? | Début de validité de l'adresse |
| `ENDDATE` | `Enddate` | `DateTime?` | `DATE` | | ? | Fin de validité — `null` = adresse active courante |
| `DATECREATE` | `Datecreate` | `DateTime` | `DATE` | | ? | Audit création |
| `USERCREATE` | `Usercreate` | `string?` | `VARCHAR2(25)` | | ? | Login Windows créateur |
| `DATEMODIF` | `Datemodif` | `DateTime?` | `DATE` | | ? | Audit modification |
| `USERMODIF` | `Usermodif` | `string?` | `VARCHAR2(25)` | | ? | |

**?? FK logique non définie** : `TOLKCODE` (VARCHAR2) référence `TOLKIDENTITY.TOLKCODE` (NUMBER) — incohérence de type. Aucune FK physique ni relation EF configurée. La navigation `Tolk` est commentée dans le modèle.

**?? KM = byte** : limité à 255 km. Cohérent avec le plafond de remboursement (100 km × 2 = 200 km), mais contraignant si un interprète habite au-delà de 255 km.

---

### 2.3 LANGUE

**Entité C#** : `Langue` | **Table Oracle** : `LANGUE`

| Colonne Oracle | Propriété C# | Type C# | PK | Nullable | Notes |
|---|---|---|---|---|---|
| `IDLANGUE` | `Idlangue` | `byte` | ? | ? | `ValueGeneratedNever()` — valeur saisie manuellement |
| `LIBELLE_FR` | `LibelleFr` | `string?` | | ? | Libellé français de la langue |
| `LIBELLE_NL` | `LibelleNl` | `string?` | | ? | Libellé néerlandais de la langue |
| `CODE_ISO` | `CodeIso` | `string?` | | ? | Code ISO 639 (ex: `fra`, `nld`) |
| `TYPE_LANGUE` | `TypeLangue` | `string?` | | ? | Catégorie interne |
| `ISLANGUE_DESTINATION` | `IslangueDestination` | `bool?` | | ? | **Converti NUMBER(1)** par le converter global. `true` = langue de destination disponible pour l'interprétariat |

**?? PK byte** : limite théorique à 255 langues. Cohérent avec le référentiel en lecture seule du CCE.

---

### 2.4 LANGUE_SOURCE

**Entité C#** : `LangueSource` | **Table Oracle** : `LANGUE_SOURCE`

| Colonne Oracle | Propriété C# | Type C# | PK | Nullable | Notes |
|---|---|---|---|---|---|
| `ID_LANGUESOURCE` | `IdLanguesource` | `int` | ? | ? | Pas de séquence déclarée dans `ApplicationDbContext` — valeur fournie via SQL brut |
| `TOLKCODE` | `Tolkcode` | `int` | | ? | Référence `TOLKIDENTITY.TOLKCODE` — type cohérent (`int`) |
| `NR_LANGUE` | `NrLangue` | `int?` | | ? | Référence `LANGUE.IDLANGUE` — type incohérent (`byte` vs `int?`) |
| `TAALCODE_OLD` | `TaalcodeOld` | `string?` | | ? | Code legacy non utilisé dans l'UI |

**?? Pas de relation EF** : `NrLangue` ? `LANGUE.IDLANGUE` non configurée comme FK.

---

### 2.5 LANGUE_DESTINATION

**Entité C#** : `LangueDestination` | **Table Oracle** : `LANGUE_DESTINATION`

| Colonne Oracle | Propriété C# | Type C# | PK | Nullable | Notes |
|---|---|---|---|---|---|
| `ID_LANGUEDESTINATION` | `IdLanguedestination` | `int` | ? | ? | Pas de séquence déclarée |
| `TOLKCODE` | `Tolkcode` | `int?` | | ? | **Nullable** — incohérent avec `LANGUE_SOURCE.Tolkcode` (non nullable) |
| `NR_LANGUE` | `NrLangue` | `int` | | ? | |

---

### 2.6 TOLK_TVA

**Entité C#** : `TolkTva` | **Table Oracle** : `TOLK_TVA`

| Colonne Oracle | Propriété C# | Type C# | PK | Nullable | Notes |
|---|---|---|---|---|---|
| `ID_TOLK_TVA` | `IdTolkTva` | `int` | ? | ? | Pas de séquence déclarée dans `ApplicationDbContext` |
| `ID_STATUT` | `IdStatut` | `byte` | | ? | FK logique ? `STATUT.ID_STATUT` — non configurée comme relation EF |
| `TOLKCODE` | `Tolkcode` | `int` | | ? | Référence `TOLKIDENTITY.TOLKCODE` |
| `START_DATE` | `StartDate` | `DateTime?` | | ? | Début de la période TVA |
| `END_DATE` | `EndDate` | `DateTime?` | | ? | Fin — `null` = statut TVA actif. Clôturé automatiquement lors de l'ajout d'un nouveau statut |

**?? Pas de relation EF** entre `TOLK_TVA.ID_STATUT` et `STATUT.ID_STATUT`.

---

### 2.7 STATUT

**Entité C#** : `Statut` | **Table Oracle** : `STATUT`

| Colonne Oracle | Propriété C# | Type C# | PK | Nullable | Notes |
|---|---|---|---|---|---|
| `ID_STATUT` | `IdStatut` | `byte` | ? | ? | Référentiel en lecture seule |
| `TYPE_STATUT` | `TypeStatut` | `string?` | | ? | Ex: `"Assujetti TVA"`, `"Exonéré"` |

---

### 2.8 TOLKINDISPO

**Entité C#** : `Tolkindispo` | **Table Oracle** : `TOLKINDISPO`

| Colonne Oracle | Propriété C# | Type C# | PK | Nullable | Notes |
|---|---|---|---|---|---|
| `ID_INDISPO` | `IdIndispo` | `short` | ? | ? | **?? TYPE SHORT** — limite à 32 767 enregistrements |
| `TOLKCODE` | `Tolkcode` | `string` | | ? | **?? TYPE STRING** (VARCHAR2) — incohérent avec `TOLKIDENTITY.TOLKCODE` (int) |
| `STARTINDISPO` | `Startindispo` | `DateTime` | | ? | Début de la période d'indisponibilité |
| `ENDINDISPO` | `Endindispo` | `DateTime?` | | ? | Fin — `null` = période ouverte (indisponible indéfiniment) |
| `MOTIFINDISPO` | `Motifindispo` | `string?` | | ? | Code motif (VARCHAR2(5)) |
| `COMMENTAIRE` | `Commentaire` | `string?` | | ? | Texte libre (VARCHAR2(1000)) |
| `DATECREATE` | `Datecreate` | `DateTime` | | ? | Audit création |
| `USERCREATE` | `Usercreate` | `string?` | | ? | |
| `DATEMODIF` | `Datemodif` | `DateTime?` | | ? | |
| `USERMODIF` | `Usermodif` | `string?` | | ? | |

**?? TOLKCODE en VARCHAR2** : jointure avec `TOLKIDENTITY` nécessite une conversion explicite (`CAST` ou `TO_NUMBER`) dans les requêtes SQL.

---

### 2.9 TOLKLINK

**Entité C#** : `Tolklink` | **Table Oracle** : `TOLKLINK`

| Colonne Oracle | Propriété C# | Type C# | PK | FK | Nullable | Notes |
|---|---|---|---|---|---|---|
| `ID_TOLKLINK` | `IdTolklink` | `int` | ? | | ? | Séquence `NR_AUTO_TOLKLINK.NEXTVAL` |
| `NR_AFF_AUDIENCE` | `NrAffAudience` | `int?` | | | ? | Référence audience — **pas de FK physique vers les vues** `VUE_CALENDAR_*` |
| `TOLKCODE` | `Tolkcode` | `int?` | | | ? | Référence `TOLKIDENTITY.TOLKCODE` — **pas de FK EF configurée** |
| `DATECREATE` | `Datecreate` | `DateTime` | | | ? | |
| `DATEMODIF` | `Datemodif` | `DateTime?` | | | ? | |
| `DATESUPP` | `Datesupp` | `DateTime?` | | | ? | **Soft delete** — `null` = lien actif. Renseigné pour marquage absence/suppression |
| `USERCREATE` | `Usercreate` | `string?` | | | ? | max 100 caractères |
| `ID_PRESTATION` | `IdPrestation` | `int?` | | ? FK | ? | ? `PRESTATION.ID_PRESTATION`. `null` avant encodage de la prestation |

**Relation EF configurée** :
```
Tolklink (many) ? Prestation (one)
FK : IdPrestation
OnDelete : NoAction
Contrainte : FK_TOLKLINK_PRESTATION
```

**?? NR_AFF_AUDIENCE sans FK** : pas de contrainte référentielle vers les vues Oracle du système tiers. L'intégrité est assurée uniquement par la logique applicative.

---

### 2.10 PRESTATION

**Entité C#** : `Prestation` | **Table Oracle** : `PRESTATION`

| Colonne Oracle | Propriété C# | Type C# | PK | FK | Nullable | Notes |
|---|---|---|---|---|---|---|
| `ID_PRESTATION` | `IdPrestation` | `int` | ? | | ? | Séquence `ID_PRESTATION_AUTO.NEXTVAL` |
| `TOLKCODE` | `Tolkcode` | `string` | | | ? | **?? TYPE STRING** — cohérent avec PAIEMENT mais pas avec TOLKIDENTITY (int) |
| `DATE_PRESTATION` | `DatePrestation` | `DateTime` | | | ? | Type Oracle `DATE` |
| `STARTHEURE` | `Startheure` | `DateTime` | | | ? | Type Oracle `TIMESTAMP(6)` — heure de début |
| `ENDHEURE` | `Endheure` | `DateTime` | | | ? | Type Oracle `TIMESTAMP(6)` — heure de fin |
| `USER_CREATE` | `UserCreate` | `string?` | | | ? | max 50 caractères |
| `ID_PAIEMENT` | `IdPaiement` | `int` | | ? FK | ? | ? `PAIEMENT.ID_PAIEMENT`. **Non nullable** — une prestation est toujours associée à un paiement |

**Relation EF configurée** :
```
Prestation (many) ? Paiement (one)
FK : IdPaiement
OnDelete : NoAction
Contrainte : FK_PRESTATION_PAIEMENT
Navigation : IdPaiementNavigation (Paiement?)
```

**Collection inverse** : `Tolklinks` (`ICollection<Tolklink>`) — liste des TOLKLINK liés à cette prestation.

---

### 2.11 PAIEMENT

**Entité C#** : `Paiement` | **Table Oracle** : `PAIEMENT`

| Colonne Oracle | Propriété C# | Type C# | PK | FK | Nullable | Notes |
|---|---|---|---|---|---|---|
| `ID_PAIEMENT` | `IdPaiement` | `int` | ? | | ? | Séquence `NR_AUTO_PAIEMENT.NEXTVAL` |
| `TOLKCODE` | `Tolkcode` | `string` | | | ? | **?? TYPE STRING** max 5 — incohérence avec TOLKIDENTITY |
| `DATE_PRESTATION` | `DatePrestation` | `DateTime` | | | ? | Type Oracle `DATE` |
| `MONTANT` | `Montant` | `decimal?` | | | ? | `NUMBER(10,2)` — montant prestation HT |
| `TRANSPORT` | `Transport` | `decimal?` | | | ? | `NUMBER(10,2)` — frais de déplacement HT |
| `TOTAL` | `Total` | `decimal?` | | | ? | `NUMBER(10,2)` — total HT (Montant + Transport) |
| `MONTANT_TVA` | `MontantTva` | `decimal?` | | | ? | `NUMBER(10,2)` — TVA calculée (21% si assujetti) |
| `ID_FACTURE` | `IdFacture` | `int?` | | ? FK | ? | ? `FACTURE.ID_FACTURE`. `null` = paiement non encore facturé |

**Colonnes shadow (non mappées en propriété C#)** — déclarées uniquement dans `OnModelCreating` :
| Colonne Oracle | Type C# shadow | Nullable | Usage |
|---|---|---|---|
| `DATE_SIGNEE` | `DateTime?` | ? | Date de signature (legacy, non affiché) |
| `DATE_TRANSMISSION` | `DateTime?` | ? | Legacy (date de transmission directe sur PAIEMENT, remplacée par `FACTURE.DATE_TRANSMISSION`) |
| `DATE_PAIEMENT` | `DateTime?` | ? | Date de paiement effectif |
| `ID_INDEX` | `decimal?` | ? | Référence barème INDEXATION utilisé lors du calcul |
| `PRESTATION_TVA` | `decimal?` | ? | TVA sur la prestation uniquement |
| `TRANSPORT_TVA` | `decimal?` | ? | TVA sur le transport uniquement |

**Relation EF** : navigation `Facture` (via `WithOne(p => p.Facture)`).

---

### 2.12 FACTURE

**Entité C#** : `Facture` | **Table Oracle** : `FACTURE`

| Colonne Oracle | Propriété C# | Type C# | PK | Nullable | Défaut | Notes |
|---|---|---|---|---|---|---|
| `ID_FACTURE` | `IdFacture` | `int` | ? | ? | `NR_AUTO_FACTURE.NEXTVAL` | Séquence non déclarée via `HasSequence` dans `ApplicationDbContext` |
| `TOLKCODE` | `Tolkcode` | `string` | | ? | — | **?? TYPE STRING** — incohérence avec TOLKIDENTITY |
| `DATE_GENERATION` | `DateGeneration` | `DateTime` | | ? | `SYSDATE` | Date de génération Oracle automatique |
| `DATE_VALIDATION_FEDCOM` | `DateValidationFedcom` | `DateTime?` | | ? | — | Renseignée lors du passage en statut `APPROUVEE` |
| `DATE_TRANSMISSION` | `DateTransmission` | `DateTime?` | | ? | — | Renseignée lors de la transmission .eml à l'interprète |
| `STATUT_FACTURE` | `StatutFacture` | `string` | | ? | `"GENEREE"` | Valeurs: `GENEREE`, `APPROUVEE`, `ANNULEE`, `NOTE DE CREDIT`, `CREDIT VALIDE`, `TRANSMISE` |
| `TOTAL_TTC` | `TotalTtc` | `decimal` | | ? | `0` | `NUMBER(12,2)` — somme des paiements TTC |
| `ID_FACTURE_ORIGINE` | `IdFactureOrigine` | `int?` | | ? | — | Référence la facture annulée (auto-référence pour notes de crédit). **Pas de FK EF configurée** |

**Relation EF configurée** :
```
Facture (one) ? Paiement (many)
FK : IdFacture (sur PAIEMENT)
OnDelete : NoAction
Contrainte : FK_PAIEMENT_FACTURE
Collection : Paiements (ICollection<Paiement>)
```

**?? Auto-référence non configurée** : `IdFactureOrigine` ? `FACTURE.ID_FACTURE` n'est pas déclarée comme relation EF. Consultée par SQL brut dans `FacturesController`.

---

### 2.13 INDEXATION

**Entité C#** : `Indexation` | **Table Oracle** : `INDEXATION`

| Colonne Oracle | Propriété C# | Type C# | PK | Nullable | Notes |
|---|---|---|---|---|---|
| `ID_INDEX` | `IdIndex` | `int` | ? | ? | |
| `STARTDATE` | `Startdate` | `DateTime` | | ? | Début de validité du barème |
| `ENDDATE` | `Enddate` | `DateTime?` | | ? | Fin — `null` = barème actif courant |
| `EURO75MIN` | `Euro75min` | `decimal` | | ? | Tarif minimum 75 minutes (ex: 31.52 €) |
| `EUROHEURE` | `Euroheure` | `decimal` | | ? | Tarif par heure au-delà des 75 min |
| `EUROKM` | `Eurokm` | `decimal` | | ? | Tarif par kilomètre de déplacement |

**Utilisée en lecture seule** dans `PrestationsController` pour calculer les montants. Jamais modifiée via l'API.

---

## 3. Vues (Keyless)

### 3.1 VUE_CALENDAR_ALL ? `VueCalendarVrmPc`

Vue du système tiers CCE. Audiences VRM/PCS courantes.

| Colonne Oracle | Propriété C# | Type C# | Nullable | Notes |
|---|---|---|---|---|
| `ID_AFF_AUDIENCE` | `IdAffAudience` | `decimal` | ? | Identifiant audience — `decimal` car NUMBER Oracle sans précision |
| `DATE_AUDIENCE` | `DateAudience` | `DateTime?` | ? | |
| `HEURE_AUDIENCE` | `HeureAudience` | `string?` | ? | Format HH:mm |
| `SALLE_AUDIENCE` | `SalleAudience` | `string?` | ? | |
| `LANGUE_ROLE` | `LangueRole` | `string?` | ? | Langue du magistrat (`F`=FR, `N`=NL) |
| `LANGUE_REQUETE` | `LangueRequete` | `string?` | ? | Langue demandée pour l'interprète. Préfixé `*` si aucun interprète requis |
| `TOLKCODE` | `Tolkcode` | `decimal?` | ? | Interprète assigné — `null` si non assigné |
| `NRO_ROLE_GEN` | `NroRoleGen` | `decimal` | ? | Numéro de dossier/affaire |
| `PROC` | `Proc` | `string?` | ? | Code procédure |
| `NOM` | `Nom` | `string` | ? | Nom du requérant |
| `LIBELLE_FR` | `LibelleFr` | `string?` | ? | Libellé FR de la langue |
| `LANGUE_CGOE` | `LangueCgoe` | `string?` | ? | Code CGOE interne |

**?? `IdAffAudience` en `decimal`** alors que `TOLKLINK.NrAffAudience` est `int?`. Conversion explicite nécessaire dans les contrôleurs pour les jointures.

---

### 3.2 VUE_CALENDAR_ANN ? `VueCalendarAnn`

Même structure que `VUE_CALENDAR_ALL`. Audiences en appel/annulation.

---

### 3.3 V_INTERPRETES_AUDIENCES_JOUR ? `ReportInterpreteRow`

Vue utilisée pour les rapports de présence journalière.

| Colonne Oracle | Propriété C# | Type C# | Notes |
|---|---|---|---|
| `TOLKCODE` | `Tolkcode` | `int?` | |
| `NOM` | `Nom` | `string?` | |
| `PRENOM` | `Prenom` | `string?` | |
| `JOUR` | `Jour` | `DateTime?` | Date de l'audience |
| `HEURE_AUDIENCE` | `HeureAudience` | `string?` | |
| `SALLE_AUDIENCE` | `SalleAudience` | `string?` | |
| `LANGUE_REQUETE` | `LangueRequete` | `string?` | |
| `GSM` | `Gsm` | `string?` | |
| `TEL` | `Tel` | `string?` | |
| `TELBIS` | `Telbis` | `string?` | |
| `TAALROL` | `Taalrol` | `int?` | 1=NL, 2=FR |

**?? Doublon** : `ReportInterpreteRow` et `VAudienceInterpreteDetail` mappent **la même vue Oracle** (`V_AUDIENCE_INTERPRETE_DETAIL` / `V_INTERPRETES_AUDIENCES_JOUR`) mais avec des noms de colonnes différents dans le mapping. `ReportInterpreteRow` utilise les colonnes en MAJUSCULES, `VAudienceInterpreteDetail` en casse mixte.

---

### 3.4 V_AUDIENCE_INTERPRETE_DETAIL ? `VAudienceInterpreteDetail`

Vue détaillée pour Dashboard et présence. Casse mixte des colonnes (spécificité Oracle).

| Colonne Oracle | Propriété C# | Casse mappée | Notes |
|---|---|---|---|
| `Tolkcode` | `Tolkcode` | mixte | |
| `Nom` | `Nom` | mixte | |
| `Prenom` | `Prenom` | mixte | |
| `Jour` | `Jour` | mixte | |
| `HeureAudience` | `HeureAudience` | mixte | |
| `SalleAudience` | `SalleAudience` | mixte | |
| `LangueRequete` | `LangueRequete` | mixte | |
| `Gsm` | `Gsm` | mixte | |
| `Tel` | `Tel` | mixte | |
| `Telbis` | `Telbis` | mixte | |
| `TAALROL` | `Taalrol` | **MAJUSCULES** | Incohérence dans la vue Oracle elle-même |

---

## 4. Relations FK configurées dans `OnModelCreating`

```
TOLKLINK ??(FK: IdPrestation, nullable)??? PRESTATION
    Relation  : HasOne<Prestation>().WithMany(p => p.Tolklinks)
    Clé       : TOLKLINK.ID_PRESTATION ? PRESTATION.ID_PRESTATION
    Delete    : NoAction
    Contrainte: FK_TOLKLINK_PRESTATION

PRESTATION ??(FK: IdPaiement, non nullable)??? PAIEMENT
    Relation  : HasOne(p => p.IdPaiementNavigation).WithMany()
    Clé       : PRESTATION.ID_PAIEMENT ? PAIEMENT.ID_PAIEMENT
    Delete    : NoAction
    Contrainte: FK_PRESTATION_PAIEMENT

PAIEMENT ??(FK: IdFacture, nullable)??? FACTURE
    Relation  : HasMany(e => e.Paiements).WithOne(p => p.Facture)
    Clé       : PAIEMENT.ID_FACTURE ? FACTURE.ID_FACTURE
    Delete    : NoAction
    Contrainte: FK_PAIEMENT_FACTURE
```

---

## 5. Séquences Oracle

| Séquence Oracle | Déclarée `HasSequence` | Utilisée via `HasDefaultValueSql` | Entité cible |
|---|---|---|---|
| `ID_PRESTATION_AUTO` | ? | ? | `PRESTATION.ID_PRESTATION` |
| `NR_AUTO_PAIEMENT` | ? | ? | `PAIEMENT.ID_PAIEMENT` |
| `NR_AUTO_TOLKLINK` | ? | ? | `TOLKLINK.ID_TOLKLINK` |
| `NR_AUTO_ADRESSE` | ? | ? | `TOLKADRESSE.ID_ADRESSE` |
| `NR_AUTO_FACTURE` | ? **manquante** | ? | `FACTURE.ID_FACTURE` |
| `NR_TOLK` | ? non déclarée | ? | `TOLKIDENTITY.TOLKCODE` — appelée via `SELECT NEXTVAL FROM DUAL` dans controller |

**?? `NR_AUTO_FACTURE` non déclarée** : utilisée dans `HasDefaultValueSql("NR_AUTO_FACTURE.NEXTVAL")` mais pas dans `modelBuilder.HasSequence<int>("NR_AUTO_FACTURE")`. EF Core ne connaît pas cette séquence pour les migrations.

---

## 6. Convertisseur global — `bool` ? `NUMBER(1)`

Appliqué automatiquement à **toutes** les propriétés `bool` et `bool?` de toutes les entités :

```csharp
new ValueConverter<bool?, int>(
    v => v.HasValue && v.Value ? 1 : 0,   // C# ? Oracle
    v => v == 1                            // Oracle ? C#
)
```

Seule propriété affectée : `Langue.IslangueDestination` (`bool?`).

---

## 7. Méthodes utilitaires sur le contexte

Le `ApplicationDbContext` expose 3 méthodes métier qui encapsulent des chaînes de requêtes liées aux annulations/notes de crédit :

```
GetFacturesAnnulees(ct)
    ? List<int> des IdFacture dont StatutFacture IN ("ANNULEE", "NOTE DE CREDIT", "CREDIT VALIDE")

GetPaiementsAnnules(ct)
    ? List<int> des IdPaiement liés à une facture annulée
      (appelle GetFacturesAnnulees en interne)

GetPrestationsAnnulees(ct)
    ? List<int> des IdPrestation liées à un paiement annulé
      (appelle GetPaiementsAnnules en interne)
```

**?? N+1 potentiel** : ces méthodes exécutent 2 ou 3 requêtes séquentielles (non parallèles) avec `await`. Pour de grands volumes, un `JOIN` SQL unique serait plus performant.

---

## 8. Synthèse des anomalies de typage

| Colonne | Table | Type C# | Type Oracle attendu | Impact |
|---|---|---|---|---|
| `TOLKCODE` | TOLKADRESSE | `string` | `NUMBER(5)` | Jointure avec TOLKIDENTITY impossible via EF — SQL brut obligatoire |
| `TOLKCODE` | TOLKINDISPO | `string` | `NUMBER(5)` | Idem |
| `TOLKCODE` | PRESTATION | `string` | `NUMBER(5)` | Idem |
| `TOLKCODE` | PAIEMENT | `string` | `NUMBER(5)` | Idem |
| `TOLKCODE` | FACTURE | `string` | `NUMBER(5)` | Idem |
| `ID_INDISPO` | TOLKINDISPO | `short` | `NUMBER(5)` | Limite à 32 767 enregistrements |
| `TOLKCODE` | LANGUE_DESTINATION | `int?` | — | Nullable vs non-nullable dans LANGUE_SOURCE |
| `ID_AFF_AUDIENCE` | VUE_CALENDAR_* | `decimal` | — | Conversion vers `int` nécessaire pour jointure avec TOLKLINK |
| `NR_LANGUE` | LANGUE_SOURCE | `int?` | — | Type cible `LANGUE.IDLANGUE` est `byte` |
