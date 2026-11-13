namespace Binsera.Core.Pool.Models
{
    /// <summary>
    /// The shape the existing vis-network tree component already consumes
    /// (libs/ui/src/lib/services/mlm-tree.service.ts). Kept field-for-field so that component can
    /// render the pool tree without being rewritten -- it is a far better visualisation than
    /// anything worth hand-rolling, it just needed a data source that isn't the empty Neo4j graph.
    /// </summary>
    public class BinaryTreeNode
    {
        public string Id { get; set; } = string.Empty;
        public string? RegistrationNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Role { get; set; }
        public string? Package { get; set; }
        public string? Image { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ParentId { get; set; }

        /// <summary>"left" / "right", or null for the root of the requested view.</summary>
        public string? Placement { get; set; }

        /// <summary>The member's pool level, which is what the node badge shows.</summary>
        public int Level { get; set; }

        /// <summary>Lifetime Main-wallet credits. What the hover card calls Income.</summary>
        public decimal Earnings { get; set; }

        public DateTime? JoinedOn { get; set; }

        public bool IsEmpty { get; set; }
        public bool HasChildren { get; set; }
        public bool LeftChildExists { get; set; }
        public bool RightChildExists { get; set; }
        public int ChildrenCount { get; set; }
    }

    public sealed class BinaryTreeResponse
    {
        public BinaryTreeNode? Root { get; set; }
        public List<BinaryTreeNode> Members { get; set; } = new();
        public BinaryTreeMeta Meta { get; set; } = new();
    }

    public sealed class BinaryTreeMeta
    {
        public long RootId { get; set; }
        public int Depth { get; set; }
        public int TotalMembers { get; set; }
        public int Returned { get; set; }
        public int MaxDepth { get; set; }
        public int RootTotal { get; set; }
    }

    public sealed class BinarySubtreeResponse
    {
        public long ParentId { get; set; }
        public List<BinaryTreeNode> Members { get; set; } = new();
        public int Depth { get; set; }
        public int Returned { get; set; }
    }

    /// <summary>One node plus its upline path, for the tree's detail panel.</summary>
    public sealed class BinaryMemberDetailResponse : BinaryTreeNode
    {
        public List<BinaryUplineEntry> Upline { get; set; } = new();
    }

    public sealed class BinaryUplineEntry
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? RegistrationNumber { get; set; }
    }

    /// <summary>
    /// The breadcrumb path. For a plain member it never climbs above themselves -- the pool's own
    /// rule is that a member may not see who sits above them.
    /// </summary>
    public sealed class BinaryUplineResponse
    {
        public List<BinaryUplineEntry> Path { get; set; } = new();
    }

    public sealed class BinarySearchResponse
    {
        public List<BinaryTreeNode> Members { get; set; } = new();
        public BinaryTreeMeta Meta { get; set; } = new();
    }

    // No stats model: MlmTreeService exposes getStats, but MlmTreeVisComponent never calls it, so
    // there is nothing to serve it to.
}
