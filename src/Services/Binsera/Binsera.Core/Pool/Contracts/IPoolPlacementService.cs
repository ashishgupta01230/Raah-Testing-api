namespace Binsera.Core.Pool.Contracts
{
    public interface IPoolPlacementService
    {
        /// <summary>
        /// Places a paid-up member into the pool and runs the money cascade that follows.
        /// Does nothing if the member is not active or is already placed, so a replayed message
        /// is safe.
        /// </summary>
        Task PlaceMemberAsync(long memberId, CancellationToken cancellationToken = default);
    }
}
