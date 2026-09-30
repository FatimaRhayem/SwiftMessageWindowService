using System;
using System.Collections.Generic; //list is provided by it 
using System.Configuration;
using System.Data.SqlClient;
using System.IO;

namespace SwiftMessageWindowService
{
    internal class SwiftMessageRepository
    {
        private readonly string connectionString;

        public SwiftMessageRepository()
        {
            connectionString = ConfigurationManager.ConnectionStrings["SwiftDbConnection"].ConnectionString;
        }
        public SqlConnection CreateConnection()
        {
            return new SqlConnection(connectionString);
        }
        public int InsertFile(
        SqlConnection connection,
        SqlTransaction transaction,
        string fileName,
        string filePath,
        int totalMessages)
        {
            string sql = @"
        INSERT INTO MsgFiles
        (
            FileName,
            FileCreationDate,
            FilePath,
            FileStatus,
            TotalMessages
        )
        VALUES
        (
            @FileName,
            @FileCreationDate,
            @FilePath,
            @FileStatus,
            @TotalMessages
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);
    ";

            {
                using (SqlCommand command =new SqlCommand(sql, connection, transaction))  //Sends SQL commands to SQL Server

                {
                    command.Parameters.AddWithValue("@FileName",fileName);

                    command.Parameters.AddWithValue("@FileCreationDate",File.GetCreationTime(filePath));

                    command.Parameters.AddWithValue("@FilePath",filePath);

                    command.Parameters.AddWithValue("@FileStatus","PROCESSING");

                    command.Parameters.AddWithValue("@TotalMessages",totalMessages);

                    return (int)command.ExecuteScalar();
                }
            }
        }

        public int InsertMessage(
   SqlConnection connection,
   SqlTransaction transaction,
   ParsedInfo parsedInfo,
   int fileIdKey,
   string messageContent)
        {
            string sql = @"
        INSERT INTO Messages
        (
            FileIdKey,
            MessageType,
            MessageDate,
            MessageContent,
            OwnBIC,
            CorrBIC,
            SubFormat,
            UserReference,
            UETR,
            Reference,
            OrderingCustomer,
            Beneficiary,
            DetailsOfCharges,
            Block1,
            Block2,
            Block3,
            Block4,
            MessageStatus
        )
        VALUES
        (
            @FileIdKey,
            @MessageType,
            @MessageDate,
            @MessageContent,
            @OwnBIC,
            @CorrBIC,
            @SubFormat,
            @UserReference,
            @UETR,
            @Reference,
            @OrderingCustomer,
            @Beneficiary,
            @DetailsOfCharges,
            @Block1,
            @Block2,
            @Block3,
            @Block4,
           @MessageStatus
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);
    ";
            {

                using (SqlCommand command =new SqlCommand(sql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@FileIdKey",fileIdKey);

                    command.Parameters.AddWithValue("@MessageType",parsedInfo.MessageType ?? (object)DBNull.Value);

                    command.Parameters.AddWithValue("@MessageDate",DateTime.Now);

                    command.Parameters.AddWithValue("@MessageContent",messageContent);

                    command.Parameters.AddWithValue("@OwnBIC",parsedInfo.OwnBIC ?? (object)DBNull.Value);

                    command.Parameters.AddWithValue( "@CorrBIC",parsedInfo.CorrBIC ?? (object)DBNull.Value);

                    command.Parameters.AddWithValue("@SubFormat",parsedInfo.SubFormat ?? (object)DBNull.Value);

                    command.Parameters.AddWithValue("@UserReference",parsedInfo.UserReference ?? (object)DBNull.Value);

                    command.Parameters.AddWithValue("@UETR",parsedInfo.UETR ?? (object)DBNull.Value);

                    command.Parameters.AddWithValue( "@Reference",parsedInfo.Reference ?? (object)DBNull.Value);

                    command.Parameters.AddWithValue("@OrderingCustomer",parsedInfo.OrderingCust ?? (object)DBNull.Value);

                    command.Parameters.AddWithValue("@Beneficiary",parsedInfo.BeneficiaryCust ?? (object)DBNull.Value);

                    command.Parameters.AddWithValue("@DetailsOfCharges",parsedInfo.DetailsOfCharges ?? (object)DBNull.Value);

                    command.Parameters.AddWithValue("@Block1",parsedInfo.Block1 ?? (object)DBNull.Value);

                    command.Parameters.AddWithValue("@Block2",parsedInfo.Block2 ?? (object)DBNull.Value);

                    command.Parameters.AddWithValue("@Block3",parsedInfo.Block3 ?? (object)DBNull.Value);

                    command.Parameters.AddWithValue("@Block4",parsedInfo.Block4 ?? (object)DBNull.Value);

                    command.Parameters.AddWithValue("@MessageStatus","MESSAGE INSERTED");

                    return (int)command.ExecuteScalar();
                }
            }
        }

        public int InsertIntervention(
     SqlConnection connection,
     SqlTransaction transaction,
     int mesgIdKey,
     string interventionType,
     string interventionDescription)
        {
            string sql = @"
        INSERT INTO MessagesInterventions
        (
            MesgIdKey,
            InterventionType,
            InterventionDate,
            InterventionDescription
        )
        VALUES
        (
            @MesgIdKey,
            @InterventionType,
            @InterventionDate,
            @InterventionDescription
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);
    ";


            {
                using (SqlCommand command =new SqlCommand(sql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@MesgIdKey",mesgIdKey);

                    command.Parameters.AddWithValue("@InterventionType",interventionType);

                    command.Parameters.AddWithValue("@InterventionDate",DateTime.Now);

                    command.Parameters.AddWithValue("@InterventionDescription",interventionDescription);
                    return (int)command.ExecuteScalar();
                }
            }
        }

        public int InsertEventLog(
  SqlConnection connection,
  SqlTransaction transaction,
  string eventType,
  string eventDescription)
        {
            string sql = @"
        INSERT INTO EventLogs
        (
            EventType,
            EventDescription
        )
        VALUES
        (
            @EventType,
            @EventDescription
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);
    ";
            {
                using (SqlCommand command =new SqlCommand(sql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@EventType",eventType);

                    command.Parameters.AddWithValue("@EventDescription",eventDescription);

                    return (int)command.ExecuteScalar();
                }
            }
        }

        public void UpdateFileStatus(SqlConnection connection, SqlTransaction transaction, int fileIdKey, string status)
        {
            string sql = @"
                 UPDATE MsgFiles
                 SET FileStatus = @FileStatus
                  WHERE FileIdKey = @FileIdKey;
                    ";
            {
                using (SqlCommand command =new SqlCommand(sql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@FileStatus",status);

                    command.Parameters.AddWithValue("@FileIdKey",fileIdKey);

                    command.ExecuteNonQuery();
                }
            }
        }
        //The @ allows us to write the SQL over multiple lines without needing \n everywhere.
        public List<InsertedMessageInfo> GetInsertedMessages(SqlConnection connection)
        {
            string sql = @" 
        SELECT
            m.MesgIdKey,
            m.FileIdKey,
            m.MessageContent,
            m.UserReference,
            f.FileName
        FROM Messages m
        INNER JOIN MsgFiles f
            ON m.FileIdKey = f.FileIdKey
        WHERE m.MessageStatus = 'MESSAGE INSERTED';
    ";

            List<InsertedMessageInfo> messages =new List<InsertedMessageInfo>(); //We create an empty list

            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                using (SqlDataReader reader =command.ExecuteReader()) //This sends the query to SQL Server. The SqlDataReader allows us to read the returned rows one by one

                {
                    while (reader.Read()) //(loop on the rows )As long as there is another row returned by SQL Server, process it.
                    {
                        InsertedMessageInfo message =new InsertedMessageInfo();

                        message.MesgIdKey =reader.GetInt32(reader.GetOrdinal("MesgIdKey"));//get the position\index of the mesgIdkey from the sql results 

                        message.FileIdKey =reader.GetInt32(reader.GetOrdinal("FileIdKey"));

                        message.MessageContent =reader["MessageContent"] == DBNull.Value
                                ? null
                                : reader["MessageContent"].ToString();

                        message.UserReference =reader["UserReference"] == DBNull.Value
                                ? null
                                : reader["UserReference"].ToString();

                        message.FileName = reader["FileName"].ToString();

                        messages.Add(message);
                    }
                }
            }

            return messages;
        }

        public void UpdateMessageStatus(SqlConnection connection,int mesgIdKey,string status)
        {
            string sql = @"
             UPDATE Messages
             SET MessageStatus = @MessageStatus
              WHERE MesgIdKey = @MesgIdKey;
    ";

            using (SqlCommand command =new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@MessageStatus",status);

                command.Parameters.AddWithValue("@MesgIdKey",mesgIdKey);

                command.ExecuteNonQuery();
            }
        }

    }

}
