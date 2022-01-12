using Hyperledger.Aries.Storage;
using System;
using IndyLedger = Hyperledger.Indy.LedgerApi.Ledger;

namespace Hyperledger.Aries.Features.IssueCredential
{
    public class LedgerQueueObject : RecordBase
    {
        public Type ReqType;
        public string IssuerDID;
        public string Request;
        public string ObjectId;
        public DateTime TimeStamp;

        public override string TypeName => "LedgerQueueObject";

        public LedgerQueueObject() { }

        public static LedgerQueueObject CreateRevocationQueueObject(string issuerDid, string revocationRecordId, string revocRegistryDeltaJson, string credentialId)
        {
            LedgerQueueObject ledgerQueueObject = new LedgerQueueObject();

            ledgerQueueObject.Request = IndyLedger.BuildRevocRegEntryRequestAsync(issuerDid, revocationRecordId, "CL_ACCUM", revocRegistryDeltaJson).Result;

            ledgerQueueObject.IssuerDID = issuerDid;
            ledgerQueueObject.ObjectId = credentialId;
            ledgerQueueObject.ReqType = LedgerQueueObject.Type.Revocation;

            ledgerQueueObject.TimeStamp = DateTime.UtcNow;

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
