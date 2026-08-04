using System.Text.Json.Serialization;

namespace SWP391_AutoWashPro_BE.Service.SePay;

public class Request
{
    public class SePayWebhookRequest
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("gateway")]
        public string? Gateway { get; set; }

        [JsonPropertyName("transactionDate")]
        public string? TransactionDate { get; set; }

        [JsonPropertyName("accountNumber")]
        public string? AccountNumber { get; set; }

        [JsonPropertyName("subAccount")]
        public string? SubAccount { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("transferType")]
        public string? TransferType { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("transferAmount")]
        public long TransferAmount { get; set; }

        [JsonPropertyName("accumulated")]
        public long Accumulated { get; set; }

        [JsonPropertyName("referenceCode")]
        public string? ReferenceCode { get; set; }
    }
}
