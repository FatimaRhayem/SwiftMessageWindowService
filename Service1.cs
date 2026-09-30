using System;
using System.Collections.Generic;
using System.Configuration; //System.Configuration contains classes for reading configuration information from files such as App.config.
using System.Data.SqlClient;//We will use SqlConnection from this namespace to connect our Windows Service to SwiftMessageNewDB.
using System.IO; //System.IO contains .NET classes for working with files, directories, paths, and streams.
using System.ServiceProcess;//give us the class such as ServiceBase for creating the window service.
using System.Threading;//These will allow OnStop() to tell the background loop:Stop running.
using System.Threading.Tasks; //We will use a background Task to run the service loop without blocking OnStart()

namespace SwiftMessageWindowService 
{ 
    public partial class Service1 : ServiceBase 
    {
        private string InputFolder;
        private string SuccessFolder;
        private string ErrorFolder;
        private SwiftMessageRepository repository;
        private CancellationTokenSource cancellationTokenSource; //Stop signal
        private Task serviceTask;  //We need to keep a reference to it so that when the service stops, we know which background operation we started and can handle its shutdown properly

        public Service1()
        {
            InitializeComponent();
            repository = new SwiftMessageRepository();

            string baseFolder = ConfigurationManager.AppSettings["BaseFolder"];

            InputFolder = baseFolder;
            SuccessFolder = Path.Combine(baseFolder, "Success");
            ErrorFolder = Path.Combine(baseFolder, "Error");
        }

        protected override void OnStart(string[] args)
        {
            using (SqlConnection connection = repository.CreateConnection())
            {
                connection.Open();
            }

            cancellationTokenSource = new CancellationTokenSource(); //the source creates the cancellationToken which carries the cancellation signal 

            serviceTask = Task.Run(() => ServiceLoop(cancellationTokenSource.Token)); //takes the Token from that source and gives it to ServiceLoop().
        }

        private void ServiceLoop(CancellationToken cancellationToken) //CancellationToken is the stop signal that we created in OnStart()
        {
            while (!cancellationToken.IsCancellationRequested)//Keep running the loop as long as the service has not been told to stop
            {
                try
                {
                    ProcessMessage();
                    ProcessInsertedMessage();
                }
                catch (Exception)
                {
                    // Keep the service loop running if an unexpected error occurs
                }
               if(cancellationToken.WaitHandle.WaitOne(10000)) //Wait either until cancellation is requested, or until 10 seconds have passed but it breaks when it is true means when cancellation was signaled
                {
                    break;
                }
            }
        }


        private void ProcessMessage()
        {
            string[] files = Directory.GetFiles(
                InputFolder,
                "*.*",
                SearchOption.TopDirectoryOnly);

            foreach (string filePath in files)
            {
                try
                {
                    string fileContent = File.ReadAllText(filePath);

                    SwiftMessageParser parser = new SwiftMessageParser();

                    List<string> messages = parser.SeparateMessages(fileContent);

                    // Store all parsed messages in memory first. 
                    List<ParsedInfo> parsedMessages = new List<ParsedInfo>();

                    foreach (string message in messages)
                    {
                        ParsedInfo parsedInfo = parser.ParseMessage(message);

                        parsedMessages.Add(parsedInfo);
                    }

                    // At this point, ALL messages have been parsed successfully.
                    // Database insertion will happen after this point.

                    using (SqlConnection connection =
                        repository.CreateConnection())
                    {
                        connection.Open();

                        SqlTransaction transaction =       //database operations that SQL Server treats as one unit of work
                            connection.BeginTransaction();

                        try
                        {
                            int fileIdKey = repository.InsertFile(
                            connection,
                                transaction,
                                Path.GetFileName(filePath),
                                filePath,
                                messages.Count);

                            for (int i = 0; i < messages.Count; i++)
                            {
                                ParsedInfo parsedInfo = parsedMessages[i];

                                int mesgIdKey = repository.InsertMessage(
                                    connection,
                                    transaction,
                                    parsedInfo,
                                    fileIdKey,
                                    messages[i]);

                                repository.InsertIntervention(
                                    connection,
                                    transaction,
                                    mesgIdKey,
                                    "PROCESSING",
                                    "Message processed successfully");
                            }

                            repository.UpdateFileStatus(
                                connection,
                                transaction,
                                fileIdKey,
                                "SUCCESS");

                            repository.InsertEventLog(
                                connection,
                                transaction,
                                "FILE_PROCESSED",
                                "SWIFT file processed successfully: " +
                                Path.GetFileName(filePath));

                            transaction.Commit(); //save all the databases 

                        }
                        catch
                        {
                            transaction.Rollback(); //if error happened Undo all database changes made in this transaction.

                            throw;
                        }
                    }
                 string successFilePath = Path.Combine(
                SuccessFolder,
                Path.GetFileName(filePath));

                    File.Move(
                        filePath,
                        successFilePath);
                }
                catch (Exception)
                {
                    string errorFilePath = Path.Combine(
                        ErrorFolder,
                        Path.GetFileName(filePath));

                    File.Move(filePath, errorFilePath);
                }

            }
        }
    private void ProcessInsertedMessage()
        {
            using (SqlConnection connection =repository.CreateConnection())
            {
                connection.Open();

                List<InsertedMessageInfo> messages =repository.GetInsertedMessages(connection); //"Give me all messages that are still waiting to be processed.

                foreach (InsertedMessageInfo message in messages)
                {
                    string folderName = message.UserReference ?? "null";
                    string destinationFolder =Path.Combine(InputFolder, folderName);

                    Directory.CreateDirectory(destinationFolder);//Create this folder if it doesn't exist
                    string dateTime =DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");

                    string fileName =$"{Path.GetFileNameWithoutExtension(message.FileName)}_{message.MesgIdKey}_{dateTime}.txt";
                    string filePath = Path.Combine(destinationFolder, fileName);//build the complete file path

                    File.WriteAllText(filePath,message.MessageContent ?? string.Empty);
                    repository.UpdateMessageStatus(connection,message.MesgIdKey,"MESSAGE PROCESSED INSERTED");
                }
            }
        }
        protected override void OnStop()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel(); //Tell the loop to stop
            }

            if (serviceTask != null)
            {
                serviceTask.Wait(); //Wait for the background task to finish
            }

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Dispose(); //We no longer need it after the service has stopped, so we release its resources
                cancellationTokenSource = null; //clears our reference
            }

            serviceTask = null; //This indicates that there is no longer a running service task(clear the task reference)
        }
    }

}
