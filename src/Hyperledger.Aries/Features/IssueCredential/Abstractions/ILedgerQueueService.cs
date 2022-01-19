using System.Threading.Tasks;

namespace Hyperledger.Aries.Features.IssueCredential
{
    public interface ILedgerQueueService
    {
        /// <summary>
        /// Adds a new LedgerQueueObject to queue.
        /// </summary>
        /// <param name="ledgerQueueObject"></param>
        /// <returns></returns>
        public Task AddToQueueAsync(LedgerQueueRecord ledgerQueueObject);

        /// <summary>
        /// Tries to write every object in the queue to the ledger.
        /// </summary>
        /// <returns>Returns true if succeed, otherwise false.</returns>
        public Task<bool> RunQueueAsync();
    }
}
