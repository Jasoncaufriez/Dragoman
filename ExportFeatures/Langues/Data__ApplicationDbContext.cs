using Dragoman.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // ===== VUES =====
    public DbSet<VueCalendarVrmPc> VueCalendarVrmPc { get; set; } = null!;
    public DbSet<VueCalendarAnn> VueCalendarAnn { get; set; } = null!;

    // ===== TABLES =====
    public DbSet<Tolkidentity> Tolkidentities { get; set; } = null!;
    public DbSet<Tolkadresse> Tolkadresses { get; set; } = null!;
    public DbSet<Langue> Langues { get; set; } = null!;
    public DbSet<LangueSource> LangueSources { get; set; } = null!;
    public DbSet<LangueDestination> LangueDestinations { get; set; } = null!;
    public DbSet<TolkTva> TolkTvas { get; set; } = null!;
    public DbSet<Statut> Statuts { get; set; } = null!;
    public DbSet<Tolkindispo> Tolkindispos { get; set; } = null!;



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 🔥 Conversion automatique : bool → NUMBER(1)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties()
                .Where(p => p.ClrType == typeof(bool)))
            {
                property.SetColumnType("NUMBER(1)");
                property.SetValueConverter(
                    new ValueConverter<bool?, int>(
                        v => v.HasValue && v.Value ? 1 : 0, // C# → Oracle
                        v => v == 1                        // Oracle → C#
                    )
                );
            }
        }
        // =========================
        // VUES (Keyless)
        // =========================

        // VRM (plein contentieux)
        modelBuilder.Entity<VueCalendarVrmPc>(e =>
        {
            e.HasNoKey();
            e.ToView("Vue_CALENDAR_VRM_PC"); // casse exacte

            e.Property(p => p.NroRoleGen).HasColumnName("NRO_ROLE_GEN").HasPrecision(38, 0);
            e.Property(p => p.LangueRole).HasColumnName("LANGUE_ROLE");
            e.Property(p => p.Proc).HasColumnName("PROC");
            e.Property(p => p.DateAudience).HasColumnName("DATE_AUDIENCE");
            e.Property(p => p.Nom).HasColumnName("NOM");
            e.Property(p => p.SalleAudience).HasColumnName("SALLE_AUDIENCE");
            e.Property(p => p.HeureAudience).HasColumnName("HEURE_AUDIENCE");
            e.Property(p => p.LangueRequete).HasColumnName("LANGUE_REQUETE");
            e.Property(p => p.LibelleFr).HasColumnName("LIBELLE_FR");
            e.Property(p => p.LangueCgoe).HasColumnName("LANGUE_CGOE");
            e.Property(p => p.IdAffAudience).HasColumnName("ID_AFF_AUDIENCE").HasPrecision(38, 0);
            e.Property(p => p.Tolkcode).HasColumnName("TOLKCODE").HasPrecision(38, 0);
        });

        // ANN (annulation)
        modelBuilder.Entity<VueCalendarAnn>(e =>
        {
            e.HasNoKey();
            e.ToView("VUE_CALENDAR_ANN");

            e.Property(p => p.NroRoleGen).HasColumnName("NRO_ROLE_GEN").HasPrecision(38, 0);
            e.Property(p => p.LangueRole).HasColumnName("LANGUE_ROLE");
            e.Property(p => p.Proc).HasColumnName("PROC");
            e.Property(p => p.DateAudience).HasColumnName("DATE_AUDIENCE");
            e.Property(p => p.Nom).HasColumnName("NOM");
            e.Property(p => p.SalleAudience).HasColumnName("SALLE_AUDIENCE");
            e.Property(p => p.HeureAudience).HasColumnName("HEURE_AUDIENCE");
            e.Property(p => p.LangueRequete).HasColumnName("LANGUE_REQUETE");
            e.Property(p => p.LibelleFr).HasColumnName("LIBELLE_FR");
            e.Property(p => p.LangueCgoe).HasColumnName("LANGUE_CGOE");
            e.Property(p => p.IdAffAudience).HasColumnName("ID_AFF_AUDIENCE").HasPrecision(38, 0);
            e.Property(p => p.Tolkcode).HasColumnName("TOLKCODE").HasPrecision(38, 0);
        });

        // =========================
        // TABLES
        // =========================

        // TOLKIDENTITY
        modelBuilder.Entity<Tolkidentity>(entity =>
        {
            entity.ToTable("TOLKIDENTITY");
            entity.HasKey(e => e.Tolkcode);
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            // mappe ici d’autres colonnes si nécessaire
        });

        // Séquence ADRESSE
        modelBuilder.HasSequence<int>("NR_AUTO_ADRESSE");

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
            entity.Property(e => e.Startdate).HasColumnName("STARTDATE");
            entity.Property(e => e.Enddate).HasColumnName("ENDDATE");
            entity.Property(e => e.Datecreate).HasColumnName("DATECREATE");
            entity.Property(e => e.Usercreate).HasColumnName("USERCREATE");
            entity.Property(e => e.Datemodif).HasColumnName("DATEMODIF");
            entity.Property(e => e.Usermodif).HasColumnName("USERMODIF");
        });

        // LANGUE (référentiel)
        modelBuilder.Entity<Langue>(entity =>
        {
            entity.ToTable("LANGUE");
            entity.HasKey(e => e.Idlangue);

            entity.Property(e => e.Idlangue).HasColumnName("IDLANGUE").ValueGeneratedNever();
            entity.Property(e => e.LibelleFr).HasColumnName("LIBELLE_FR");
            entity.Property(e => e.LibelleNl).HasColumnName("LIBELLE_NL");
            entity.Property(e => e.CodeIso).HasColumnName("CODE_ISO");
            entity.Property(e => e.TypeLangue).HasColumnName("TYPE_LANGUE");
            entity.Property(e => e.IslangueDestination)
            .HasColumnName("ISLANGUE_DESTINATION")
            .HasConversion(
            v => v.HasValue && v.Value ? 1 : 0, // Fix: Explicitly handle nullable bool
            v => v == 1
            )
            .HasColumnType("NUMBER(1)");


        });

        // Séquence LANGUE_SOURCE
        modelBuilder.HasSequence<int>("NR_AUTO_LANGUE_SOURCE");

        modelBuilder.Entity<LangueSource>(entity =>
        {
            entity.ToTable("LANGUE_SOURCE");
            entity.HasKey(e => e.IdLanguesource);

            entity.Property(e => e.IdLanguesource)
                  .HasColumnName("ID_LANGUESOURCE")
                  .ValueGeneratedOnAdd()
                  .HasDefaultValueSql("NR_AUTO_LANGUE_SOURCE.NEXTVAL");

            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            entity.Property(e => e.NrLangue).HasColumnName("NR_LANGUE");
            entity.Property(e => e.TaalcodeOld).HasColumnName("TAALCODE_OLD");
        });

        // Séquence LANGUE_DESTINATION
        modelBuilder.HasSequence<int>("NR_AUTO_DESTINATION");

        modelBuilder.Entity<LangueDestination>(entity =>
        {
            entity.ToTable("LANGUE_DESTINATION");
            entity.HasKey(e => e.IdLanguedestination);

            entity.Property(e => e.IdLanguedestination)
                  .HasColumnName("ID_LANGUEDESTINATION")
                  .ValueGeneratedOnAdd()
                  .HasDefaultValueSql("NR_AUTO_DESTINATION.NEXTVAL");

            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            entity.Property(e => e.NrLangue).HasColumnName("NR_LANGUE");
        });
        modelBuilder.Entity<TolkTva>(entity =>
        {
            entity.ToTable("TOLK_TVA");
            entity.HasKey(e => e.IdTolkTva);
            entity.Property(e => e.IdTolkTva)
                  .HasColumnName("ID_TOLK_TVA")
                  .ValueGeneratedOnAdd();   
            entity.Property(e => e.IdStatut).HasColumnName("ID_STATUT");
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE");
            entity.Property(e => e.StartDate).HasColumnName("START_DATE");
            entity.Property(e => e.EndDate).HasColumnName("END_DATE");
        });
        modelBuilder.Entity<Statut>(e =>
        {
            e.ToTable("STATUT");
            e.HasKey(x => x.IdStatut);
            e.Property(x => x.IdStatut).HasColumnName("ID_STATUT");
            e.Property(x => x.TypeStatut).HasColumnName("TYPE_STATUT");
        });
        // Séquence (déclaration côté EF : utile si tu utilises des migrations)
        modelBuilder.HasSequence<int>("NR_AUTO_INDISPO", schema: "DRAGOMAN");

        modelBuilder.Entity<Tolkindispo>(entity =>
        {
            entity.ToTable("TOLKINDISPO");
            entity.HasKey(e => e.IdIndispo);

            entity.Property(e => e.IdIndispo)
                  .HasColumnName("ID_INDISPO")
                  .ValueGeneratedOnAdd()
                  .HasDefaultValueSql("DRAGOMAN.NR_AUTO_INDISPO.NEXTVAL"); // <- qualifié

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

        base.OnModelCreating(modelBuilder);
    }
}
