namespace SwiftMessageWindowService
{
    internal class ParsedInfo
    {
        public string OwnBIC { get; set; }
        public string CorrBIC { get; set; }
        public string MessageType { get; set; }
        public string SubFormat { get; set; }
        public string UserReference { get; set; }
        public string UETR { get; set; }
        public string SLA {  get; set; }
        public string Reference { get; set; }
        public string OrderingCust { get; set; }
        public string BeneficiaryCust { get; set; }
        public string DetailsOfCharges { get; set; }
        public string Block1 { get; set; }
        public string Block2 { get; set; }
        public string Block3 { get; set; }
        public string Block4 { get; set; }
  
    }
}