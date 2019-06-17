using System.Threading.Tasks;

namespace Orleans.Indexing.Tests.Grains.Players
{
    public class TransactionalPlayerRoot : Grain, ITransactionalPlayerGrainRoot
    {
        public async Task InsertAsync(int score, string location, int count, int abortAfter = -1)
        {
            for (var ii = 0; ii < count; ++ii)
            {
                var grain = base.GrainFactory.GetGrain<ITransactionalPlayerGrain>(ii);
                await grain.SetScore(score);
                await grain.SetLocation(location);

                if (abortAfter > 0 && ii == abortAfter)
                {
                    throw new TestAbortTransactionException($"InsertAsync aborted as requested after {abortAfter} insertions.");
                }
            }
        }

        public async Task UpdateAsync(int fromScore, int toScore, string fromLocation, string toLocation, int count, int abortAfter = -1)
        {
            for (var ii = 0; ii < count; ++ii)
            {
                var grain = base.GrainFactory.GetGrain<ITransactionalPlayerGrain>(ii);
                var oldScore = await grain.GetScore();
                if (oldScore != fromScore)
                {
                    throw new TestFailedException($"OldScore {oldScore} != fromScore {fromScore}.");
                }
                var oldLocation = await grain.GetLocation();
                if (oldLocation != fromLocation)
                {
                    throw new TestFailedException($"OldLocation {oldLocation} != fromLocation {fromLocation}.");
                }

                await grain.SetScore(toScore);
                await grain.SetLocation(toLocation);

                if (abortAfter > 0 && ii == abortAfter)
                {
                    throw new TestAbortTransactionException($"UpdateAsync aborted as requested after {abortAfter} insertions.");
                }
            }
        }
    }
}
