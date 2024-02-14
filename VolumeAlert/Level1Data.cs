using MongoDB.Bson;

namespace VolumeAlert
{
    public class Level1Data
    {
        public BsonObjectId _id { get; set; }
        public string Message { get; set; }
        public string MarketTime { get; set; }
        public string Symbol { get; set; }
        public double BidPrice { get; set; }
        public double AskPrice { get; set; }
        public long BidSize { get; set; }
        public long AskSize { get; set; }
        public long Volume { get; set; }
        public double MinPrice { get; set; }
        public double MaxPrice { get; set; }
        public double LowPrice { get; set; }
        public double HighPrice { get; set; }
        public double FirstPrice { get; set; }
        public double OpenPrice { get; set; }
        public double ClosePrice { get; set; }
        public double MaxPermittedPrice { get; set; }
        public double MinPermittedPrice { get; set; }
        public int LotSize { get; set; }
        public double LastPrice { get; set; }
        public string InstrumentState { get; set; }
        public string AssetClass { get; set; }
        public double TickValue { get; set; }
        public double TickSize { get; set; }
        public string Currency { get; set; }
        public string Tick { get; set; }
        public double TAP { get; set; }
        public int TAV { get; set; }
        public string TAT { get; set; }
        public string SSR { get; set; }
    }
}
