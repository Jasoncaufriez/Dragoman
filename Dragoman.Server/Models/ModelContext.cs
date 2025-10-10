using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Dragoman.Server.Models;

public partial class ModelContext : DbContext
{
    public ModelContext()
    {
    }

    public ModelContext(DbContextOptions<ModelContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AudienceSupp> AudienceSupps { get; set; }

    public virtual DbSet<BaseCout> BaseCouts { get; set; }

    public virtual DbSet<BasePrestation> BasePrestations { get; set; }

    public virtual DbSet<Calendar> Calendars { get; set; }

    public virtual DbSet<CalendarEntier> CalendarEntiers { get; set; }

    public virtual DbSet<ComboboxLangueDestination> ComboboxLangueDestinations { get; set; }

    public virtual DbSet<ComboboxLangueSource> ComboboxLangueSources { get; set; }

    public virtual DbSet<CountExp> CountExps { get; set; }

    public virtual DbSet<Indexation> Indexations { get; set; }

    public virtual DbSet<Langue> Langues { get; set; }

    public virtual DbSet<LangueDestination> LangueDestinations { get; set; }

    public virtual DbSet<LangueSource> LangueSources { get; set; }

    public virtual DbSet<ListeInterpreteDisponible> ListeInterpreteDisponibles { get; set; }

    public virtual DbSet<ListeInterpretePlanning> ListeInterpretePlannings { get; set; }

    public virtual DbSet<Paiement> Paiements { get; set; }

    public virtual DbSet<Planning> Plannings { get; set; }

    public virtual DbSet<PlanningTolk> PlanningTolks { get; set; }

    public virtual DbSet<Prestation> Prestations { get; set; }

    public virtual DbSet<PrestationMoi> PrestationMois { get; set; }

    public virtual DbSet<PrestationVueJour> PrestationVueJours { get; set; }

    public virtual DbSet<Statut> Statuts { get; set; }

    public virtual DbSet<Test> Tests { get; set; }

    public virtual DbSet<TestADelete> TestADeletes { get; set; }

    public virtual DbSet<TestSelectLangue> TestSelectLangues { get; set; }

    public virtual DbSet<TolkPaiement> TolkPaiements { get; set; }

    public virtual DbSet<TolkTva> TolkTvas { get; set; }

    public virtual DbSet<TolkZonderFedcom> TolkZonderFedcoms { get; set; }

    public virtual DbSet<Tolkadresse> Tolkadresses { get; set; }

    public virtual DbSet<Tolkidentity> Tolkidentities { get; set; }

    public virtual DbSet<Tolkindispo> Tolkindispos { get; set; }

    public virtual DbSet<Tolklink> Tolklinks { get; set; }

    public virtual DbSet<ToutLesTolk> ToutLesTolks { get; set; }

    public virtual DbSet<VerificationConvoc> VerificationConvocs { get; set; }

    public virtual DbSet<View1> View1s { get; set; }

    public virtual DbSet<View3> View3s { get; set; }

    public virtual DbSet<VueAlerteAudienceSupp> VueAlerteAudienceSupps { get; set; }

    public virtual DbSet<VueCalendarAnn> VueCalendarAnns { get; set; }

    public virtual DbSet<VueCalendarVrmPc> VueCalendarVrmPcs { get; set; }

    public virtual DbSet<VueLangue> VueLangues { get; set; }

    public virtual DbSet<VuePrestationManquante> VuePrestationManquantes { get; set; }

    public virtual DbSet<VueTva> VueTvas { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseOracle("Data Source=LAURENTIDE;User ID=DRAGOMAN;Password=InterTolk;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDefaultSchema("DRAGOMAN")
            .UseCollation("USING_NLS_COMP");

        modelBuilder.Entity<AudienceSupp>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("AUDIENCE_SUPP");

            entity.Property(e => e.AudienceSupp1)
                .HasMaxLength(59)
                .IsUnicode(false)
                .HasColumnName("AUDIENCE_SUPP");
        });

        modelBuilder.Entity<BaseCout>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("BASE_COUT");

            entity.Property(e => e.DatePrestation)
                .HasColumnType("DATE")
                .HasColumnName("DATE_PRESTATION");
            entity.Property(e => e.Euroheure)
                .HasColumnType("FLOAT")
                .HasColumnName("EUROHEURE");
            entity.Property(e => e.Eurokm)
                .HasColumnType("FLOAT")
                .HasColumnName("EUROKM");
            entity.Property(e => e.EurokmAPayer)
                .HasColumnType("NUMBER")
                .HasColumnName("EUROKM_A_PAYER");
            entity.Property(e => e.KmAPayer)
                .HasColumnType("NUMBER")
                .HasColumnName("KM_A_PAYER");
            entity.Property(e => e.Kmeuro)
                .HasColumnType("NUMBER")
                .HasColumnName("KMEURO");
            entity.Property(e => e.Testround)
                .HasColumnType("NUMBER")
                .HasColumnName("TESTROUND");
            entity.Property(e => e.Tolkcode)
                .HasColumnType("NUMBER")
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<BasePrestation>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("BASE_PRESTATIONS");

            entity.Property(e => e.DateAudience)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("DATE_AUDIENCE");
            entity.Property(e => e.Tolkcode)
                .HasColumnType("NUMBER")
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<Calendar>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("CALENDAR");

            entity.Property(e => e.DateAudience)
                .HasColumnType("DATE")
                .HasColumnName("DATE_AUDIENCE");
            entity.Property(e => e.HeureAudience)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("HEURE_AUDIENCE");
            entity.Property(e => e.IdAffAudience)
                .HasColumnType("NUMBER")
                .HasColumnName("ID_AFF_AUDIENCE");
            entity.Property(e => e.LangueCgoe)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LANGUE_CGOE");
            entity.Property(e => e.LangueRequete)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LANGUE_REQUETE");
            entity.Property(e => e.LangueRole)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("LANGUE_ROLE");
            entity.Property(e => e.LibelleFr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LIBELLE_FR");
            entity.Property(e => e.Nom)
                .HasMaxLength(128)
                .IsUnicode(false)
                .HasColumnName("NOM");
            entity.Property(e => e.NroRoleGen)
                .HasColumnType("NUMBER")
                .HasColumnName("NRO_ROLE_GEN");
            entity.Property(e => e.Proc)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("PROC");
            entity.Property(e => e.SalleAudience)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("SALLE_AUDIENCE");
            entity.Property(e => e.Tolkcode)
                .HasColumnType("NUMBER")
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<CalendarEntier>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("CALENDAR_ENTIER");

            entity.Property(e => e.DateAudience)
                .HasColumnType("DATE")
                .HasColumnName("DATE_AUDIENCE");
            entity.Property(e => e.HeureAudience)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("HEURE_AUDIENCE");
            entity.Property(e => e.IdAffAudience)
                .HasColumnType("NUMBER")
                .HasColumnName("ID_AFF_AUDIENCE");
            entity.Property(e => e.LangueCgoe)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LANGUE_CGOE");
            entity.Property(e => e.LangueRequete)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LANGUE_REQUETE");
            entity.Property(e => e.LangueRole)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("LANGUE_ROLE");
            entity.Property(e => e.LibelleFr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LIBELLE_FR");
            entity.Property(e => e.Nom)
                .HasMaxLength(128)
                .IsUnicode(false)
                .HasColumnName("NOM");
            entity.Property(e => e.NroRoleGen)
                .HasColumnType("NUMBER")
                .HasColumnName("NRO_ROLE_GEN");
            entity.Property(e => e.Proc)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("PROC");
            entity.Property(e => e.SalleAudience)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("SALLE_AUDIENCE");
            entity.Property(e => e.Tolkcode)
                .HasColumnType("NUMBER")
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<ComboboxLangueDestination>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("COMBOBOX_LANGUE_DESTINATION");

            entity.Property(e => e.LibelleFr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LIBELLE_FR");
            entity.Property(e => e.NrLangue)
                .HasPrecision(6)
                .HasColumnName("NR_LANGUE");
        });

        modelBuilder.Entity<ComboboxLangueSource>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("COMBOBOX_LANGUE_SOURCE");

            entity.Property(e => e.LibelleFr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LIBELLE_FR");
            entity.Property(e => e.NrLangue)
                .HasPrecision(6)
                .HasColumnName("NR_LANGUE");
        });

        modelBuilder.Entity<CountExp>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("COUNT_EXP");

            entity.Property(e => e.DateClic)
                .HasColumnType("DATE")
                .HasColumnName("DATE_CLIC");
            entity.Property(e => e.IdCount)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("ID_COUNT");
            entity.Property(e => e.Login)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("LOGIN");
            entity.Property(e => e.TypeConvoc)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("TYPE_CONVOC");
        });

        modelBuilder.Entity<Indexation>(entity =>
        {
            entity.HasKey(e => e.IdIndex).HasName("INDEXATION_PK");

            entity.ToTable("INDEXATION");

            entity.Property(e => e.IdIndex)
                .HasPrecision(3)
                .HasColumnName("ID_INDEX");
            entity.Property(e => e.Enddate)
                .HasColumnType("DATE")
                .HasColumnName("ENDDATE");
            entity.Property(e => e.Euro75min)
                .HasColumnType("FLOAT")
                .HasColumnName("EURO75MIN");
            entity.Property(e => e.Euroheure)
                .HasColumnType("FLOAT")
                .HasColumnName("EUROHEURE");
            entity.Property(e => e.Eurokm)
                .HasColumnType("FLOAT")
                .HasColumnName("EUROKM");
            entity.Property(e => e.Startdate)
                .HasColumnType("DATE")
                .HasColumnName("STARTDATE");
        });

        modelBuilder.Entity<Langue>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("LANGUE");

            entity.Property(e => e.CodeIso)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("CODE_ISO");
            entity.Property(e => e.Idlangue)
                .HasPrecision(3)
                .HasColumnName("IDLANGUE");
            entity.Property(e => e.IslangueDestination)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("ISLANGUE_DESTINATION");
            entity.Property(e => e.LibelleFr)
                .HasMaxLength(19)
                .IsUnicode(false)
                .HasColumnName("LIBELLE_FR");
            entity.Property(e => e.LibelleNl)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasColumnName("LIBELLE_NL");
            entity.Property(e => e.TypeLangue)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("TYPE_LANGUE");
        });

        modelBuilder.Entity<LangueDestination>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("LANGUE_DESTINATION");

            entity.Property(e => e.IdLanguedestination)
                .HasPrecision(7)
                .HasColumnName("ID_LANGUEDESTINATION");
            entity.Property(e => e.NrLangue)
                .HasPrecision(6)
                .HasColumnName("NR_LANGUE");
            entity.Property(e => e.Tolkcode)
                .HasPrecision(6)
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<LangueSource>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("LANGUE_SOURCE");

            entity.Property(e => e.IdLanguesource)
                .HasPrecision(7)
                .HasColumnName("ID_LANGUESOURCE");
            entity.Property(e => e.NrLangue)
                .HasPrecision(6)
                .HasColumnName("NR_LANGUE");
            entity.Property(e => e.TaalcodeOld)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("TAALCODE_OLD");
            entity.Property(e => e.Tolkcode)
                .HasPrecision(6)
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<ListeInterpreteDisponible>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("LISTE_INTERPRETE_DISPONIBLE");

            entity.Property(e => e.Tolkcode)
                .HasPrecision(10)
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<ListeInterpretePlanning>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("LISTE_INTERPRETE_PLANNING");

            entity.Property(e => e.Tolkcode)
                .HasColumnType("NUMBER")
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<Paiement>(entity =>
        {
            entity.ToTable("PAIEMENT");
            entity.HasKey(e => e.IdPaiement);
            entity.Property(e => e.IdPaiement)
                .HasColumnName("ID_PAIEMENT")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("NR_AUTO_PAIEMENT.NEXTVAL");
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE").HasMaxLength(5).IsRequired();
            entity.Property(e => e.DatePrestation).HasColumnName("DATE_PRESTATION").HasColumnType("DATE");
            entity.Property(e => e.Montant).HasColumnName("MONTANT").HasColumnType("NUMBER(10,2)");
            entity.Property(e => e.Transport).HasColumnName("TRANSPORT").HasColumnType("NUMBER(10,2)");
            entity.Property(e => e.Total).HasColumnName("TOTAL").HasColumnType("NUMBER(10,2)");
            entity.Property(e => e.MontantTva).HasColumnName("MONTANT_TVA").HasColumnType("NUMBER(10,2)");
        });

        modelBuilder.Entity<Planning>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("PLANNING");

            entity.Property(e => e.DateAudience)
                .HasColumnType("DATE")
                .HasColumnName("DATE_AUDIENCE");
            entity.Property(e => e.HeureAudience)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("HEURE_AUDIENCE");
            entity.Property(e => e.IdAffAudience)
                .HasColumnType("NUMBER")
                .HasColumnName("ID_AFF_AUDIENCE");
            entity.Property(e => e.LangueCgoe)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LANGUE_CGOE");
            entity.Property(e => e.LangueRequete)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LANGUE_REQUETE");
            entity.Property(e => e.LangueRole)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("LANGUE_ROLE");
            entity.Property(e => e.LibelleFr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LIBELLE_FR");
            entity.Property(e => e.Nom)
                .HasMaxLength(128)
                .IsUnicode(false)
                .HasColumnName("NOM");
            entity.Property(e => e.NroRoleGen)
                .HasColumnType("NUMBER")
                .HasColumnName("NRO_ROLE_GEN");
            entity.Property(e => e.Proc)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("PROC");
            entity.Property(e => e.SalleAudience)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("SALLE_AUDIENCE");
            entity.Property(e => e.Tolkcode)
                .HasColumnType("NUMBER")
                .HasColumnName("TOLKCODE");
            entity.Property(e => e.VerzoekerStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("VERZOEKER_STATUS");
            entity.Property(e => e.ZaakStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ZAAK_STATUS");
        });

        modelBuilder.Entity<PlanningTolk>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("PLANNING_TOLK");

            entity.Property(e => e.DateAudience)
                .HasColumnType("DATE")
                .HasColumnName("DATE_AUDIENCE");
            entity.Property(e => e.HeureAudience)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("HEURE_AUDIENCE");
            entity.Property(e => e.IdAffAudience)
                .HasColumnType("NUMBER")
                .HasColumnName("ID_AFF_AUDIENCE");
            entity.Property(e => e.LangueRequete)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LANGUE_REQUETE");
            entity.Property(e => e.LangueRole)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("LANGUE_ROLE");
            entity.Property(e => e.Nom)
                .HasMaxLength(128)
                .IsUnicode(false)
                .HasColumnName("NOM");
            entity.Property(e => e.Proc)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("PROC");
            entity.Property(e => e.SalleAudience)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("SALLE_AUDIENCE");
            entity.Property(e => e.Tolkcode)
                .HasColumnType("NUMBER")
                .HasColumnName("TOLKCODE");
            entity.Property(e => e.VerzoekerStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("VERZOEKER_STATUS");
            entity.Property(e => e.ZaakStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ZAAK_STATUS");
        });

        modelBuilder.Entity<Prestation>(entity =>
        {
            entity.ToTable("PRESTATION");

            entity.HasKey(e => e.IdPrestation);

            entity.Property(e => e.IdPrestation)
                .HasColumnName("ID_PRESTATION")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("ID_PRESTATION_AUTO.NEXTVAL");
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE").HasMaxLength(5).IsRequired();
            entity.Property(e => e.DatePrestation).HasColumnName("DATE_PRESTATION").HasColumnType("DATE");
            entity.Property(e => e.Startheure).HasColumnName("STARTHEURE").HasColumnType("TIMESTAMP(6)");
            entity.Property(e => e.Endheure).HasColumnName("ENDHEURE").HasColumnType("TIMESTAMP(6)");
            entity.Property(e => e.UserCreate).HasColumnName("USER_CREATE").HasMaxLength(50);
            entity.Property(e => e.IdPaiement).HasColumnName("ID_PAIEMENT");
        });

        modelBuilder.Entity<PrestationMoi>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("PRESTATION_MOIS");

            entity.Property(e => e.DateAudience)
                .HasColumnType("DATE")
                .HasColumnName("DATE_AUDIENCE");
            entity.Property(e => e.Debut)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("DEBUT");
            entity.Property(e => e.Fin)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("FIN");
            entity.Property(e => e.HeureAudience)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("HEURE_AUDIENCE");
            entity.Property(e => e.IdPaiement)
                .HasColumnType("NUMBER")
                .HasColumnName("ID_PAIEMENT");
            entity.Property(e => e.IdPrestation)
                .HasColumnType("NUMBER")
                .HasColumnName("ID_PRESTATION");
            entity.Property(e => e.NrAudience)
                .HasColumnType("NUMBER")
                .HasColumnName("NR_AUDIENCE");
            entity.Property(e => e.Tolkcode)
                .HasColumnType("NUMBER")
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<PrestationVueJour>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("PRESTATION_VUE_JOUR");

            entity.Property(e => e.ApercuPrestation)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("APERCU_PRESTATION");
            entity.Property(e => e.DatePrestation)
                .HasColumnType("DATE")
                .HasColumnName("DATE_PRESTATION");
        });

        modelBuilder.Entity<Statut>(entity =>
        {
            entity.HasKey(e => e.IdStatut);

            entity.ToTable("STATUT");

            entity.Property(e => e.IdStatut)
                .HasPrecision(2)
                .HasColumnName("ID_STATUT");
            entity.Property(e => e.TypeStatut)
                .HasMaxLength(32)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("TYPE_STATUT");
        });

        modelBuilder.Entity<Test>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("TEST_PK");

            entity.ToTable("TEST");

            entity.Property(e => e.Id)
                .HasColumnType("NUMBER")
                .HasColumnName("ID");
            entity.Property(e => e.TestNb)
                .HasColumnType("NUMBER")
                .HasColumnName("TEST_NB");
            entity.Property(e => e.TestVarchar)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("TEST_VARCHAR");
        });

        modelBuilder.Entity<TestADelete>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("TEST_A_DELETE");

            entity.Property(e => e.Tolkcode)
                .HasPrecision(10)
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<TestSelectLangue>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("TEST_SELECT_LANGUE");

            entity.Property(e => e.Tolkcode)
                .HasPrecision(10)
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<TolkPaiement>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("TOLK_PAIEMENT");

            entity.Property(e => e.Tolkcode)
                .HasColumnType("NUMBER")
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<TolkTva>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("TOLK_TVA");

            entity.Property(e => e.EndDate)
                .HasColumnType("DATE")
                .HasColumnName("END_DATE");
            entity.Property(e => e.IdStatut)
                .HasPrecision(2)
                .HasColumnName("ID_STATUT");
            entity.Property(e => e.IdTolkTva)
                .HasPrecision(10)
                .HasColumnName("ID_TOLK_TVA");
            entity.Property(e => e.StartDate)
                .HasColumnType("DATE")
                .HasColumnName("START_DATE");
            entity.Property(e => e.Tolkcode)
                .HasPrecision(10)
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<TolkZonderFedcom>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("TOLK_ZONDER_FEDCOM");

            entity.Property(e => e.Tolkcode)
                .HasPrecision(10)
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<Tolkadresse>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("TOLKADRESSE");

            entity.Property(e => e.Boite)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("BOITE");
            entity.Property(e => e.Commune)
                .HasMaxLength(44)
                .IsUnicode(false)
                .HasColumnName("COMMUNE");
            entity.Property(e => e.Cp)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("CP");
            entity.Property(e => e.Datecreate)
                .HasColumnType("DATE")
                .HasColumnName("DATECREATE");
            entity.Property(e => e.Datemodif)
                .HasColumnType("DATE")
                .HasColumnName("DATEMODIF");
            entity.Property(e => e.Enddate)
                .HasColumnType("DATE")
                .HasColumnName("ENDDATE");
            entity.Property(e => e.IdAdresse)
                .HasPrecision(5)
                .HasColumnName("ID_ADRESSE");
            entity.Property(e => e.Km)
                .HasPrecision(3)
                .HasColumnName("KM");
            entity.Property(e => e.Land)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("LAND");
            entity.Property(e => e.Numero)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("NUMERO");
            entity.Property(e => e.Rue)
                .HasMaxLength(29)
                .IsUnicode(false)
                .HasColumnName("RUE");
            entity.Property(e => e.Startdate)
                .HasColumnType("DATE")
                .HasColumnName("STARTDATE");
            entity.Property(e => e.Tolkcode)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("TOLKCODE");
            entity.Property(e => e.Usercreate)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("USERCREATE");
            entity.Property(e => e.Usermodif)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("USERMODIF");
        });

        modelBuilder.Entity<Tolkidentity>(entity =>
        {
            entity.HasKey(e => e.Tolkcode).HasName("TOLKIDENTITY_PK");

            entity.ToTable("TOLKIDENTITY");

            entity.Property(e => e.Tolkcode)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("TOLKCODE");
            entity.Property(e => e.Adresbusnr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ADRESBUSNR");
            entity.Property(e => e.Adresnr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ADRESNR");
            entity.Property(e => e.Ba)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("BA");
            entity.Property(e => e.Bankrekening)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("BANKREKENING");
            entity.Property(e => e.Beedigd)
                .HasPrecision(10)
                .HasColumnName("BEEDIGD");
            entity.Property(e => e.Beroepscode)
                .HasPrecision(10)
                .HasColumnName("BEROEPSCODE");
            entity.Property(e => e.BtwNr)
                .HasPrecision(10)
                .HasColumnName("BTW_NR");
            entity.Property(e => e.DateNaissance)
                .HasColumnType("DATE")
                .HasColumnName("DATE_NAISSANCE");
            entity.Property(e => e.Email)
                .HasMaxLength(80)
                .IsUnicode(false)
                .HasColumnName("EMAIL");
            entity.Property(e => e.Evaluatiecode)
                .HasPrecision(10)
                .HasColumnName("EVALUATIECODE");
            entity.Property(e => e.Fax)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("FAX");
            entity.Property(e => e.Fedcom)
                .HasPrecision(10)
                .HasColumnName("FEDCOM");
            entity.Property(e => e.Fedcomnummer)
                .HasPrecision(10)
                .HasColumnName("FEDCOMNUMMER");
            entity.Property(e => e.Genre)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("GENRE");
            entity.Property(e => e.Gsm)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("GSM");
            entity.Property(e => e.Herkomst)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("HERKOMST");
            entity.Property(e => e.Iban)
                .HasMaxLength(34)
                .IsUnicode(false)
                .HasColumnName("IBAN");
            entity.Property(e => e.Iscce)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("0")
                .HasColumnName("ISCCE");
            entity.Property(e => e.LangueAdministrative)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("LANGUE_ADMINISTRATIVE");
            entity.Property(e => e.Nationaliteit)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NATIONALITEIT");
            entity.Property(e => e.Nom)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NOM");
            entity.Property(e => e.Ondernemingsnummer)
                .HasPrecision(10)
                .HasColumnName("ONDERNEMINGSNUMMER");
            entity.Property(e => e.OpenbareVeiligheid)
                .HasPrecision(10)
                .HasColumnName("OPENBARE_VEILIGHEID");
            entity.Property(e => e.Postid)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("POSTID");
            entity.Property(e => e.Prenom)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRENOM");
            entity.Property(e => e.Remarque)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("REMARQUE");
            entity.Property(e => e.Rijksregisternr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("RIJKSREGISTERNR");
            entity.Property(e => e.Rue)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("RUE");
            entity.Property(e => e.Taalrol)
                .HasPrecision(10)
                .HasColumnName("TAALROL");
            entity.Property(e => e.Tel)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TEL");
            entity.Property(e => e.Telbis)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TELBIS");
            entity.Property(e => e.Tva)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("TVA");
            entity.Property(e => e.Vestigingsnummer)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("VESTIGINGSNUMMER");
        });

        modelBuilder.Entity<Tolkindispo>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("TOLKINDISPO");

            entity.Property(e => e.Commentaire)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("COMMENTAIRE");
            entity.Property(e => e.Datecreate)
                .HasColumnType("DATE")
                .HasColumnName("DATECREATE");
            entity.Property(e => e.Datemodif)
                .HasColumnType("DATE")
                .HasColumnName("DATEMODIF");
            entity.Property(e => e.Endindispo)
                .HasColumnType("DATE")
                .HasColumnName("ENDINDISPO");
            entity.Property(e => e.IdIndispo)
                .HasPrecision(5)
                .HasColumnName("ID_INDISPO");
            entity.Property(e => e.Motifindispo)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("MOTIFINDISPO");
            entity.Property(e => e.Startindispo)
                .HasColumnType("DATE")
                .HasColumnName("STARTINDISPO");
            entity.Property(e => e.Tolkcode)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("TOLKCODE");
            entity.Property(e => e.Usercreate)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("USERCREATE");
            entity.Property(e => e.Usermodif)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("USERMODIF");
        });

        modelBuilder.Entity<Tolklink>(entity =>
        {
            entity.HasKey(e => e.IdTolklink).HasName("TOLKLINK_PK");

            entity.ToTable("TOLKLINK");

            entity.HasIndex(e => new { e.NrAffAudience, e.IdTolklink }, "INDEX1").IsUnique();

            entity.Property(e => e.IdTolklink)
                .HasColumnType("NUMBER")
                .HasColumnName("ID_TOLKLINK");
            entity.Property(e => e.Datecreate)
                .HasColumnType("DATE")
                .HasColumnName("DATECREATE");
            entity.Property(e => e.Datemodif)
                .HasColumnType("DATE")
                .HasColumnName("DATEMODIF");
            entity.Property(e => e.Datesupp)
                .HasColumnType("DATE")
                .HasColumnName("DATESUPP");
            entity.Property(e => e.IdPrestation)
                .HasColumnType("NUMBER")
                .HasColumnName("ID_PRESTATION");
            entity.Property(e => e.NrAffAudience)
                .HasColumnType("NUMBER")
                .HasColumnName("NR_AFF_AUDIENCE");
            entity.Property(e => e.Tolkcode)
                .HasColumnType("NUMBER")
                .HasColumnName("TOLKCODE");
            entity.Property(e => e.Usercreate)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("USERCREATE");

            entity.HasOne(d => d.IdPrestationNavigation).WithMany(p => p.Tolklinks)
                .HasForeignKey(d => d.IdPrestation)
                .HasConstraintName("FK_TOLKLINK_PRESTATION");
        });

        modelBuilder.Entity<ToutLesTolk>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("TOUT_LES_TOLK");

            entity.Property(e => e.Tolkcode)
                .HasPrecision(10)
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<VerificationConvoc>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VERIFICATION_CONVOC");

            entity.Property(e => e.DateConvoc)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasColumnName("DATE_CONVOC");
            entity.Property(e => e.Login)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("LOGIN");
            entity.Property(e => e.TypeConvoc)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("TYPE_CONVOC");
        });

        modelBuilder.Entity<View1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VIEW1");

            entity.Property(e => e.IdTolklink)
                .HasColumnType("NUMBER")
                .HasColumnName("ID_TOLKLINK");
        });

        modelBuilder.Entity<View3>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VIEW3");

            entity.Property(e => e.Tolkcode)
                .HasPrecision(6)
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<VueAlerteAudienceSupp>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VUE_ALERTE_AUDIENCE_SUPP");

            entity.Property(e => e.Bge)
                .HasMaxLength(62)
                .IsUnicode(false)
                .HasColumnName("BGE");
        });

        modelBuilder.Entity<VueCalendarAnn>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VUE_CALENDAR_ANN");

            entity.Property(e => e.DateAudience)
                .HasColumnType("DATE")
                .HasColumnName("DATE_AUDIENCE");
            entity.Property(e => e.HeureAudience)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("HEURE_AUDIENCE");
            entity.Property(e => e.IdAffAudience)
                .HasColumnType("NUMBER")
                .HasColumnName("ID_AFF_AUDIENCE");
            entity.Property(e => e.LangueCgoe)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LANGUE_CGOE");
            entity.Property(e => e.LangueRequete)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LANGUE_REQUETE");
            entity.Property(e => e.LangueRole)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("LANGUE_ROLE");
            entity.Property(e => e.LibelleFr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LIBELLE_FR");
            entity.Property(e => e.Nom)
                .HasMaxLength(128)
                .IsUnicode(false)
                .HasColumnName("NOM");
            entity.Property(e => e.NroRoleGen)
                .HasColumnType("NUMBER")
                .HasColumnName("NRO_ROLE_GEN");
            entity.Property(e => e.Proc)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("PROC");
            entity.Property(e => e.SalleAudience)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("SALLE_AUDIENCE");
            entity.Property(e => e.Tolkcode)
                .HasColumnType("NUMBER")
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<VueCalendarVrmPc>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("Vue_CALENDAR_VRM_PC");

            entity.Property(e => e.DateAudience)
                .HasColumnType("DATE")
                .HasColumnName("DATE_AUDIENCE");
            entity.Property(e => e.HeureAudience)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("HEURE_AUDIENCE");
            entity.Property(e => e.IdAffAudience)
                .HasColumnType("NUMBER")
                .HasColumnName("ID_AFF_AUDIENCE");
            entity.Property(e => e.LangueCgoe)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LANGUE_CGOE");
            entity.Property(e => e.LangueRequete)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LANGUE_REQUETE");
            entity.Property(e => e.LangueRole)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("LANGUE_ROLE");
            entity.Property(e => e.LibelleFr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LIBELLE_FR");
            entity.Property(e => e.Nom)
                .HasMaxLength(128)
                .IsUnicode(false)
                .HasColumnName("NOM");
            entity.Property(e => e.NroRoleGen)
                .HasColumnType("NUMBER")
                .HasColumnName("NRO_ROLE_GEN");
            entity.Property(e => e.Proc)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("PROC");
            entity.Property(e => e.SalleAudience)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("SALLE_AUDIENCE");
            entity.Property(e => e.Tolkcode)
                .HasColumnType("NUMBER")
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<VueLangue>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VUE_LANGUE");

            entity.Property(e => e.CodeIso)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("CODE_ISO");
            entity.Property(e => e.Idlangue)
                .HasColumnType("NUMBER")
                .HasColumnName("IDLANGUE");
            entity.Property(e => e.IslangueDestination)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ISLANGUE_DESTINATION");
            entity.Property(e => e.LibelleFr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LIBELLE_FR");
            entity.Property(e => e.LibelleNl)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LIBELLE_NL");
        });

        modelBuilder.Entity<VuePrestationManquante>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VUE_PRESTATION_MANQUANTE");

            entity.Property(e => e.DateAudience)
                .HasColumnType("DATE")
                .HasColumnName("DATE_AUDIENCE");
            entity.Property(e => e.Lsb)
                .HasMaxLength(71)
                .IsUnicode(false)
                .HasColumnName("LSB");
            entity.Property(e => e.Tolkcode)
                .HasColumnType("NUMBER")
                .HasColumnName("TOLKCODE");
        });

        modelBuilder.Entity<VueTva>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VUE_TVA");

            entity.Property(e => e.EndDate)
                .HasColumnType("DATE")
                .HasColumnName("END_DATE");
            entity.Property(e => e.StartDate)
                .HasColumnType("DATE")
                .HasColumnName("START_DATE");
            entity.Property(e => e.Tolkcode)
                .HasPrecision(10)
                .HasColumnName("TOLKCODE");
            entity.Property(e => e.TypeStatut)
                .HasMaxLength(32)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("TYPE_STATUT");
        });

        // Séquences Oracle
        modelBuilder.HasSequence<int>("ID_PRESTATION_AUTO");
        modelBuilder.HasSequence<int>("NR_AUTO_PAIEMENT");

        // TOLKLINK
        modelBuilder.Entity<Tolklink>(entity =>
        {
            entity.ToTable("TOLKLINK");
            entity.HasKey(e => e.IdTolklink);
            entity.Property(e => e.IdTolklink).HasColumnName("ID_TOLKLINK");
            entity.Property(e => e.NrAffAudience).HasColumnName("NR_AFF_AUDIENCE");
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE").HasMaxLength(5).IsRequired();
            entity.Property(e => e.Datesupp).HasColumnName("DATESUPP").HasColumnType("DATE");
            entity.Property(e => e.IdPrestation).HasColumnName("ID_PRESTATION");
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
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE").HasMaxLength(5).IsRequired();
            entity.Property(e => e.DatePrestation).HasColumnName("DATE_PRESTATION").HasColumnType("DATE");
            entity.Property(e => e.Startheure).HasColumnName("STARTHEURE").HasColumnType("TIMESTAMP(6)");
            entity.Property(e => e.Endheure).HasColumnName("ENDHEURE").HasColumnType("TIMESTAMP(6)");
            entity.Property(e => e.UserCreate).HasColumnName("USER_CREATE").HasMaxLength(50);
            entity.Property(e => e.IdPaiement).HasColumnName("ID_PAIEMENT");
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
            entity.Property(e => e.Tolkcode).HasColumnName("TOLKCODE").HasMaxLength(5).IsRequired();
            entity.Property(e => e.DatePrestation).HasColumnName("DATE_PRESTATION").HasColumnType("DATE");
            entity.Property(e => e.Montant).HasColumnName("MONTANT").HasColumnType("NUMBER(10,2)");
            entity.Property(e => e.Transport).HasColumnName("TRANSPORT").HasColumnType("NUMBER(10,2)");
            entity.Property(e => e.Total).HasColumnName("TOTAL").HasColumnType("NUMBER(10,2)");
            entity.Property(e => e.MontantTva).HasColumnName("MONTANT_TVA").HasColumnType("NUMBER(10,2)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
