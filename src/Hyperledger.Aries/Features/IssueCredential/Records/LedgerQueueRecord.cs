using Hyperledger.Aries.Storage;
using System;
using System.Threading.Tasks;
using IndyLedger = Hyperledger.Indy.LedgerApi.Ledger;

namespace Hyperledger.Aries.Features.IssueCredential
{
    public class LedgerQueueRecord : RecordBase
    {
        public Type ReqType;
        public string IssuerDID;
        public string Request;
        public string ObjectId;

        public override string TypeName => "LedgerQueueRecord";

        public LedgerQueueRecord() { }

        public static async Task<LedgerQueueRecord> CreateRevocationQueueObject(string issuerDid, string revocationRecordId, string revocRegistryDeltaJson, string credentialId)
        {
            LedgerQueueRecord ledgerQueueObject = new LedgerQueueRecord();
            ledgerQueueObject.CreatedAtUtc = DateTime.UtcNow;
            ledgerQueueObject.Id = credentialId;

            ledgerQueueObject.ReqType = LedgerQueueRecord.Type.Revocation;
            ledgerQueueObject.Request = await IndyLedger.BuildRevocRegEntryRequestAsync(issuerDid, revocationRecordId, "CL_ACCUM", revocRegistryDeltaJson);
            ledgerQueueObject.IssuerDID = issuerDid;
            ledgerQueueObject.ObjectId = credentialId;           


            return ledgerQueueObject;
        }

        public enum Type
        {
            Revocation,
            SchemaDefinition,
            CredentialDefinition
        }
    }

}
