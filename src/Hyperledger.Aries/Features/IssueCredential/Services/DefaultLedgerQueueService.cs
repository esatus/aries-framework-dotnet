using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Ledger;
using Hyperledger.Aries.Payments;
using Hyperledger.Aries.Storage;
using Hyperledger.Indy;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Hyperledger.Aries.Features.IssueCredential
{
    public class DefaultLedgerQueueService : ILedgerQueueService
    {
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

        public async Task AddToQueueAsync(LedgerQueueRecord ledgerQueueObject)
        {
            IAgentContext agentContext = await AgentProvider.GetContextAsync();
            await RecordService.AddAsync(agentContext.Wallet, ledgerQueueObject);
        }

        public async Task<bool> RunQueueAsync()
        {
            IAgentContext agentContext = await AgentProvider.GetContextAsync();
            TransactionCost paymentInfo = await PaymentService.GetTransactionCostAsync(agentContext, TransactionTypes.REVOC_REG_ENTRY);

            List<LedgerQueueRecord> ledgerQueueObjects = await RecordService.SearchAsync<LedgerQueueRecord>(agentContext.Wallet);
            ledgerQueueObjects = ledgerQueueObjects.FindAll(x => x.QueueStatus == LedgerQueueRecord.Status.Pending).OrderBy(x => x.CreatedAtUtc).ToList<LedgerQueueRecord>();

            foreach(LedgerQueueRecord ledgerQueueObject in ledgerQueueObjects)
            {
                try
                {
                    var res = await LedgerService.SignAndSubmitAsync(agentContext, ledgerQueueObject.IssuerDID, ledgerQueueObject.Request, paymentInfo);

                    // Trigger credential record to revoke
                    var credentialRecord = await RecordService.GetAsync<CredentialRecord>(agentContext.Wallet, ledgerQueueObject.ObjectId);
                    await credentialRecord.TriggerAsync(CredentialTrigger.Revoke);
                    await RecordService.UpdateAsync(agentContext.Wallet, credentialRecord);

                    //Remove from queue
                    await RecordService.DeleteAsync<LedgerQueueRecord>(agentContext.Wallet, ledgerQueueObject.Id);
                }
                catch (IndyException ex) when (ex.SdkErrorCode == 307) // From indy sdk: Timeout for action ->  PoolLedgerTimeout = 307
                {
                    return false;
                }
                catch // If the ledger was reachable but the operation was rejected, change status to failed
                {
                    ledgerQueueObject.QueueStatus = LedgerQueueRecord.Status.Failed;
                    await RecordService.UpdateAsync(agentContext.Wallet, ledgerQueueObject);
                }
            }

            return true;
        }
    }
}
