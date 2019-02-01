using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureQueueProcessor
{
    public class QueueMessage
    {
        public string HyperlinkID { get; set; }
        public int EventType { get; set; }
        public string ClientName { get; set; }
        public DateTimeOffset SourceEditDataUTC { get; set; }
        public bool PassedValidation { get; set; }
        public int OperationType { get; set; }
    }
}
