using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Ledger;
using Hyperledger.Aries.Payments;
using Hyperledger.Aries.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hyperledger.Aries.Features.IssueCredential
{
    public class DefaultLedgerQueueService : ILedgerQueueService
    {
        private Queue<Dictionary<string, string>> _queue = new Queue<Dictionary<string, string>>(); 

        private ILedgerService LedgerService;
        private IAgentProvider AgentProvider;
        private IPaymentService PaymentService;
        private IWalletRecordService RecordService;

        public DefaultLedgerQueueService(
            ILedgerService ledgerService,
            IAgentProvider agentProvider,
            IPaymentService paymentService,
            IWalletRecordService recordService
            )
        {
            LedgerService = ledgerService;
            AgentProvider = agentProvider;
            PaymentService = paymentService;
            RecordService = recordService;
        }

        public void AddToQueue(string issuerDid, string credentialId, string revocRegistryDeltaJson)
        {
            Dictionary<string, string> entry = new Dictionary<string, string>();
            entry.Add("issuerDid", issuerDid);
            entry.Add("credentialId", credentialId);
            entry.Add("revocRegistryDeltaJson", revocRegistryDeltaJson);
            _queue.Enqueue(entry);
        }

        public async Task<bool> RunQueueAsync()
        {
            IAgentContext agentContext = await AgentProvider.GetContextAsync();
            TransactionCost paymentInfo = await PaymentService.GetTransactionCostAsync(agentContext, TransactionTypes.REVOC_REG_ENTRY);

            return await NextQueueEntry(agentContext, paymentInfo);
        }

        private async Task<bool> NextQueueEntry(IAgentContext agentContext, TransactionCost paymentInfo)
        {
            if(_queue.Count == 0)
            {
                // Queue is empty
                return true;
            }
            else
            {
                Dictionary<string, string> entry = _queue.Peek();
                var credentialRecord = await RecordService.GetAsync<CredentialRecord>(agentContext.Wallet, entry["credentialId"]);

                bool succeed = await LedgerService.SendRevocationRegistryEntryAsync(
                    agentContext,
                    entry["issuerDid"],
                    credentialRecord.RevocationRegistryId,
                    "CL_ACCUM",
                    entry["revocRegistryDeltaJson"],
                    paymentInfo
                    );

                if (succeed)
                {
                    // Trigger to Revoke
                    await credentialRecord.TriggerAsync(CredentialTrigger.Revoke);

                    if (paymentInfo != null)
                    {
                        await RecordService.UpdateAsync(agentContext.Wallet, paymentInfo.PaymentAddress);
                    }

                    // Update local credential record
                    await RecordService.UpdateAsync(agentContext.Wallet, credentialRecord);

                    _queue.Dequeue();

                    return await NextQueueEntry(agentContext, paymentInfo);
                }
                else
                {
                    return false;
                }
            }
            
        }
    }
}
