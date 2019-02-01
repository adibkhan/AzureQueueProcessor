using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobStorageManager
{
    public class ResultFromStorage
    {

        /// <summary>
        /// Downloaded serialized object from storage
        /// </summary>
        public string SerializedResult { get; set; }

        /// <summary>
        /// TRUE- found result within storage; serialized result is populated FALSE- results not found within storage
        /// </summary>
        public bool HasResult { get; set; }

    }
}
