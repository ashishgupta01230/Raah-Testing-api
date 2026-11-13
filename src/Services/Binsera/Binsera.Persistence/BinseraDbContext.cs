using Binsera.Persistence.Extensions;
using Binsera.Persistence.Interceptors;
using HautSphere.BuildingBlocks.Domain;
using HautSphere.BuildingBlocks.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

namespace Binsera.Persistence
{
    public class BinseraDbContext : DbContext
    {
        private long _currentIdentityUserId = 1;
        private readonly ICurrentUserService _currentUserService;
        public BinseraDbContext(DbContextOptions<BinseraDbContext> dbContext, ICurrentUserService currentUserService) : base(dbContext)
        {
            _currentUserService = currentUserService;
            _currentIdentityUserId = currentUserService.UserId;
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entityTypes = modelBuilder.Model.GetEntityTypes();
            modelBuilder.HasDefaultSchema("dbo");
            foreach (var entityType in entityTypes)
            {
                if (typeof(DomainBase).IsAssignableFrom(entityType.ClrType))
                {
                    // Discriminator column
                    modelBuilder.Entity(entityType.ClrType).HasDiscriminator("IsDeleted", typeof(bool)).HasValue(false);
                    // Shadow Property       
                    modelBuilder.Entity(entityType.ClrType).Property(typeof(bool), "IsDeleted").IsRequired(true);

                    modelBuilder.Entity(entityType.ClrType).Property(typeof(bool), "IsDeleted").Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Save);

                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var body = Expression.Equal(Expression.Call(typeof(EF), nameof(EF.Property), new[] { typeof(bool) }, parameter, Expression.Constant("IsDeleted")), Expression.Constant(false));

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
                }
            }
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BinseraDbContext).Assembly);
            modelBuilder.AddViews();
            modelBuilder.HasAnnotation("Relational:JsonColumnMappingEnabled", false);
            base.OnModelCreating(modelBuilder);
        }
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
            base.ConfigureConventions(configurationBuilder);

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(new MemberCodeGeneratorInterceptor());
            //optionsBuilder.AddInterceptors(new RemoveOutputInterceptor());
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
        }
        public void SetCurrentUser(long currentSessionUserId)
        {
            _currentIdentityUserId = currentSessionUserId;
        }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ChangeTracker.HandleAuditBeforeSave(_currentIdentityUserId, DateTime.Now);
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
