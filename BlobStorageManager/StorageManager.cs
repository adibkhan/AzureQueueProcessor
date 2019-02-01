using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BlobStorageManager
{
    public class StorageManager
    {
        #region Private Properties

        /// <summary>
        /// Category for log messages
        /// </summary>
        private const string _logCategory = "BlobStorageManager";

        /// <summary>
        /// Name of the Cloud Blob Container that is used to store the incoming messages
        /// </summary>
        private const string _containerName = "storage";

        /// <summary>
        /// Name of the directory within the container that contains the storage blob results; 
        /// </summary>
        private const string _contributionResultDirectoryName = "contribution";

        /// <summary>
        /// Sync object used to lock the initialization of the instance
        /// </summary>
        private static object _syncRoot = new object();

        /// <summary>
        /// Single instance of the manager; set during the initialization
        /// </summary>
        private static volatile StorageManager _instance;

        /// <summary>
        /// TRUE- instance is initialized with connection to container FALSE- unable to connect to the container
        /// </summary>
        private bool _isAvailable;

        /// <summary>
        /// Reference to the container that contains the alert results; not an open connection as it uses request/response with HTTPs. 
        /// Set with initialization
        /// </summary>
        private CloudBlobContainer _blobContainer;

        #endregion



        #region Public Properties: Static

        /// <summary>
        /// TRUE- instance is initialized with connection to container FALSE- unable to connect to the container
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
                if (_instance != null)
                {
                    return _instance._isAvailable;
                }
                else
                {
                    return false;
                }
            }
        }

        #endregion



        #region Constructor

        /// <summary>
        /// Creates a new instance and sets the default values
        /// </summary>
        private StorageManager()
        {
            _isAvailable = false;
        }

        #endregion



        #region Initialize

        /// <summary>
        /// Creates the single instance and connects to the blob container; creates the container if it has not already been done
        /// </summary>
        /// <param name="connectionString">Connection s tring to the Azure storage account that contains blobs</param>
        /// <returns>Nothing; async task</returns>
        public static async Task InitializeAsync(string connectionString)
        {
            try
            {
                //- Create Instance
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new StorageManager();
                        }
                    }
                }

                if (_instance._isAvailable == true)
                {
                    return;
                }

                //- Connect to Account
                CloudStorageAccount storageAccount;
                if (CloudStorageAccount.TryParse(connectionString, out storageAccount) == false)
                {
                    throw new Exception("Invalid connection string provided for the alert result storage");
                }

                //- Reference Container
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                _instance._blobContainer = blobClient.GetContainerReference(_containerName);

                //- Create Container
                await _instance._blobContainer.CreateIfNotExistsAsync();

                _instance._isAvailable = true;
            }
            catch (Exception ex)
            {
                if (_instance != null)
                {
                    _instance._isAvailable = false;
                }
                throw new Exception("An error was encountered with the intialization of the AlertResultBlobManager", ex);
            }
        }

        #endregion



        #region Check if Available

        /// <summary>
        /// Determines if the manager is available for usage; throws exception when not initialized or IsAvailable is false
        /// </summary>
        private static void CheckIfAvailable()
        {
            if (IsAvailable == false)
            {
                throw new Exception("Unable to use AlertResultBlobManager; instance is not available");
            }
        }

        #endregion



        #region storage Results

        /// <summary>
        /// Loads the object into Azure Storage
        /// </summary>
        /// <param name="sourceResult">Metadata to be stored within the Azure storage</param>
        /// <returns>Nothing, async task</returns>
        public static async Task StoreStorageBlobAsync(ResultStorage sourceResult)
        {
            //- Check Parameters
            if (sourceResult == null)
            {
                throw new ArgumentNullException("sourceResult");
            }

            if (string.IsNullOrEmpty(sourceResult.HyperlinkID) == true)
            {
                throw new Exception("Unable to load storage result; missing hyperlink ID");
            }

            //- Check Manager
            CheckIfAvailable();

            //- Serialize
            string sourceResultSerialized = JsonConvert.SerializeObject(sourceResult);

            //- Insert Blob
            CloudBlockBlob blob = _instance._blobContainer.GetBlockBlobReference(GetContributionBlobKey(sourceResult.HyperlinkID));

            await blob.UploadTextAsync(sourceResultSerialized);
        }

        /// <summary>
        /// Returns the key for the contribution blob; joins the directory name with the hyperlink ID; ensures in lower case
        /// </summary>
        /// <param name="hyperlinkID">Unique identifier for the storage blob</param>
        /// <returns>Key for the storage blob</returns>
        private static string GetContributionBlobKey(string hyperlinkID)
        {
            return string.Format("{0}/{1}", _contributionResultDirectoryName, hyperlinkID).ToLower();
        }

        /// <summary>
        /// Gets the contribution metadata that is stored within Azure storage using the hyperlink ID as the key; returns null when not found
        /// </summary>
        /// <param name="hyperlinkID">Unique identifier of the storage blob result; key used to locate the results in storage</param>
        /// <returns>Contribution data from storage; NULL when not found</returns>
        public static async Task<ResultStorage> GetContributionResultAsync(string hyperlinkID)
        {
            //- Check Parameters
            if (string.IsNullOrEmpty(hyperlinkID) == true)
            {
                throw new ArgumentNullException("hyperlinkID");
            }

            //- Get Value
            ResultFromStorage storageResult = await GetFromStorageAsync(GetContributionBlobKey(hyperlinkID));

            //- Deserialize Result
            ResultStorage result = null;

            if (storageResult.HasResult == true)
            {
                result = JsonConvert.DeserializeObject<ResultStorage>(storageResult.SerializedResult);
            }

            return result;
        }

        #endregion



        #region Get From Storage

        /// <summary>
        /// Gets the serialized results from storage with validation to check for errors while downloading
        /// </summary>
        /// <param name="key">Unique identifier to get the result information</param>
        /// <returns>Metadata returned from storage</returns>
        private static async Task<ResultFromStorage> GetFromStorageAsync(string key)
        {
            //- Check Manager
            CheckIfAvailable();

            //- Get Value
            ResultFromStorage result = new ResultFromStorage()
            {
                HasResult = false,
            };

            CloudBlockBlob blob = _instance._blobContainer.GetBlockBlobReference(key);

            result.SerializedResult = await blob.DownloadTextAsync();
            result.HasResult = true;

            return result;
        }

        #endregion


    }
}
