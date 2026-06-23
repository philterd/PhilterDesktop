using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhilterData
{
    public class PolicyEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();
        public string Name { get; set; } = string.Empty;
        public string Json { get; set; } = "{\"Identifiers\": {}}";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
