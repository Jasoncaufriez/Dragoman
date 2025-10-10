using Dragoman.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // ===== VUES (DbSet keyless) =====
    // ⚠️ Noms alignés avec les usages dans les contrôleurs (pluriels)
    public DbSet<VueCalendarVrmPc> VueCalendarVrmPcs { get; set; } = null!;
    public DbSet<VueCalendarAnn> VueCalendarAnns { get; set; } = null!;
    public DbSet<ReportInterpreteRow> ReportInterpreteRows { get; set; } = null!;
    public DbSet<VAudienceInterpreteDetail> VAudienceInterpreteDetail { get; set; } = null!;

    // ===== TABLES =====
    public DbSet<Tolkidentity> Tolkidentities { get; set; } = null!;
    public DbSet<Tolkadresse> Tolkadresses { get; set; } = null!;
    public DbSet<Langue> Langues { get; set; } = null!;
    public DbSet<LangueSource> LangueSources { get; set; } = null!;
    public DbSet<LangueDestination> LangueDestinations { get; set; } = null!;
    public DbSet<TolkTva> TolkTvas { get; set; } = null!;
    public DbSet<Statut> Statuts { get; set; } = null!;
    public DbSet<Tolkindispo> Tolkindispos { get; set; } = null!;

    // ===== AJOUTS POUR PRESTATIONS =====
    public DbSet<Tolklink> Tolklinks { get; set; } = null!;
    public DbSet<Prestation> Prestations { get; set; } = null!;
    public DbSet<Paiement> Paiements { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =========================
        // VUES (Keyless)
        // =========================
        modelBuilder.Entity<ReportInterpreteRow>()
            .ToView("V_INTERPRETES_AUDIENCES_JOUR")
            .HasNoKey();

        modelBuilder.Entity<VAudienceInterpreteDetail>()
            .ToView("V_AUDIENCE_INTERPRETE_DETAIL")
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
        // Conversion bool → NUMBER(1) Oracle (optionnel, garde si utile)
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
        // SEQUENCES (déclarées côté DB)
        // =========================
        modelBuilder.HasSequence<int>("ID_PRESTATION_AUTO");   // PRESTATION PK
        modelBuilder.HasSequence<int>("NR_AUTO_PAIEMENT");     // PAIEMENT PK
        modelBuilder.HasSequence<int>("NR_AUTO_TOLKLINK");     // TOLKLINK PK

        // =========================
        // TABLES
        // =========================

        // STATUT (PK: ID_STATUT NUMBER(2))
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
            entity.Property(e => e.Idlangue).HasColumnName("IDLANGUE");
            entity.Property(e => e.LibelleFr).HasColumnName("LIBELLE_FR");
            entity.Property(e => e.LibelleNl).HasColumnName("LIBELLE_NL");
            entity.Property(e => e.CodeIso).HasColumnName("CODE_ISO");
            entity.Property(e => e.TypeLangue).HasColumnName("TYPE_LANGUE");
            entity.Property(e => e.IslangueDestination).HasColumnName("ISLANGUE_DESTINATION");
        });

        // LANGUE_SOURCE (PK: ID_LANGUESOURCE)
        modelBuilder.Entity<LangueSource>(entity =>
        {
            entity.ToTable("LANGUE_SOURCE");
            entity.HasKey(e => e.IdLanguesource);
            entity.Property(e => e.IdLanguesource).HasColumnName("ID_LANGUESOURCE");
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            entity.Property(e => e.NrLangue).HasColumnName("NR_LANGUE");
            entity.Property(e => e.TaalcodeOld).HasColumnName("TAALCODE_OLD");
        });

        // LANGUE_DESTINATION (PK: ID_LANGUEDESTINATION)
        modelBuilder.Entity<LangueDestination>(entity =>
        {
            entity.ToTable("LANGUE_DESTINATION");
            entity.HasKey(e => e.IdLanguedestination);
            entity.Property(e => e.IdLanguedestination).HasColumnName("ID_LANGUEDESTINATION");
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            entity.Property(e => e.NrLangue).HasColumnName("NR_LANGUE");
        });

        // TOLKIDENTITY (PK: TOLKCODE)
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
            // … autres colonnes si besoin
        });

        // TOLKADRESSE (PK: ID_ADRESSE)
        modelBuilder.Entity<Tolkadresse>(entity =>
        {
            entity.ToTable("TOLKADRESSE");
            entity.HasKey(e => e.IdAdresse);
            entity.Property(e => e.IdAdresse).HasColumnName("ID_ADRESSE");
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            // … autres colonnes si besoin
        });

        // TOLKINDISPO (PK: ID_INDISPO)
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

        // TOLK_TVA (PK: ID_TOLK_TVA)
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

        // TOLKLINK (PK: ID_TOLKLINK, seq: NR_AUTO_TOLKLINK)
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
            entity.Property(e => e.Usercreate).HasColumnName("USERCREATE");
            entity.Property(e => e.IdPrestation).HasColumnName("ID_PRESTATION");
        });

        // PRESTATION (PK: ID_PRESTATION, seq: ID_PRESTATION_AUTO)
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
            entity.Property(e => e.Startheure).HasColumnName("STARTHEURE");   // TIMESTAMP(8)
            entity.Property(e => e.Endheure).HasColumnName("ENDHEURE");       // TIMESTAMP(6)
            entity.Property(e => e.UserCreate).HasColumnName("USER_CREATE");
            entity.Property(e => e.IdPaiement).HasColumnName("ID_PAIEMENT");
        });

        // PAIEMENT (PK: ID_PAIEMENT, seq: NR_AUTO_PAIEMENT)
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
            // Les colonnes optionnelles existent côté DB (toutes nullables) :
            entity.Property<DateTime?>("DATE_SIGNEE");
            entity.Property<DateTime?>("DATE_TRANSMISSION");
            entity.Property<DateTime?>("DATE_PAIEMENT");
            entity.Property<decimal?>("ID_INDEX");
            entity.Property<decimal?>("PRESTATION_TVA");
            entity.Property<decimal?>("TRANSPORT_TVA");
        });
    }
}
