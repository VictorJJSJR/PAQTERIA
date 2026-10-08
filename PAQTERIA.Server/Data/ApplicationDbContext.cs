using Microsoft.EntityFrameworkCore;
using PAQTERIA.Server.Models;

namespace PAQTERIA.Server.Data;

/// <summary>
/// Mapea las tablas que ya existen en el script de PAQTERIA. Este contexto no
/// administra migraciones: los cambios requeridos por la aplicación están en
/// database/paqteria-app-setup.sql y se aplican de forma explícita.
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<AppRole> Roles => Set<AppRole>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<DistributionCenter> DistributionCenters => Set<DistributionCenter>();
    public DbSet<DeliveryPackage> Packages => Set<DeliveryPackage>();
    public DbSet<TrackingHistory> TrackingHistory => Set<TrackingHistory>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<DriverShift> DriverShifts => Set<DriverShift>();
    public DbSet<VehicleUnit> Units => Set<VehicleUnit>();
    public DbSet<DatabaseChange> DatabaseChanges => Set<DatabaseChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.ToTable("ROLES", "dbo", table => table.HasTrigger("TR_ROLES_APP_CAMBIOS"));
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Id).HasColumnName("id_rol");
            entity.Property(role => role.Name).HasColumnName("nombre").HasMaxLength(100).IsRequired();
            entity.HasIndex(role => role.Name).IsUnique();
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("USUARIOS", "dbo", table => table.HasTrigger("TR_USUARIOS_APP_CAMBIOS"));
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("id_usuario");
            entity.Property(user => user.RoleId).HasColumnName("id_rol");
            entity.Property(user => user.Name).HasColumnName("nombre").HasMaxLength(150).IsRequired();
            entity.Property(user => user.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
            entity.Property(user => user.Phone).HasColumnName("telefono").HasMaxLength(20);
            entity.Property(user => user.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            entity.Property(user => user.RegisteredAt).HasColumnName("fecha_registro").HasColumnType("datetime2");
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasOne(user => user.Role).WithMany().HasForeignKey(user => user.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DistributionCenter>(entity =>
        {
            entity.ToTable("CENTROS_DISTRIBUCION", "dbo", table => table.HasTrigger("TR_CENTROS_DISTRIBUCION_APP_CAMBIOS"));
            entity.HasKey(center => center.Id);
            entity.Property(center => center.Id).HasColumnName("id_centro");
            entity.Property(center => center.Name).HasColumnName("nombre").HasMaxLength(150).IsRequired();
            entity.Property(center => center.City).HasColumnName("ciudad").HasMaxLength(100).IsRequired();
            entity.Property(center => center.Address).HasColumnName("direccion").HasMaxLength(255).IsRequired();
        });

        modelBuilder.Entity<DeliveryPackage>(entity =>
        {
            entity.ToTable("PAQUETES", "dbo", table => table.HasTrigger("TR_PAQUETES_APP_CAMBIOS"));
            entity.HasKey(package => package.Id);
            entity.Property(package => package.Id).HasColumnName("id_paquete");
            entity.Property(package => package.TrackingNumber).HasColumnName("folio").HasMaxLength(50).IsRequired();
            entity.Property(package => package.ClientId).HasColumnName("id_cliente");
            entity.Property(package => package.SenderName).HasColumnName("nombre_remitente").HasMaxLength(150);
            entity.Property(package => package.OriginCenterId).HasColumnName("id_centro_origen");
            entity.Property(package => package.OriginAddress).HasColumnName("direccion_origen").HasMaxLength(255).IsRequired();
            entity.Property(package => package.DestinationAddress).HasColumnName("direccion_destino").HasMaxLength(255).IsRequired();
            entity.Property(package => package.DestinationCoordinates).HasColumnName("coordenadas_destino").HasMaxLength(100);
            entity.Property(package => package.WeightKg).HasColumnName("peso_kg").HasColumnType("decimal(10,2)");
            entity.Property(package => package.LabelSize).HasColumnName("tamano_etiqueta").HasMaxLength(50);
            entity.Property(package => package.IsPriority).HasColumnName("es_prioritario");
            entity.Property(package => package.IsFragile).HasColumnName("es_fragil");
            entity.Property(package => package.Status).HasColumnName("estado_actual").HasMaxLength(50).IsRequired();
            entity.Property(package => package.CreatedAt).HasColumnName("fecha_creacion").HasColumnType("datetime2");
            entity.Property(package => package.UpdatedAt).HasColumnName("fecha_actualizacion").HasColumnType("datetime2");
            entity.Property(package => package.RowVersion).HasColumnName("version_fila").IsRowVersion();
            entity.HasIndex(package => package.TrackingNumber).IsUnique();
            entity.HasOne(package => package.Client).WithMany().HasForeignKey(package => package.ClientId)
                .IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(package => package.OriginCenter).WithMany().HasForeignKey(package => package.OriginCenterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Incident>(entity =>
        {
            entity.ToTable("INCIDENCIAS_ENTREGA", "dbo", table => table.HasTrigger("TR_INCIDENCIAS_ENTREGA_APP_CAMBIOS"));
            entity.HasKey(incident => incident.Id);
            entity.Property(incident => incident.Id).HasColumnName("id_incidencia");
            entity.Property(incident => incident.PackageId).HasColumnName("id_paquete");
            entity.Property(incident => incident.DriverId).HasColumnName("id_repartidor");
            entity.Property(incident => incident.Title).HasColumnName("tipo_incidencia").HasMaxLength(100).IsRequired();
            entity.Property(incident => incident.Description).HasColumnName("comentario").HasColumnType("text");
            entity.Property(incident => incident.PhotoUrl).HasColumnName("url_foto_reporte").HasMaxLength(500);
            entity.Property(incident => incident.CreatedAt).HasColumnName("fecha_hora").HasColumnType("datetime2");
            entity.Property(incident => incident.Status).HasColumnName("estado").HasMaxLength(50).IsRequired();
            entity.Property(incident => incident.Severity).HasColumnName("severidad").HasMaxLength(20).IsRequired();
            entity.Property(incident => incident.UpdatedAt).HasColumnName("fecha_actualizacion").HasColumnType("datetime2");
            entity.Property(incident => incident.RowVersion).HasColumnName("version_fila").IsRowVersion();
            entity.HasOne(incident => incident.Package).WithMany().HasForeignKey(incident => incident.PackageId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(incident => incident.Driver).WithMany().HasForeignKey(incident => incident.DriverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TrackingHistory>(entity =>
        {
            entity.ToTable("HISTORIAL_SEGUIMIENTO", "dbo", table => table.HasTrigger("TR_HISTORIAL_SEGUIMIENTO_APP_CAMBIOS"));
            entity.HasKey(history => history.Id);
            entity.Property(history => history.Id).HasColumnName("id_historial");
            entity.Property(history => history.PackageId).HasColumnName("id_paquete");
            entity.Property(history => history.Title).HasColumnName("titulo").HasMaxLength(150).IsRequired();
            entity.Property(history => history.Description).HasColumnName("descripcion").HasColumnType("text");
            entity.Property(history => history.ChangedAt).HasColumnName("fecha_hora").HasColumnType("datetime2");
            entity.HasOne(history => history.Package).WithMany(package => package.TrackingHistory)
                .HasForeignKey(history => history.PackageId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DriverShift>(entity =>
        {
            entity.ToTable("TURNOS_REPARTIDOR", "dbo", table => table.HasTrigger("TR_TURNOS_REPARTIDOR_APP_CAMBIOS"));
            entity.HasKey(shift => shift.Id);
            entity.Property(shift => shift.Id).HasColumnName("id_turno");
            entity.Property(shift => shift.DriverId).HasColumnName("id_repartidor");
            entity.Property(shift => shift.UnitId).HasColumnName("id_unidad");
            entity.Property(shift => shift.ShiftDate).HasColumnName("fecha_turno").HasColumnType("date");
            entity.Property(shift => shift.Status).HasColumnName("estado_turno").HasMaxLength(50).IsRequired();
            entity.Property(shift => shift.TotalPackages).HasColumnName("total_paquetes");
            entity.Property(shift => shift.EstimatedHours).HasColumnName("tiempo_estimado_horas").HasColumnType("decimal(5,2)");
            entity.HasOne(shift => shift.Driver).WithMany().HasForeignKey(shift => shift.DriverId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(shift => shift.Unit).WithMany().HasForeignKey(shift => shift.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VehicleUnit>(entity =>
        {
            entity.ToTable("UNIDADES", "dbo", table => table.HasTrigger("TR_UNIDADES_APP_CAMBIOS"));
            entity.HasKey(unit => unit.Id);
            entity.Property(unit => unit.Id).HasColumnName("id_unidad");
            entity.Property(unit => unit.CenterId).HasColumnName("id_centro");
            entity.Property(unit => unit.Code).HasColumnName("codigo_unidad").HasMaxLength(50).IsRequired();
            entity.Property(unit => unit.LicensePlate).HasColumnName("placas").HasMaxLength(20).IsRequired();
            entity.HasOne(unit => unit.Center).WithMany().HasForeignKey(unit => unit.CenterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DatabaseChange>(entity =>
        {
            entity.ToTable("APP_CAMBIOS", "dbo");
            entity.HasKey(change => change.Id);
            entity.Property(change => change.Id).HasColumnName("id_cambio").UseIdentityColumn();
            entity.Property(change => change.EntityType).HasColumnName("entidad").HasMaxLength(32).IsRequired();
            entity.Property(change => change.ChangedAt).HasColumnName("fecha_utc").HasColumnType("datetime2(7)");
            entity.HasIndex(change => change.ChangedAt);
        });
    }
}
