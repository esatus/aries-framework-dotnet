using System.Threading.Tasks;

namespace Hyperledger.Aries.Features.IssueCredential
{
    public interface ILedgerQueueService
    {
        public void AddToQueue(string issuerDid, string credentialId, string revocRegistryDeltaJson);
        public Task<bool> RunQueueAsync();
    }
}
