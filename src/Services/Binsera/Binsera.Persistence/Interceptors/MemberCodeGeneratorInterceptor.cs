using Binsera.Core;
using Binsera.Core.Members.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Binsera.Persistence.Interceptors
{
    public class MemberCodeGeneratorInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

            var newMembers = context.ChangeTracker.Entries<Member>()
                .Where(e => e.State == EntityState.Added);

            foreach (var entry in newMembers)
            {
                if (string.IsNullOrEmpty(entry.Entity.LoginID))
                {
                    var prefix = CompanySetting.Default.MemberCodeSettings.Prefix;

                    // Existing rows can carry legacy/manually-seeded LoginIDs (e.g. "rootid",
                    // "rootid3246") that don't follow the "<prefix><digits>" pattern. Ordering
                    // by LoginID alone can surface one of those as the "max" and break Substring
                    // + int.Parse, so pull a batch of recent codes and pick the first one that
                    // actually matches the configured prefix format.
                    var recentCodes = context.Set<Member>()
                        .Where(m => m.Id > 3 && m.LoginID != null)
                        .OrderByDescending(m => m.Id)
                        .Select(m => m.LoginID)
                        .Take(50)
                        .ToList();

                    int? number = null;
                    foreach (var code in recentCodes)
                    {
                        if (code.Length > prefix.Length &&
                            code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                            int.TryParse(code.Substring(prefix.Length), out var parsed))
                        {
                            number = parsed;
                            break;
                        }
                    }

                    var nextNumber = number.HasValue ? number.Value + 1 : 1;
                    var padding = CompanySetting.Default.MemberCodeSettings.Length;
                    string newCode = $"{prefix}{nextNumber.ToString().PadLeft(padding, '0')}";

                    entry.Entity.LoginID = newCode;
                }
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null) return await base.SavedChangesAsync(eventData, result, cancellationToken);

            // RegNo always mirrors the identity Id. A SQL Server trigger on member_Master used to
            // set it on INSERT; that trigger was removed, so every member registered since came out
            // with RegNo NULL -- which silently breaks order approval, sponsor lookup and every
            // wallet write, since all of those match on RegNo rather than Id.
            //
            // It can only be filled after the INSERT has run, because Id is database-generated.
            // Any member row with a NULL RegNo is broken by definition, so this repairs those too
            // rather than only the row just inserted.
            var missingRegNo = context.ChangeTracker.Entries<Member>()
                .Where(e => e.Entity.RegNo == null && e.Entity.Id > 0)
                .ToList();

            if (missingRegNo.Count == 0)
                return await base.SavedChangesAsync(eventData, result, cancellationToken);

            foreach (var entry in missingRegNo)
                entry.Property(nameof(Member.RegNo)).CurrentValue = entry.Entity.Id;

            // Re-entrant by design: this save runs the interceptor again, but by then no tracked
            // member has a NULL RegNo, so the second pass exits above instead of looping.
            await context.SaveChangesAsync(cancellationToken);

            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }
    }

}
