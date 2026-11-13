using Binsera.Core.Members.Domain;
using Binsera.Core.Pool;
using Binsera.Core.Pool.Domain;
using Binsera.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Application.Pool.Services
{
    /// <summary>
    /// Everything the cascade needs about the tree, loaded up front so the cascade itself stays a
    /// synchronous pure function.
    ///
    /// Pre-loading is possible because a cascade can only ever climb: starting from the joining
    /// member it moves to an ancestor, then that ancestor's ancestor, and so on. So the complete
    /// set of members it can touch is the joiner plus its ancestors -- one per level of depth,
    /// which is a handful of rows even for a very large tree.
    ///
    /// Level state is kept in the legacy Member_Level table, one row per (member, level).
    /// <c>mlActive</c> means "this level is complete" -- the column predates the pool and has no
    /// other meaning here.
    /// </summary>
    internal sealed class PlacementContext
    {
        private readonly BinseraDbContext _dbContext;
        private readonly Dictionary<long, long> _regNoByPosition = [];
        private readonly Dictionary<int, long> _seedRegNoByIndex = [];
        private readonly Dictionary<long, PoolMemberState> _statesByPosition = [];
        private readonly Dictionary<(long RegNo, int LevelNo), MemberLevel> _levelRows = [];

        private PlacementContext(BinseraDbContext dbContext) => _dbContext = dbContext;

        public static async Task<PlacementContext> CreateAsync(
            BinseraDbContext dbContext, long joiningPosition, CancellationToken cancellationToken)
        {
            var context = new PlacementContext(dbContext);

            var seeds = await dbContext.Set<PoolSeedAccount>().AsNoTracking().ToListAsync(cancellationToken);
            foreach (var seed in seeds)
                context._seedRegNoByIndex[seed.SeedIndex] = seed.MemberRegNo;

            // The joiner is not placed yet, so only its ancestors exist to load.
            var ancestorPositions = new List<long>();
            for (var position = joiningPosition >> 1; position >= PoolTreePosition.RootPosition; position >>= 1)
                ancestorPositions.Add(position);

            if (ancestorPositions.Count > 0)
            {
                var ancestors = await dbContext.Set<Member>()
                    .Where(x => x.PPositionM != null && ancestorPositions.Contains(x.PPositionM.Value))
                    .Select(x => new { Position = x.PPositionM!.Value, RegNo = x.RegNo!.Value })
                    .ToListAsync(cancellationToken);

                foreach (var ancestor in ancestors)
                    context._regNoByPosition[ancestor.Position] = ancestor.RegNo;

                var regNos = ancestors.Select(x => x.RegNo).ToList();

                var levelRows = await dbContext.Set<MemberLevel>()
                    .Where(x => regNos.Contains(x.RegNo))
                    .ToListAsync(cancellationToken);

                foreach (var row in levelRows)
                    context._levelRows[(row.RegNo, row.Levels)] = row;
            }

            return context;
        }

        /// <summary>Registers the member being placed, once its position is known.</summary>
        public void Register(long position, long regNo) => _regNoByPosition[position] = regNo;

        public long? RegNoAt(long position)
            => _regNoByPosition.TryGetValue(position, out var regNo) ? regNo : null;

        public long RequireRegNoAt(long position)
            => RegNoAt(position) ?? throw new InvalidOperationException(
                $"Pool position {position} has no member; the tree is not contiguous.");

        public long SeedRegNo(int seedIndex)
            => _seedRegNoByIndex.TryGetValue(seedIndex, out var regNo)
                ? regNo
                : throw new InvalidOperationException(
                    $"Seed account X{seedIndex} is missing. Run SeedGlobalAutoPoolSeedAccounts.sql.");

        /// <summary>
        /// The cascade's view of a member: which level they are on, and how full it is. Derived
        /// from their Member_Level rows -- a member who has completed levels 0 and 1 is on level 2.
        /// </summary>
        public PoolMemberState StateOf(long position)
        {
            if (_statesByPosition.TryGetValue(position, out var cached))
                return cached;

            var state = new PoolMemberState();

            if (RegNoAt(position) is long regNo)
            {
                var rows = _levelRows.Where(x => x.Key.RegNo == regNo).ToList();

                var completed = rows.Where(x => x.Value.MlActive == true).Select(x => x.Key.LevelNo).ToList();
                state.CurrentLevel = completed.Count > 0 ? completed.Max() + 1 : 0;

                state.ContributionCount = rows
                    .Where(x => x.Key.LevelNo == state.CurrentLevel)
                    .Select(x => x.Value.TlCount)
                    .FirstOrDefault();
            }

            _statesByPosition[position] = state;
            return state;
        }

        public void BumpLevelCounter(long regNo, int levelNo)
        {
            var row = LevelRow(regNo, levelNo);
            row.RecordContribution();
        }

        public void RecordLevelIncome(long regNo, int levelNo, decimal amount)
        {
            var row = LevelRow(regNo, levelNo);
            row.RecordIncome(amount);
        }

        public void MarkLevelComplete(long regNo, int levelNo)
        {
            var row = LevelRow(regNo, levelNo);
            row.MarkComplete();
        }

        private MemberLevel LevelRow(long regNo, int levelNo)
        {
            if (_levelRows.TryGetValue((regNo, levelNo), out var existing))
                return existing;

            var created = MemberLevel.StartLevel(regNo, levelNo);
            _dbContext.Set<MemberLevel>().Add(created);
            _levelRows[(regNo, levelNo)] = created;

            return created;
        }
    }
}
