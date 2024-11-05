using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Data;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Project.Database.Models;
using Microsoft.Extensions.Configuration;

namespace Project.Database.EFDbContext
{
    public class AppDbContext : DbContext
    {
        private readonly IConfiguration _configuration;
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration)
            : base(options)
        {
        }

        public virtual DbSet<Audit> Audits { get; set; }

        public virtual DbSet<AuditEntry> AuditEntries { get; set; }

        //public virtual DbSet<Menu> Menus { get; set; }

        //public virtual DbSet<Role> Roles { get; set; }

        //public virtual DbSet<RoleMenu> RoleMenus { get; set; }

        public virtual DbSet<User> Users { get; set; }

        //public virtual DbSet<UserRole> UserRoles { get; set; }

        //public virtual DbSet<UserToken> UserTokens { get; set; }

        public virtual DbSet<Customer> Customers { get; set; }
        //public virtual DbSet<RoleGrant> RoleGrants { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = _configuration.GetConnectionString("Context");
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        private List<EntityAuditInformation> BeforeSaveChanges()
        {
            List<EntityAuditInformation> entityAuditInformation = new();

            foreach (EntityEntry entityEntry in ChangeTracker.Entries())
            {
                dynamic entity = entityEntry.Entity;
                bool isAdd = entityEntry.State == EntityState.Added;
                List<AuditEntry> changes = new();
                foreach (PropertyEntry property in entityEntry.Properties)
                {
                    if ((isAdd && property.CurrentValue != null) || (property.IsModified && !Object.Equals(property.CurrentValue, property.OriginalValue)))
                    {
                        if (property.Metadata.Name != "Id") // Do not track primary key values (never going to change)
                        {
                            changes.Add(new AuditEntry()
                            {
                                FieldName = property.Metadata.Name,
                                NewValue = property.CurrentValue?.ToString(),
                                OldValue = isAdd ? null : property.OriginalValue?.ToString()
                            });
                        }
                    }
                }
                PropertyEntry? IsDeletedPropertyEntry = entityEntry.Properties.FirstOrDefault(x => x.Metadata.Name == nameof(entity.IsDeleted));
                if (IsDeletedPropertyEntry != null)
                {
                    entityAuditInformation.Add(new EntityAuditInformation()
                    {
                        Entity = entity,
                        TableName = entityEntry.Metadata?.GetTableName() ?? entity.GetType().Name,
                        State = entityEntry.State,
                        IsDeleteChanged = IsDeletedPropertyEntry != null && !object.Equals(IsDeletedPropertyEntry.CurrentValue, IsDeletedPropertyEntry.OriginalValue),
                        Changes = changes
                    });
                }
            }

            return entityAuditInformation;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var entityAuditInformation = BeforeSaveChanges();
            int returnValue = 0;
            var userId = await Users.Select(x => x.Id).FirstOrDefaultAsync();
            returnValue = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            if (returnValue > 0)
            {
                foreach (EntityAuditInformation item in entityAuditInformation)
                {
                    dynamic entity = item.Entity;
                    List<AuditEntry> changes = item.Changes;
                    if (changes != null && changes.Any())
                    {
                        Audit audit = new()
                        {
                            TableName = item.TableName,
                            RecordId = entity.Id,
                            ChangeDate = DateTime.Now,
                            Operation = item.OperationType,
                            AuditEntries = changes,
                            ChangedById = (int)userId // LoggedIn user Id
                        };
                        _ = await AddAsync(audit, cancellationToken);
                    }
                }

                //Save audit data
                await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
            return returnValue;
        }
    }    
}
