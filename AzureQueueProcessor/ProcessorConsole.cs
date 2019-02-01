using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace AzureQueueProcessor
{
    public class ProcessorConsole
    {
        #region logging configuration
        private const string _loggerName = "QueueProcessor";                     // logger target
        private Timer serviceTimer = null;
        private int checkQueueProcessorTimerInterval = Convert.ToInt32(ConfigurationManager.AppSettings["CheckQueueProcessorTimerIntervalInMilliSeconds"]);
        private bool usePoisonQueue;
        Manager queueManager;
        #endregion

        public void ProcessQueue(bool usePsnQueue)
        {
            try
            {
                usePoisonQueue = usePsnQueue;

                InitializeAndStartTimer(ref serviceTimer);

                Console.WriteLine("Press any key to stop the message pump.");
                CreateQueueManagerAndProcessMessages(usePoisonQueue);
            }
            catch (Exception ex)
            {

            }
        }

        void serviceTimer_Elapsed(object sender, EventArgs e)
        {
            try
            {
                serviceTimer.Stop();
                if (queueManager != null)
                {
                    if (!queueManager.QueueIsRunning)
                    {
                        queueManager.DisposeQueueClients();
                        CreateQueueManagerAndProcessMessages(usePoisonQueue);
                    }
                }
                else
                {
                    CreateQueueManagerAndProcessMessages(usePoisonQueue);
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                serviceTimer.Start();
            }
        }

        private void CreateQueueManagerAndProcessMessages(bool usePoisonQueue)
        {
            queueManager = new Manager();

            queueManager.DequeueMessages();
            Console.ReadKey();
            queueManager.DisposeQueueClients();
        }

        private void InitializeAndStartTimer(ref Timer serviceTimer)
        {
            serviceTimer = new Timer();
            serviceTimer.Interval = checkQueueProcessorTimerInterval;
            serviceTimer.Elapsed += (serviceTimer_Elapsed);
            serviceTimer.Start();
        }
    }


}

