using LiteDB;

namespace PhilterData
{
    public class ContextEntryEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();
        public string Token { get; set; } = string.Empty;
        public string Replacement { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
    }

}
