# Dragoman — Description technique et métier

---

## 1. Contexte métier

Le Conseil du Contentieux des Étrangers (CCE / RvV — *Raad voor Vreemdelingenbetwistingen*) est une juridiction administrative fédérale belge rattachée au SPF Intérieur (IBZ). Elle traite les recours en matière de droit des étrangers.

Pour instruire ses affaires, le CCE recourt à des interprètes judiciaires freelances assermentés ou non, qui interviennent lors des audiences pour assurer la traduction entre le magistrat (en français ou en néerlandais) et la partie étrangère.

Avant Dragoman, la gestion de ces interprètes — recherche de disponibilités, assignation aux audiences, suivi des prestations et facturation — reposait sur des processus manuels (tableurs, emails, documents Word) avec les risques associés : erreurs de double assignation, délais de paiement, absence de traçabilité.

---

## 2. Objectif principal

Dragoman est une application web interne de gestion opérationnelle des interprètes judiciaires du CCE. Elle couvre l'intégralité du cycle de vie d'une prestation d'interprétariat :

```
Audience programmée → Identification de l'interprète → Assignation
→ Encodage de la prestation → Calcul automatique de la rémunération
→ Génération de la facture → Transmission à l'interprète → Validation Fedcom
```

Le système est à usage exclusif des agents administratifs du CCE (pas d'accès interprètes).

---

## 3. Problèmes résolus

| Problème | Solution apportée |
|---|---|
| Trouver manuellement un interprète disponible parlant la bonne langue | Recherche par paire langue source/destination avec filtre indisponibilités |
| Risque de double assignation d'un interprète | Contrainte d'unicité sur TOLKLINK + vérification avant insertion |
| Calcul manuel des montants (durée, km, TVA) | Calcul automatique selon barème INDEXATION actif + règle 75 min minimum |
| Génération de factures chronophage | Génération groupée par interprète sur une période, avec PDF prêt à envoyer |
| Absence de traçabilité des transmissions | Statuts facture tracés (GENEREE → APPROUVEE → ANNULEE), date de transmission enregistrée |
| Gestion des annulations et notes de crédit | Workflow automatisé : annulation facture = création note de crédit à montants négatifs |
| Feuilles de présence journalières en format papier | Export Excel / Word / PDF de la présence interprètes par jour |
| Manque de visibilité sur le calendrier des audiences | Vue calendrier filtrée sur les données Oracle existantes |

---

## 4. Public cible

Exclusivement interne au CCE / IBZ :

- **Agents administratifs** chargés de la planification des interprètes (assignation, présence, prestations)
- **Service comptabilité / gestionnaires** chargés de la validation et transmission des factures via Fedcom
- **Équipe IT IBZ** (accès aux modules AD Status et GlobalProtect — restreint par rôle AD)

L'application n'est pas accessible aux interprètes eux-mêmes.

---

## 5. Fonctionnalités majeures

### 5.1 Gestion des interprètes

- **Fiche identité complète** : nom, prénom, date de naissance, numéro de registre national, genre, nationalité, statut assermenté (BEEDIGD), langue de rôle (FR/NL)
- **Coordonnées** : email, GSM, téléphone fixe, téléphone bis — avec normalisation et dédoublonnage automatique
- **Adresses historisées** : chaque adresse a une période de validité (STARTDATE / ENDDATE). La distance kilométrique (KM) associée détermine le montant du transport dans la facture
- **Langues source et destination** : gestion multi-langues par interprète, avec référentiel centralisé
- **Statut TVA historisé** : assujetti ou non sur des périodes, ce qui impacte directement le calcul de la TVA à 21 %
- **Indisponibilités** : saisie de périodes avec vérification d'anti-chevauchement et clôture automatique de la période précédente ouverte
- **Création d'un nouvel interprète** : avec validation format téléphone belge et numéro TVA (BE + 10 chiffres), allocation du code via séquence Oracle `NR_TOLK`

### 5.2 Calendrier des audiences

- Visualisation de toutes les audiences issues des vues Oracle `VUE_CALENDAR_ALL` et `VUE_CALENDAR_ANN`
- Filtres multi-critères en temps réel : date, heure, salle, langue rôle, langue requête, magistrat, tolkcode, numéro d'affaire
- Filtres rapides : "sans interprète assigné" / "exclure les audiences sans interprète demandé"
- Assignation directe depuis le calendrier (modal avec recherche par nom/tolkcode)
- Désassignation (soft delete via DATESUPP)
- Navigation directe vers la fiche interprète

### 5.3 Assignation aux audiences

Deux modes :
- **Depuis la fiche interprète** : liste des audiences compatibles calculée dynamiquement (langues source/destination de l'interprète vs langue de l'audience, filtre indisponibilités)
- **Depuis le calendrier** : sélection manuelle d'un interprète dans la liste complète

Assignation unitaire ou en masse (bulk). Vérification de doublon avant insertion.

### 5.4 Présence journalière

- Vue synthétique des interprètes assignés pour un jour donné
- Statut par interprète : avec prestation / sans prestation / absent
- Marquage absence (DATESUPP sur TOLKLINK)
- Remplacement d'un interprète par un autre sur une audience
- **Export tri-format** : Excel (ClosedXML), Word (OOXML SDK — 1 page par audience + synthèse), PDF (QuestPDF, A4 paysage)

### 5.5 Encodage des prestations

- Saisie des heures de début et de fin pour chaque interprète du jour
- Pré-remplissage automatique de l'heure suggérée (heure d'audience - 15 min)
- **Calcul automatique** à la validation :
  - Barème actif depuis `INDEXATION` (EURO75MIN, EUROHEURE, EUROKM)
  - Durée arrondie au quart d'heure supérieur
  - Minimum facturable : 75 minutes
  - Transport : `min(100 km, 2 × KM_adresse) × EUROKM`, payé une seule fois par jour pour un même interprète
  - TVA 21 % si interprète assujetti à la date de la prestation
- Les TOLKLINK concernés sont liés à la PRESTATION créée

### 5.6 Facturation

- **Génération** : regroupe les paiements non encore facturés par interprète sur une période (mois ou plage libre). Crée une `FACTURE` par interprète et lie les `PAIEMENT` correspondants
- **Workflow de statut** : `GENEREE` → `APPROUVEE` (validation Fedcom, date enregistrée) → `ANNULEE` (création automatique d'une `NOTE DE CREDIT` à montants négatifs, libération des Tolklinks, suppression des paiements/prestations originaux)
- **PDF** : génération d'un document A4 par facture (ou note de crédit), bilingue FR/NL selon le `TAALROL` de l'interprète. Contient : coordonnées fournisseur/client, tableau des prestations (date, heure début/fin, durée, km, montant, transport), totaux HT/TVA/TTC, numéro de PO, numéro Fedcom, IBAN
- **Transmission** : génération d'un fichier `.eml` (RFC 2822) avec le PDF en pièce jointe, préadressé à l'email de l'interprète. La date de transmission est enregistrée en base
- **Historique** : filtrage par mois, statut, tolkcode. Total TTC calculé côté client

### 5.7 Tableau de bord

Statistiques temps réel du jour : nombre d'audiences, nombre d'interprètes assignés, répartition des langues demandées. Source : vues Oracle (lecture seule).

### 5.8 Modules annexes IT

- **Fiche Helpdesk (HD)** : saisie des tickets et tâches journalières pour les agents IT, stockage JSON sur le serveur (aucune base de données), export Word hebdomadaire
- **AD Status** : tableau de bord des statuts de comptes Active Directory (mot de passe expiré, inactivité), alimenté par un CSV PowerShell. Permet d'annoter et de marquer les comptes comme "normaux". Accès restreint au rôle `gg_rol_SystemAdministrator`
- **GlobalProtect Inventory** : inventaire des machines avec client VPN Palo Alto, alimenté par import CSV PowerShell. Merge avec l'état précédent (préserve les annotations manuelles)

---

## 6. Composants Angular — Inventaire complet

### 6.1 Structure générale

```
AppComponent                  → racine (shell, routing outlet)
NavbarComponent               → barre de navigation principale (tous les modules)
NavbarInterComponent          → barre de navigation secondaire (sous-pages d'un interprète)
```

### 6.2 Composants métier — Interprètes

| Composant | Sélecteur | Route | Rôle |
|---|---|---|---|
| `InterpreteListComponent` | `app-interprete-list` | `/interpretes` | Recherche rapide (nom/tolkcode) + recherche avancée (paire de langues + date de disponibilité) + formulaire de création d'un nouvel interprète |
| `InterpreteDetailComponent` | `app-interprete-detail` | `/interpretes/:tolkcode/detail` | Fiche identité complète, formulaire accordéon en 6 sections (Identité, Contact, Langue & Statut, Banque & TVA, Entreprise, Divers). Gestion des 3 numéros de téléphone avec normalisation GSM/fixe/bis automatique à la sauvegarde |
| `AdressesComponent` | `app-adresses` | `/interpretes/:tolkcode/adresses` | Liste des adresses historisées. Création via logique "replace" (clôture l'active + crée la nouvelle). Édition directe d'une ligne existante. Filtre "actif uniquement" |
| `LanguesComponent` | `app-langues` | `/interpretes/:tolkcode/langues` | Gestion des langues source et destination. Référentiels chargés en parallèle (`forkJoin`). Filtre `destOnly` sur les langues destination |
| `TvaComponent` | `app-tva` | `/interpretes/:tolkcode/tva` | Historique des statuts TVA. Ajout d'un nouveau statut avec clôture automatique du précédent. Référentiel des statuts chargé à l'init |
| `IndispoComponent` | `app-indispo` | `/interpretes/:tolkcode/indispo` | Périodes d'indisponibilité. Validation croisée start/end dans le formulaire (`AbstractControl` validator). Calcul du nombre de jours affiché en lecture |
| `InterpreteAudiencesComponent` | `app-interprete-audiences` | `/interpretes/:tolkcode/audiences` | Liste des audiences compatibles non assignées. Détection des audiences "jumelles" (même jour/heure/langue) pour proposer une assignation bulk automatique |
| `ConvocationComponent` | `app-convocation` | `/interpretes/:tolkcode/convocation` | Génération de la convocation email. Combine audiences assignées (validées) + audiences disponibles sélectionnables. Construction d'un email HTML bilingue FR/NL avec tableaux stylés. Copie dans le presse-papier via `ClipboardItem` API (fallback `execCommand`). Ouvre le client email via `mailto:` |

### 6.3 Composants métier — Planification et présence

| Composant | Sélecteur | Route | Rôle |
|---|---|---|---|
| `CalendarComponent` | `app-calendar` | `/calendar` | Calendrier global des audiences (VUE_CALENDAR_ALL + ANN). Filtres multi-critères réactifs via `combineLatest` + `BehaviorSubject`. Modal d'assignation directe (recherche parmi tous les interprètes). Désassignation avec confirmation. Navigation vers la fiche interprète |
| `PresenceInterpretesComponent` | `app-presence-interpretes` | `/presence-interpretes` | Présence journalière des interprètes. Vue "flat" (1 ligne par couple interprète × audience). Affichage des magistrats. Téléphone cliquable en protocole `tel:` normalisé format international `+32`. Exports Excel / Word / PDF via téléchargement blob |
| `PrestationsComponent` | `app-prestations` | `/prestations` | Encodage des prestations du jour. Liste les interprètes assignés (vues calendrier + TOLKLINK). Sélection d'un interprète → formulaire heure début/fin avec pré-remplissage suggéré (heure audience − 15 min). Marquage absence. Scroll automatique vers le formulaire (`scrollIntoView`) |

### 6.4 Composants métier — Facturation

| Composant | Sélecteur | Route | Rôle |
|---|---|---|---|
| `FacturesComponent` | `app-factures` | `/factures` | Récapitulatif mensuel des paiements par interprète (avant facturation). Sélection d'un interprète → détail des lignes. Suppression d'un paiement non facturé. Téléchargement PDF mensuel brut (via `PaiementsController`) |
| `GenerationFacturesComponent` | `app-generation-factures` | `/generation-factures` | 3 onglets : **Générer** (par mois ou période libre) / **Enregistrer** (approbation, annulation, transmission .eml par facture) / **Historique** (filtrage par mois, statut, tolkcode). Gestion des notes de crédit inline. Total TTC calculé côté client |

### 6.5 Composants — Tableau de bord

| Composant | Sélecteur | Route | Rôle |
|---|---|---|---|
| `DashboardComponent` | `app-dashboard` | `/dashboard` | Page d'accueil. Compteurs du jour (audiences, interprètes, langues distinctes). Liste des audiences du jour regroupées par (heure, salle, langue) avec les interprètes assignés. Identification de l'utilisateur courant via `UserService` |

### 6.6 Composants — Modules IT

| Composant | Sélecteur | Route | Rôle |
|---|---|---|---|
| `HDComponent` | `app-hd` | `/hd` | Page d'accueil Helpdesk — liens vers Fiche jour et Récap semaine. Composant standalone (`CommonModule` + `RouterModule`) |
| `HDPrestationJourComponent` | `app-hd-prestation-jour` | `/hd/fiche-jour` | Fiche journalière Helpdesk. `FormArray` pour tickets (heure, N°, type, durée min) et autres tâches (dénomination, durée min). Rechargement automatique au changement de date. Nettoyage des lignes vides avant sauvegarde. Export JSON local. Composant **standalone** + lazy loaded |
| `HDRecapSemaineComponent` | `app-hd-recap-semaine` | `/hd/recap-semaine` | Récapitulatif hebdomadaire Helpdesk. Sélecteur `<input type="week">` (valeur ISO `YYYY-Www`). Calcul total minutes tickets/autres/général. Accordéon par jour. Export Word hebdomadaire. Composant **standalone** + lazy loaded |
| `AdStatusDashboardComponent` | `app-ad-status-dashboard` | `/ad-status` | Tableau de bord AD. 5 catégories d'alertes (expiré, bientôt expiré ×3 seuils, inactif 90j+, bientôt inactif). Tri multi-colonnes. Filtre "masquer les situations normales". Marquage IsNormal + commentaire persistés en JSON via API. Ouverture chat Teams. Accès restreint rôle AD |
| `InventoryComponent` | `app-inventory` | `/globalprotect` | Inventaire GlobalProtect. Import CSV PowerShell via `<input type="file">` (import automatique au changement). Filtres : texte global, localisation, version exacte, vérifié/non vérifié. Stats résumées (total, avec/sans GP, bureau, TT, injoignable, % à jour). Export CSV filtré avec BOM UTF-8. Mise à jour VerifiedByTeam/Remark persistée à la perte de focus |

### 6.7 Composants partagés

| Composant | Sélecteur | Portée | Rôle |
|---|---|---|---|
| `NavbarComponent` | `app-navbar` | Global | Barre de navigation principale. Affiche le login Windows courant (via `AuthentificationService`). Liens vers tous les modules |
| `NavbarInterComponent` | `app-navbarinter` | Pages interprète | Barre secondaire présente sur les 7 sous-pages d'un interprète. Avatar avec initiales, nom complet, email cliquable (`mailto:`), téléphone via protocole `msteams://call?phone=`. Langues source/destination en badges. Charge les données via `@Input() tolkcode` ou depuis les `paramMap` de la route |

### 6.8 Services Angular

| Service | Fichier | Endpoints consommés |
|---|---|---|
| `InterpretesService` | `interpretes.service.ts` | `GET/POST/PUT/DELETE /api/interpretes`, `/search`, `/match`, `/audiences-exact`, `/convocations`, `/tolkcodes`, `/tolklink` |
| `TolklinkService` | `tolklink.service.ts` | `POST/DELETE /api/interpretes/{tolkcode}/tolklink`, `/bulk` |
| `AdressesService` | `adresses.service.ts` | `GET/POST/PUT/DELETE /api/interpretes/{tolkcode}/adresses`, `/replace` |
| `LanguesService` | `langues.service.ts` | `GET /api/langues`, `GET/POST/DELETE /api/interpretes/{tolkcode}/langues/sources` et `/destination` |
| `TvaService` | `tva.service.ts` | `GET /api/tva/statuts`, `GET/POST /api/interpretes/{tolkcode}/tva` |
| `IndispoService` | `indispo.service.ts` | `GET/POST/DELETE /api/interpretes/{tolkcode}/indispo` |
| `CalendarService` | `calendar.service.ts` | `GET /api/calendar` (VUE_CALENDAR_ALL + ANN) |
| `PrestationsService` | `prestations.service.ts` | `GET /api/prestations/jour`, `POST /api/prestations`, `/absence`, `/remplacement` |
| `PaiementsService` | `paiements.service.ts` | `GET /api/paiements/mois`, `/mois/{tolkcode}`, `/mois/pdf`, `DELETE /api/paiements/{id}` |
| `FacturesGenService` | `factures.service.ts` | `GET/POST /api/factures`, `/generer`, `PATCH /{id}/statut`, `PATCH /{id}/transmettre`, `/pdf`, `/{id}/eml` |
| `ReportsService` | `reports.service.ts` | `GET /api/reports/interpretes`, `/excel`, `/word`, `/pdf` |
| `DashboardService` | `dashboard.service.ts` | `GET /api/dashboard/audiences/today`, `/count-today`, `/interpretes/count-today`, `/langues/today`, `/audiences-supprimees/today`, `/detail-today` |
| `UserService` | `user.service.ts` | `GET /api/user/current`, `POST /api/user/addUser` |
| `AuthentificationService` | `authentification.service.ts` | `GET /api/auth/whoami` |
| `AdStatusService` | `ad-status.service.ts` | `GET /api/adstatus`, `POST /api/adstatus/comment`, `/normalstatus` |
| `InventoryService` | `inventory.service.ts` | `GET /api/inventory`, `POST /api/inventory/import`, `PUT /api/inventory/{computerName}` |
| `HDPrestationsService` | `hd-prestations.service.ts` | `GET/POST /api/hd-prestations/jour`, `GET/PUT /api/hd-prestations/semaine`, `GET /api/hd-prestations/semaine/export/word` |

---

## 7. Limites actuelles

### Techniques

- **Pas d'authentification au niveau API** : l'authentification Windows (Negotiate/IIS) est configurée mais n'est pas vérifiée sur la majorité des endpoints (seul `AdStatusController` vérifie le rôle). N'importe quel utilisateur authentifié sur le réseau peut appeler tous les endpoints
- **Clé étrangère TOLKCODE incohérente** : dans certaines tables (`TOLKINDISPO`, `TOLKADRESSE`), `TOLKCODE` est stocké en `VARCHAR2` au lieu de `NUMBER`, ce qui implique des conversions explicites et des jointures manuelles dans le code
- **Pas de relation FK physique** entre `TOLKLINK.NR_AFF_AUDIENCE` et les vues `VUE_CALENDAR_ALL` / `VUE_CALENDAR_ANN` — la cohérence est assurée uniquement par la logique applicative
- **Données des audiences en lecture seule** : les vues Oracle (`VUE_CALENDAR_ALL`, `VUE_CALENDAR_ANN`, `V_AUDIENCE_INTERPRETE_DETAIL`) sont des vues d'un système tiers (logiciel métier du CCE). Dragoman ne peut pas modifier le calendrier des audiences
- **Pas de pagination côté frontend** sur le calendrier et la liste des interprètes — tous les résultats sont chargés en mémoire
- **Pas de gestion des conflits d'assignation d'audience** : un interprète peut théoriquement être assigné à deux audiences simultanées si elles proviennent de vues différentes (VRM vs ANN)
- **Module HD et Inventory sans base de données** : les données sont stockées en fichiers JSON sur le serveur, sans verrouillage concurrent ni sauvegarde automatisée
- **Calcul de la distance KM** : la distance en km est saisie manuellement dans l'adresse. Il n'y a pas d'intégration avec un service de géolocalisation
- **Pas de tests automatisés** : aucun test unitaire ni test d'intégration identifié dans le projet

### Métier
- **Pas de notification automatique** aux interprètes lors d'une assignation (le fichier `.eml` est téléchargé et envoyé manuellement par l'agent)
- **Pas de portail interprète** : les interprètes ne peuvent pas consulter leur planning, leurs prestations ni leurs factures
- **Pas de gestion des remplacements avec recalcul** : le remplacement d'un interprète (`POST /prestations/remplacement`) change uniquement le TOLKCODE dans TOLKLINK — il ne crée pas de nouvelle prestation ni de nouveau paiement pour le remplaçant
- **Pas de gestion multi-sessions** d'audience : si un interprète intervient à la même audience à deux créneaux horaires distincts dans la même journée, la règle "transport 1 fois/jour" peut conduire à une sous-facturation

---

## 8. Hypothèses techniques


| Hypothèse | Détail |
|---|---|
| Réseau interne IBZ uniquement | L'application n'est pas exposée sur Internet. Elle suppose un réseau local IBZ avec Active Directory |
| Oracle Database existante | La base Oracle (`LAURENTIDE`, user `DRAGOMAN`) est une base existante partagée avec le logiciel métier du CCE. Les vues `VUE_CALENDAR_*` sont produites par ce logiciel tiers |
| IIS comme serveur web | Le déploiement cible IIS avec Windows Authentication. Le frontend Angular est servi statiquement via IIS |
| Un seul utilisateur actif à la fois par session | Pas de gestion de concurrence sur les fichiers JSON (HD, Inventory, AD Status) |
| La règle 75 min minimum est constante | Le minimum facturable est codé en dur comme 75 minutes dans `PrestationsController`. Seuls EUROHEURE, EUROKM et EURO75MIN varient via l'indexation |
| TVA à 21 % fixe | Le taux de TVA est une constante dans `PrestationsController` (`TVA_RATE = 0.21m`). Aucun autre taux n'est prévu |
| Bilingue FR/NL uniquement | La langue de facturation est déterminée par `TAALROL` (1=NL, 2=FR). Aucune autre langue n'est supportée pour les documents générés |
| Un seul PO Fedcom | Le numéro de commande Fedcom (`4501133577`) est une valeur par défaut modifiable dans l'interface, mais unique pour toutes les factures d'un mois |
| Séquences Oracle gérées manuellement | EF Core ne gère pas nativement les séquences Oracle pour les clés primaires dans tous les cas. Les insertions critiques (PAIEMENT, PRESTATION dans les notes de crédit) utilisent des commandes SQL raw |

---

## 9. Dépendances externes


| Dépendance | Usage |
|---|---|
| Oracle Database (ODP.NET) | Base de données principale + vues calendrier système tiers |
| Active Directory IBZ | Authentification Windows (Negotiate), contrôle d'accès rôle |
| Microsoft Teams | Protocole `msteams://call?phone=` pour appeler les interprètes depuis la navbarinter |
| QuestPDF | Génération PDF factures et rapports de présence |
| ClosedXML | Export Excel rapports de présence |
| DocumentFormat.OpenXml (OOXML SDK) | Export Word rapports de présence et fiches HD |
| AutoMapper | Mapping entités → DTOs (IndispoController, TvaController) |
