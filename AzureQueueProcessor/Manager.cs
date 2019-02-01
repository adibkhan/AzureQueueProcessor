using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.Azure.ServiceBus;
using System.Threading;
using BlobStorageManager;

namespace AzureQueueProcessor
{


    public class Manager
    {

        static string serviceBusConnectionString = AppSettings.Get<string>("ServiceBusConnectionString");
        static string contributionQueueName = AppSettings.Get<string>("ContibutionQueueName");
        static string poisonQueueName = AppSettings.Get<string>("PoisonQueueName");
        private static string storageBlobConnectionString = AppSettings.Get<string>("StorageBlobConnectionString");
        static int maxConcurrentCalls = AppSettings.Get<int>("MaxConcurrentCalls");
        static int maxAutoRenewDuration = AppSettings.Get<int>("MaxAutoRenewDuration");
        static bool useDeadLetterQueue = AppSettings.Get<bool>("UseDeadLetterQueue");
        static int preFetchCount = AppSettings.Get<int>("PreFetchCount");
        public bool QueueIsRunning { get; set; }

        static IQueueClient contributionQueueClient;


        public Manager()
        {
            if (useDeadLetterQueue)
            {
                contributionQueueName = GetDeadLetterQueue(contributionQueueName);
            }

            contributionQueueClient = new QueueClient(serviceBusConnectionString, contributionQueueName);
            InitializeBlobStorage();
            QueueIsRunning = true;
        }

        public void DequeueMessages()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        public async Task RunAsync()
        {
            RegisterOnMessageHandlerAndReceiveMessages();
        }

        public void RegisterOnMessageHandlerAndReceiveMessages()
        {
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = maxConcurrentCalls,
                MaxAutoRenewDuration = TimeSpan.FromMinutes(maxAutoRenewDuration),
                AutoComplete = false
            };
            contributionQueueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
            contributionQueueClient.PrefetchCount = preFetchCount;
        }

        public async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
           
            try
            {
                string tempString = Encoding.UTF8.GetString(message.Body);
                // Do some work
               
            }
            catch (Exception ex)
            {
               
            }
            finally
            {
                await contributionQueueClient.CompleteAsync(message.SystemProperties.LockToken);
                QueueIsRunning = true;
            }

        }

        public Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            string errorMessage = $"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}, " +
                $" Exception context for troubleshooting: - Endpoint: {context.Endpoint} - Entity Path: {context.EntityPath} - Executing Action: {context.Action}";

            QueueIsRunning = false;
            return Task.CompletedTask;
        }

        public QueueMessage BuildPoisonQueueMessage(QueueMessage queueMessage)
        {
            QueueMessage poinsonQueueMessage = new QueueMessage()
            {
                ClientName = AppSettings.AppName(),
                HyperlinkID = queueMessage.HyperlinkID,
                EventType = queueMessage.EventType,
                PassedValidation = queueMessage.PassedValidation,
                OperationType = queueMessage.OperationType
            };

            return poinsonQueueMessage;
        }

        public async Task DisposeQueueClients()
        {
            await contributionQueueClient.CloseAsync();
        }

        public async void InitializeBlobStorage()
        {
            await StorageManager.InitializeAsync(storageBlobConnectionString);
        }

        private string GetDeadLetterQueue(string contributionQueueName)
        {
            return contributionQueueName = contributionQueueName + "/$DeadLetterQueue";
        }

    }


}
