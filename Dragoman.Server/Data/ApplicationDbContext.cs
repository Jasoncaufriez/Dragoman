using Dragoman.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // ===== VUES (DbSet keyless)
    public DbSet<VueCalendarVrmPc> VueCalendarVrmPcs { get; set; } = null!;
    public DbSet<VueCalendarAnn> VueCalendarAnns { get; set; } = null!;
    public DbSet<ReportInterpreteRow> ReportInterpreteRows { get; set; } = null!;
    public DbSet<VAudienceInterpreteDetail> VAudienceInterpreteDetail { get; set; } = null!;

    // ===== TABLES
    public DbSet<Tolkidentity> Tolkidentities { get; set; } = null!;
    public DbSet<Tolkadresse> Tolkadresses { get; set; } = null!;
    public DbSet<Langue> Langues { get; set; } = null!;
    public DbSet<LangueSource> LangueSources { get; set; } = null!;
    public DbSet<LangueDestination> LangueDestinations { get; set; } = null!;
    public DbSet<TolkTva> TolkTvas { get; set; } = null!;
    public DbSet<Statut> Statuts { get; set; } = null!;
    public DbSet<Tolkindispo> Tolkindispos { get; set; } = null!;

    // ===== AJOUTS PRESTATIONS
    public DbSet<Tolklink> Tolklinks { get; set; } = null!;
    public DbSet<Prestation> Prestations { get; set; } = null!;
    public DbSet<Paiement> Paiements { get; set; } = null!;
        public DbSet<Facture> Factures { get; set; } = null!;
    
    // ===== INDEXATION (utilisée en lecture pour les tarifs)
    public DbSet<Indexation> Indexations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =========================
        // VUES (Keyless)
        // =========================
        modelBuilder.Entity<ReportInterpreteRow>()
            .ToView("V_INTERPRETES_AUDIENCES_JOUR")
            .HasNoKey();

        modelBuilder.Entity<VueCalendarVrmPc>(e =>
        {
            e.HasNoKey();
            e.ToView("VUE_CALENDAR_ALL");
            e.Property(x => x.IdAffAudience).HasColumnName("ID_AFF_AUDIENCE");
            e.Property(x => x.DateAudience).HasColumnName("DATE_AUDIENCE");
            e.Property(x => x.HeureAudience).HasColumnName("HEURE_AUDIENCE");
            e.Property(x => x.SalleAudience).HasColumnName("SALLE_AUDIENCE");
            e.Property(x => x.LangueRole).HasColumnName("LANGUE_ROLE");
            e.Property(x => x.LangueRequete).HasColumnName("LANGUE_REQUETE");
            e.Property(x => x.Tolkcode).HasColumnName("TOLKCODE");
            e.Property(x => x.NroRoleGen).HasColumnName("NRO_ROLE_GEN");
            e.Property(x => x.Proc).HasColumnName("PROC");
            e.Property(x => x.Nom).HasColumnName("NOM");
            e.Property(x => x.LibelleFr).HasColumnName("LIBELLE_FR");
            e.Property(x => x.LangueCgoe).HasColumnName("LANGUE_CGOE");
        });
        modelBuilder.Entity<VAudienceInterpreteDetail>(e =>
        {
            e.HasNoKey();
            e.ToView("V_AUDIENCE_INTERPRETE_DETAIL");

            // Mapping avec les noms EXACTS de la vue Oracle (casse mixte)
            e.Property(x => x.Tolkcode).HasColumnName("Tolkcode");
            e.Property(x => x.Nom).HasColumnName("Nom");
            e.Property(x => x.Prenom).HasColumnName("Prenom");
            e.Property(x => x.Jour).HasColumnName("Jour");
            e.Property(x => x.HeureAudience).HasColumnName("HeureAudience");
            e.Property(x => x.SalleAudience).HasColumnName("SalleAudience");
            e.Property(x => x.LangueRequete).HasColumnName("LangueRequete");
            e.Property(x => x.Gsm).HasColumnName("Gsm");
            e.Property(x => x.Tel).HasColumnName("Tel");
            e.Property(x => x.Telbis).HasColumnName("Telbis");
            e.Property(x => x.Taalrol).HasColumnName("TAALROL");  // Celui-ci est en MAJUSCULES
        });
        modelBuilder.Entity<VueCalendarAnn>(e =>
        {
            e.HasNoKey();
            e.ToView("VUE_CALENDAR_ANN");
            e.Property(x => x.IdAffAudience).HasColumnName("ID_AFF_AUDIENCE");
            e.Property(x => x.DateAudience).HasColumnName("DATE_AUDIENCE");
            e.Property(x => x.HeureAudience).HasColumnName("HEURE_AUDIENCE");
            e.Property(x => x.SalleAudience).HasColumnName("SALLE_AUDIENCE");
            e.Property(x => x.LangueRole).HasColumnName("LANGUE_ROLE");
            e.Property(x => x.LangueRequete).HasColumnName("LANGUE_REQUETE");
            e.Property(x => x.Tolkcode).HasColumnName("TOLKCODE");
            e.Property(x => x.NroRoleGen).HasColumnName("NRO_ROLE_GEN");
            e.Property(x => x.Proc).HasColumnName("PROC");
            e.Property(x => x.Nom).HasColumnName("NOM");
            e.Property(x => x.LibelleFr).HasColumnName("LIBELLE_FR");
            e.Property(x => x.LangueCgoe).HasColumnName("LANGUE_CGOE");
        });

        // =========================
        // Conversion bool → NUMBER(1)
        // =========================
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties()
                .Where(p => p.ClrType == typeof(bool) || p.ClrType == typeof(bool?)))
            {
                property.SetColumnType("NUMBER(1)");
                property.SetValueConverter(
                    new ValueConverter<bool?, int>(
                        v => v.HasValue && v.Value ? 1 : 0,
                        v => v == 1
                    )
                );
            }
        }

        // =========================
        // SEQUENCES
        // =========================
        modelBuilder.HasSequence<int>("ID_PRESTATION_AUTO");
        modelBuilder.HasSequence<int>("NR_AUTO_PAIEMENT");
        modelBuilder.HasSequence<int>("NR_AUTO_TOLKLINK");
        modelBuilder.HasSequence<int>("NR_AUTO_ADRESSE");

        // =========================
        // TABLES
        // =========================

        // STATUT
        modelBuilder.Entity<Statut>(entity =>
        {
            entity.ToTable("STATUT");
            entity.HasKey(e => e.IdStatut);
            entity.Property(e => e.IdStatut).HasColumnName("ID_STATUT");
            entity.Property(e => e.TypeStatut).HasColumnName("TYPE_STATUT");
        });

        // LANGUE
        modelBuilder.Entity<Langue>(entity =>
        {
            entity.ToTable("LANGUE");
            entity.HasKey(e => e.Idlangue);
            entity.Property(e => e.Idlangue).HasColumnName("IDLANGUE").ValueGeneratedNever();
            entity.Property(e => e.LibelleFr).HasColumnName("LIBELLE_FR");
            entity.Property(e => e.LibelleNl).HasColumnName("LIBELLE_NL");
            entity.Property(e => e.CodeIso).HasColumnName("CODE_ISO");
            entity.Property(e => e.TypeLangue).HasColumnName("TYPE_LANGUE");
            entity.Property(e => e.IslangueDestination).HasColumnName("ISLANGUE_DESTINATION");
        });

        // LANGUE_SOURCE
        modelBuilder.Entity<LangueSource>(entity =>
        {
            entity.ToTable("LANGUE_SOURCE");
            entity.HasKey(e => e.IdLanguesource);
            entity.Property(e => e.IdLanguesource).HasColumnName("ID_LANGUESOURCE");
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            entity.Property(e => e.NrLangue).HasColumnName("NR_LANGUE");
            entity.Property(e => e.TaalcodeOld).HasColumnName("TAALCODE_OLD");
        });

        // LANGUE_DESTINATION
        modelBuilder.Entity<LangueDestination>(entity =>
        {
            entity.ToTable("LANGUE_DESTINATION");
            entity.HasKey(e => e.IdLanguedestination);
            entity.Property(e => e.IdLanguedestination).HasColumnName("ID_LANGUEDESTINATION");
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            entity.Property(e => e.NrLangue).HasColumnName("NR_LANGUE");
        });

        // TOLKIDENTITY
        modelBuilder.Entity<Tolkidentity>(entity =>
        {
            entity.ToTable("TOLKIDENTITY");
            entity.HasKey(e => e.Tolkcode);
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            entity.Property(e => e.Nom).HasColumnName("NOM");
            entity.Property(e => e.Prenom).HasColumnName("PRENOM");
            entity.Property(e => e.Tel).HasColumnName("TEL");
            entity.Property(e => e.Telbis).HasColumnName("TELBIS");
            entity.Property(e => e.Gsm).HasColumnName("GSM");
            entity.Property(e => e.Bankrekening).HasColumnName("BANKREKENING");
            entity.Property(e => e.Fedcomnummer).HasColumnName("FEDCOMNUMMER");
        });

        // TOLKADRESSE
        modelBuilder.Entity<Tolkadresse>(entity =>
        {
            entity.ToTable("TOLKADRESSE");
            entity.HasKey(e => e.IdAdresse);
            entity.Property(e => e.IdAdresse)
                  .HasColumnName("ID_ADRESSE")
                  .ValueGeneratedOnAdd()
                  .HasDefaultValueSql("NR_AUTO_ADRESSE.NEXTVAL");
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            entity.Property(e => e.Land).HasColumnName("LAND");
            entity.Property(e => e.Cp).HasColumnName("CP");
            entity.Property(e => e.Commune).HasColumnName("COMMUNE");
            entity.Property(e => e.Rue).HasColumnName("RUE");
            entity.Property(e => e.Numero).HasColumnName("NUMERO");
            entity.Property(e => e.Boite).HasColumnName("BOITE");
            entity.Property(e => e.Km).HasColumnName("KM");
            entity.Property(e => e.Startdate).HasColumnName("STARTDATE").HasColumnType("DATE");
            entity.Property(e => e.Enddate).HasColumnName("ENDDATE").HasColumnType("DATE");
            entity.Property(e => e.Datecreate).HasColumnName("DATECREATE").HasColumnType("DATE");
            entity.Property(e => e.Usercreate).HasColumnName("USERCREATE");
            entity.Property(e => e.Datemodif).HasColumnName("DATEMODIF").HasColumnType("DATE");
            entity.Property(e => e.Usermodif).HasColumnName("USERMODIF");
        });

        // TOLKINDISPO
        modelBuilder.Entity<Tolkindispo>(entity =>
        {
            entity.ToTable("TOLKINDISPO");
            entity.HasKey(e => e.IdIndispo);
            entity.Property(e => e.IdIndispo).HasColumnName("ID_INDISPO");
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            entity.Property(e => e.Startindispo).HasColumnName("STARTINDISPO");
            entity.Property(e => e.Endindispo).HasColumnName("ENDINDISPO");
            entity.Property(e => e.Motifindispo).HasColumnName("MOTIFINDISPO");
            entity.Property(e => e.Commentaire).HasColumnName("COMMENTAIRE");
            entity.Property(e => e.Datecreate).HasColumnName("DATECREATE");
            entity.Property(e => e.Usercreate).HasColumnName("USERCREATE");
            entity.Property(e => e.Datemodif).HasColumnName("DATEMODIF");
            entity.Property(e => e.Usermodif).HasColumnName("USERMODIF");
        });

        // TOLK_TVA
        modelBuilder.Entity<TolkTva>(entity =>
        {
            entity.ToTable("TOLK_TVA");
            entity.HasKey(e => e.IdTolkTva);
            entity.Property(e => e.IdTolkTva).HasColumnName("ID_TOLK_TVA");
            entity.Property(e => e.IdStatut).HasColumnName("ID_STATUT");
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            entity.Property(e => e.StartDate).HasColumnName("START_DATE");
            entity.Property(e => e.EndDate).HasColumnName("END_DATE");
        });

        // INDEXATION
        modelBuilder.Entity<Indexation>(entity =>
        {
            entity.ToTable("INDEXATION");
            entity.HasKey(e => e.IdIndex);
            entity.Property(e => e.Startdate).HasColumnName("STARTDATE");
            entity.Property(e => e.Enddate).HasColumnName("ENDDATE");
            entity.Property(e => e.Euroheure).HasColumnName("EUROHEURE");
            entity.Property(e => e.Eurokm).HasColumnName("EUROKM");
            entity.Property(e => e.Euro75min).HasColumnName("EURO75MIN");
            entity.Property(e => e.IdIndex).HasColumnName("ID_INDEX");
        });

        // TOLKLINK
        modelBuilder.Entity<Tolklink>(entity =>
        {
            entity.ToTable("TOLKLINK");
            entity.HasKey(e => e.IdTolklink);

            entity.Property(e => e.IdTolklink)
                  .HasColumnName("ID_TOLKLINK")
                  .ValueGeneratedOnAdd()
                  .HasDefaultValueSql("NR_AUTO_TOLKLINK.NEXTVAL");

            entity.Property(e => e.NrAffAudience).HasColumnName("NR_AFF_AUDIENCE");
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            entity.Property(e => e.Datecreate).HasColumnName("DATECREATE");
            entity.Property(e => e.Datemodif).HasColumnName("DATEMODIF");
            entity.Property(e => e.Datesupp).HasColumnName("DATESUPP");
            entity.Property(e => e.Usercreate).HasColumnName("USERCREATE").HasMaxLength(100);
            entity.Property(e => e.IdPrestation).HasColumnName("ID_PRESTATION");

            entity.HasOne<Prestation>()
                  .WithMany(p => p.Tolklinks)
                  .HasForeignKey(e => e.IdPrestation)
                  .OnDelete(DeleteBehavior.NoAction)
                  .HasConstraintName("FK_TOLKLINK_PRESTATION");
        });

        // PRESTATION
        modelBuilder.Entity<Prestation>(entity =>
        {
            entity.ToTable("PRESTATION");
            entity.HasKey(e => e.IdPrestation);

            entity.Property(e => e.IdPrestation)
                  .HasColumnName("ID_PRESTATION")
                  .ValueGeneratedOnAdd()
                  .HasDefaultValueSql("ID_PRESTATION_AUTO.NEXTVAL");

            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            entity.Property(e => e.DatePrestation).HasColumnName("DATE_PRESTATION").HasColumnType("DATE");
            entity.Property(e => e.Startheure).HasColumnName("STARTHEURE");
            entity.Property(e => e.Endheure).HasColumnName("ENDHEURE");
            entity.Property(e => e.UserCreate).HasColumnName("USER_CREATE");
            entity.Property(e => e.IdPaiement).HasColumnName("ID_PAIEMENT");

            entity.HasOne(p => p.IdPaiementNavigation)
                  .WithMany()
                  .HasForeignKey(p => p.IdPaiement)
                  .OnDelete(DeleteBehavior.NoAction)
                  .HasConstraintName("FK_PRESTATION_PAIEMENT");
        });

        // PAIEMENT
        modelBuilder.Entity<Paiement>(entity =>
        {
            entity.ToTable("PAIEMENT");
            entity.HasKey(e => e.IdPaiement);

            entity.Property(e => e.IdPaiement)
                  .HasColumnName("ID_PAIEMENT")
                  .ValueGeneratedOnAdd()
                  .HasDefaultValueSql("NR_AUTO_PAIEMENT.NEXTVAL");

            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            entity.Property(e => e.DatePrestation).HasColumnName("DATE_PRESTATION").HasColumnType("DATE");
            entity.Property(e => e.Montant).HasColumnName("MONTANT");
            entity.Property(e => e.Transport).HasColumnName("TRANSPORT");
            entity.Property(e => e.Total).HasColumnName("TOTAL");
            entity.Property(e => e.MontantTva).HasColumnName("MONTANT_TVA");
            entity.Property(e => e.IdFacture).HasColumnName("ID_FACTURE");

            entity.Property<DateTime?>("DATE_SIGNEE");
            entity.Property<DateTime?>("DATE_TRANSMISSION");
            entity.Property<DateTime?>("DATE_PAIEMENT");
            entity.Property<decimal?>("ID_INDEX");
            entity.Property<decimal?>("PRESTATION_TVA");
            entity.Property<decimal?>("TRANSPORT_TVA");
        });
        modelBuilder.Entity<Facture>(entity =>
        {
            entity.ToTable("FACTURE");

            entity.HasKey(e => e.IdFacture)
                  .HasName("PK_FACTURE");

            entity.Property(e => e.IdFacture)
                  .HasColumnName("ID_FACTURE")
                  .ValueGeneratedOnAdd()
                  .HasDefaultValueSql("NR_AUTO_FACTURE.NEXTVAL");

            entity.Property(e => e.Tolkcode)
                  .HasColumnName("TOLKCODE")
                  .IsRequired();

            entity.Property(e => e.DateGeneration)
                  .HasColumnName("DATE_GENERATION")
                  .HasColumnType("DATE")
                  .HasDefaultValueSql("SYSDATE")
                  .IsRequired();

            entity.Property(e => e.DateValidationFedcom)
                  .HasColumnName("DATE_VALIDATION_FEDCOM")
                  .HasColumnType("DATE");
            entity.Property(e => e.DateTransmission)
      .HasColumnName("DATE_TRANSMISSION")
      .HasColumnType("DATE");

            entity.Property(e => e.StatutFacture)
                  .HasColumnName("STATUT_FACTURE")
                  .HasMaxLength(20)
                  .HasDefaultValue("GENEREE")
                  .IsRequired();

            entity.Property(e => e.TotalTtc)
                  .HasColumnName("TOTAL_TTC")
                  .HasColumnType("NUMBER(12,2)")
                  .HasDefaultValue(0)
                  .IsRequired();

            entity.Property(e => e.IdFactureOrigine)
                  .HasColumnName("ID_FACTURE_ORIGINE");

            entity.HasMany(e => e.Paiements)
                  .WithOne(p => p.Facture)
                  .HasForeignKey(p => p.IdFacture)
                  .OnDelete(DeleteBehavior.NoAction)
                  .HasConstraintName("FK_PAIEMENT_FACTURE");
        });
        }

    // 0) Récupérer les prestations annulées/créditées (liées à une facture ANNULEE ou NOTE DE CREDIT)
    public async Task<List<int>> GetFacturesAnnulees(CancellationToken ct)
    {
        var facturesAnnulees = await Factures
            .Where(f => f.StatutFacture == "ANNULEE" || f.StatutFacture == "NOTE DE CREDIT" || f.StatutFacture == "CREDIT VALIDE")
            .Select(f => f.IdFacture)
            .ToListAsync(ct);

        return facturesAnnulees;
    }

    public async Task<List<int>> GetPaiementsAnnules(CancellationToken ct)
    {
        // Récupérer les identifiants des factures annulées
        var facturesAnnulees = await GetFacturesAnnulees(ct);

        var paiementsAnnules = await Paiements
            .Where(p => p.IdFacture != null && facturesAnnulees.Contains(p.IdFacture.Value))
            .Select(p => p.IdPaiement)
            .ToListAsync(ct);

        return paiementsAnnules;
    }

    public async Task<List<int>> GetPrestationsAnnulees(CancellationToken ct)
    {
        // Récupérer les identifiants des paiements annulés
        var paiementsAnnules = await GetPaiementsAnnules(ct);

        var prestationsAnnulees = await Prestations
            .Where(pr => paiementsAnnules.Contains(pr.IdPaiement))
            .Select(pr => pr.IdPrestation)
            .ToListAsync(ct);

        return prestationsAnnulees;
    }

}
