using Amazon.SecurityToken.Model;
using MongoDB.Bson;

namespace VolumeAlert
{
    public class AvgVol
    {
        public BsonObjectId _id { get; set; }
        public BsonDateTime Data { get; set; }
        public string Data_String { get; set; }
        public string Symbol { get; set; }
        public long AvgVolume { get; set; }        
    }
}
