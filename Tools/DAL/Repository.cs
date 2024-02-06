using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using Tools.Classes;
using Tools.Models;
using Tools.Rosreestr;
using Tools.Rosreestr.Rosreestr.Interfaces;

namespace Tools.DAL
{
    public static class Repository
    {
        public static SqlConnection GetDebtConnection(bool needOpen = true)
        {
            string str = SETTINGS.PIPELINE_DB_CONNECTION;
            SqlConnection connection = new SqlConnection(str);

            if (needOpen)
                connection.Open();

            return connection;
        }


        public static RepositoryTransaction BeginTransaction()
        {
            SqlConnection connection = GetDebtConnection();
            return new RepositoryTransaction(connection.BeginTransaction());
        }


        public static string ResolveKey(string sourceKey)
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                try
                {
                    cmd.CommandText = @"SELECT [Source] FROM dbo.Sources
                                    WHERE [Key] = @Key";

                    cmd.Parameters.AddWithValue("@Key", sourceKey);

                    var result = cmd.ExecuteScalar();

                    return (string)result;
                }
                catch
                {
                    return null;
                }
            }
        }


        public static bool QueueUpOrders(OrderPackage package)
        {
            string source = ResolveKey(package.Source_Key);

            using (RepositoryTransaction tr = Repository.BeginTransaction())
            {
                try
                {
                    foreach (var item in package.Orders)
                        QueueUpOrder(item, source, tr.Transaction);

                    tr.Commit();

                    return true;
                }
                catch
                {
                    tr.Rollback();
                    throw;
                }
            }
        }

        public static void QueueUpOrder(UnpreparedOrder order, string source, SqlTransaction tr)
        {
            using (SqlCommand cmd = new SqlCommand(string.Empty, tr.Connection, tr))
            {
                try
                {
                    cmd.Parameters.Clear();

                    cmd.CommandText = @"INSERT INTO [dbo].[Pipeline]
                                           (
                                              [ID_Request],
                                              [Source],
                                              [AddressRecivedAt],
                                              [Address],
                                              [Square],
                                              [Result],
                                              [District],
                                              [City],
                                              [Town],
                                              [Street],
                                              [Home],
                                              [Corp],
                                              [Flat]
                                           )
                                           VALUES 
                                           (
                                              @ID_Request,
                                              @Source,
                                              @AddressRecivedAt,
                                              @Address,
                                              @Square,
                                              'Адрес получен',
                                              @District,
                                              @City,
                                              @Town,
                                              @Street,
                                              @Home,
                                              @Corp,
                                              @Flat
                                           )";

                    cmd.Parameters.AddWithValue("@ID_Request", order.ID_Request);
                    cmd.Parameters.AddWithValue("@Source", source);
                    cmd.Parameters.AddWithValue("@AddressRecivedAt", DateTime.Now);
                    cmd.Parameters.AddWithValue("@Address", order.Address);
                    cmd.Parameters.AddWithValue("@Square", order.Square, true);
                    cmd.Parameters.AddWithValue("@District", order.District, true);
                    cmd.Parameters.AddWithValue("@City", order.Town == "Ростов-на-Дону" ? order.Town : order.City, true);
                    cmd.Parameters.AddWithValue("@Town", order.Town == "Ростов-на-Дону" ? string.Empty : order.Town, true);
                    cmd.Parameters.AddWithValue("@Street", order.Street);
                    cmd.Parameters.AddWithValue("@Home", order.Home, true);
                    cmd.Parameters.AddWithValue("@Corp", order.Corp, true);
                    cmd.Parameters.AddWithValue("@Flat", order.Flat, true);

                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
            }
        }


        public static bool CheckUnpreparedQueue()
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"SELECT COUNT(*) FROM [dbo].[Pipeline]
                                    WHERE [CadastralNumber] is null
                                    AND [Result] = 'Адрес получен'";

                var result = cmd.ExecuteScalar();

                return (int)result == 0;
            }
        }

        public static UnpreparedOrder GetUnprepared()
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                try
                {
                    cmd.CommandText = @"SELECT TOP 1 * FROM [dbo].[Pipeline]
                                    WHERE [CadastralNumber] is NULL
                                    AND [Result] = 'Адрес получен'";

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();

                        return new UnpreparedOrder
                        {
                            ID = reader.GetData<int>(0),
                            ID_Request = reader.GetData<int>(1),
                            RecivedAt = DateTime.Now,
                            Source = reader.GetData<string>(2),
                            Square = reader.GetData<string>(8),
                            District = reader.GetData<string>(14),
                            City = reader.GetData<string>(15),
                            Town = reader.GetData<string>(16),
                            Street = reader.GetData<string>(17),
                            Home = reader.GetData<string>(18),
                            Corp = reader.GetData<string>(19),
                            Flat = reader.GetData<string>(20)
                        };
                    }
                }
                catch
                {
                    throw;
                }
            }
        }

        public static RosreestrPipeline GetUnpreparedPipeline()
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                try
                {
                    cmd.CommandText = @"SELECT TOP 1
                                        [LoginKey],
                                        [Value]
                                        FROM [dbo].[RosrKeys]";

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();

                        return new RosreestrPipeline(reader.GetData<string>(1))
                        {
                            LoginKey = reader.GetData<string>(0)
                        };
                    }
                }
                catch
                {
                    throw;
                }
            }
        }


        public static bool CheckPreparedQueue()
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"SELECT COUNT(*) FROM [dbo].[Pipeline]
                                    WHERE [IsChecked] = 1
                                    AND [NumRequest] is NULL
                                    AND ([Result] = 'Кадастровый номер получен'
                                    OR Result = 'Потерян Росреестром')";

                var result = cmd.ExecuteScalar();

                return (int)result == 0;
            }
        }

        public static PreparedOrder GetPrepared()
        {
            try
            {
                using (SqlConnection connection = GetDebtConnection())
                using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
                {
                    cmd.CommandText = @"select top 1
                                    [ID],
                                    [Source],
                                    [CadastralNumber]
                                    FROM [dbo].[Pipeline]
                                    WHERE [IsChecked] = 1 
                                    and NumRequest is null and
                                    (Result = 'Кадастровый номер получен'
                                    or Result = 'Потерян Росреестром')";

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();

                        return new PreparedOrder
                        {
                            ID = reader.GetData<int>(0),
                            Source = reader.GetData<string>(1),
                            CadastralNumber = reader.GetData<string>(2)
                        };
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static RosreestrPipeline GetPreparedPipeline()
        {
            try
            {
                using (SqlConnection connection = GetDebtConnection())
                using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
                {
                    cmd.CommandText = @"declare @workerID int = (SELECT TOP 1 [ID] FROM [dbo].[RosrKeys] WHERE InOrdering = '' or InOrdering is null);
	                                    declare @resultID int;
	                                    declare @today int = DATEPART(DW, GETDATE());
	                                    declare @now time(7) = cast(GETDATE() as time(7));

			                            IF		(@workerID in (SELECT ID_RosrKeys FROM KeysSchedule))
			                            AND		(@today in (SELECT DayWeek FROM KeysSchedule WHERE ID_RosrKeys = @workerID))
			                            AND		(@now between (SELECT TimeBlockSince FROM KeysSchedule WHERE ID_RosrKeys = @workerID AND DayWeek = @today) AND (SELECT TimeBlockTil FROM KeysSchedule WHERE ID_RosrKeys = @workerID AND DayWeek = @today))

					                            set @resultID = (SELECT TOP 1 [ID] FROM RosrKeys WHERE ID != @workerID AND InOrdering = '' or InOrdering is null)
			                            ELSE
					                            set @resultID = (SELECT TOP 1 [ID] FROM RosrKeys WHERE InOrdering = '' or InOrdering is null)

					                    SELECT 
                                        LoginKey,
                                        Value
					                    FROM RosrKeys WHERE ID = @resultID";

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();

                        return new RosreestrPipeline(reader.GetData<string>(1))
                        {
                            LoginKey = reader.GetData<string>(0)
                        };
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static void SetBusyOrder(string loginKey, string cadastral)
        {
            using (SqlConnection connection = Repository.GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE [dbo].[RosrKeys]
                                    SET InOrdering = @Cadastral
                                    WHERE [LoginKey] =  @loginKey";

                cmd.Parameters.AddWithValue("@Cadastral", cadastral);
                cmd.Parameters.AddWithValue("@loginKey", loginKey);

                cmd.ExecuteNonQuery();
            }
        }

        public static void SetFreeOrder(string loginKey)
        {
            using (SqlConnection connection = Repository.GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE [dbo].[RosrKeys]
                                    SET InOrdering = ''
                                    WHERE [LoginKey] =  @loginKey";

                cmd.Parameters.AddWithValue("@loginKey", loginKey);

                cmd.ExecuteNonQuery();
            }
        }


        public static bool CheckLoadQueue()
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"SELECT count(*)
                                    FROM Pipeline
                                    WHERE HasXml = 0 AND NumRequest is not null
									AND NumRequest not in (select InLoading from RosrKeys where InLoading != '' or InLoading is not null) and 
                                    CASE
                                    WHEN LastUploadAttempt is not null 
                                    THEN Dateadd(hour, @PickUpAttemptDelay, LastUploadAttempt) 
                                    ELSE DateAdd(hour, @FirstPickUpAttemptDelay, RequestRecivedAt)
                                    end<GETDATE()";

                cmd.Parameters.AddWithValue("@PickUpAttemptDelay", SETTINGS.PICKUP_ATTEMPT_DELAY);
                cmd.Parameters.AddWithValue("@FirstPickUpAttemptDelay", /*SETTINGS.FIRST_PICKUP_ATTEMPT_DELAY*/ 0);

                var result = cmd.ExecuteScalar();

                return (int)result == 0;
            }
        }

        public static LoadOrder GetLoadable()
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                try
                {
                    cmd.CommandText = @"SELECT TOP (1) Pipeline.[ID], Pipeline.[Source], Pipeline.[NumRequest], RosrKeys.[Value]
                                    FROM [DebtPipeline].[dbo].[Pipeline]
                                    join RosrKeys ON RosrKeys.LoginKey = Pipeline.Worker and RosrKeys.InLoading =''
                                    Where HasXml = 0 and NumRequest is not null
									
                                    and
                                    Case when LastUploadAttempt is not null
                                    then Dateadd(hour, @PickUpAttemptDelay, LastUploadAttempt) 
                                    else DateAdd(hour, @FirstPickUpAttemptDelay, RequestRecivedAt)
                                    end<GETDATE()
                                    order by Priority, LastUploadAttempt, RequestRecivedAt";

                    cmd.Parameters.AddWithValue("@PickUpAttemptDelay", SETTINGS.PICKUP_ATTEMPT_DELAY);
                    cmd.Parameters.AddWithValue("@FirstPickUpAttemptDelay", /*SETTINGS.FIRST_PICKUP_ATTEMPT_DELAY*/ 0);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();

                        return new LoadOrder
                        {
                            ID = reader.GetData<int>(0),
                            Source = reader.GetData<string>(1),
                            NumRequest = reader.GetData<string>(2),
                            WorkerKey = reader.GetData<string>(3)
                        };
                    }
                }
                catch
                {
                    throw;
                }
            }
        }


        public static void SetBusyLoader(string Key, string numRequest)
        {
            using (SqlConnection connection = Repository.GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE [dbo].[RosrKeys]
                                    SET InLoading = @NumRequest
                                    WHERE [Value] =  @Key";

                cmd.Parameters.AddWithValue("@NumRequest", numRequest);
                cmd.Parameters.AddWithValue("@Key", Key);

                cmd.ExecuteNonQuery();
            }
        }

        public static void SetFreeLoader(string key)
        {
            using (SqlConnection connection = Repository.GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE [dbo].[RosrKeys]
                                    SET InLoading = ''
                                    WHERE [Value] =  @key";

                cmd.Parameters.AddWithValue("@key", key);

                cmd.ExecuteNonQuery();
            }
        }


        public static void SetFreeOnStart()
        {
            using (SqlConnection connection = Repository.GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE [dbo].[RosrKeys]
                                    SET InOrdering = ''

                                    UPDATE [dbo].[RosrKeys]
                                    SET InLoading = ''";

                cmd.ExecuteNonQuery();
            }
        }




        public static void SetNotFoundData(UnpreparedOrder order)
        {
            using (SqlConnection connection = Repository.GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE [dbo].[Pipeline]
                                    SET [Result] = 'Ни одного адреса не найдено'
                                    WHERE [ID] =  @ID
                                    AND [Source] = @Source";

                cmd.Parameters.AddWithValue("@Source", order.Source);
                cmd.Parameters.AddWithValue("@ID", order.ID);

                cmd.ExecuteNonQuery();
            }
        }


        public static void SetAddressNotFound(UnpreparedOrder order)
        {
            using (SqlConnection connection = Repository.GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE [dbo].[Pipeline]
                                    SET [Result] = 'Адрес не найден'
                                    WHERE [ID] =  @ID
                                    AND [Source] = @Source";

                cmd.Parameters.AddWithValue("@Source", order.Source);
                cmd.Parameters.AddWithValue("@ID", order.ID);

                cmd.ExecuteNonQuery();
            }
        }

        public static IEnumerable<string> TryGetCorrectStreet(string originalStreet)
        {
            if (string.IsNullOrEmpty(originalStreet))
            {
                yield return null; yield break;
            }
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"select Correct from Dictionary 
                                    where Wrong = @Value";

                cmd.Parameters.AddWithValue("@Value", originalStreet);

                using (SqlDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                        yield return reader.GetData<string>(0);
            }
        }


        public static void AddPreparedData(UnpreparedOrder original, List<AddressSearchInfo> addrs)
        {
            if (addrs.Count == 0)
                return;

            using (RepositoryTransaction tr = Repository.BeginTransaction())
            {
                try
                {
                    foreach (var item in addrs)
                        AddPreparedData(original, item, tr.Transaction);

                    tr.Commit();
                }
                catch
                {
                    tr.Rollback();
                    throw;
                }
            }

            RemoveSuccessedPreparedOrder(original);
        }

        public static void AddPreparedDataNew(UnpreparedOrder original, List<AddressSearchInfoGos> addrsnew)
        {
            if (addrsnew.Count == 0)
                return;

            using (RepositoryTransaction tr = Repository.BeginTransaction())
            {
                try
                {
                    foreach (var item in addrsnew)
                        AddPreparedDataNew(original, item, tr.Transaction);

                    tr.Commit();
                }
                catch
                {
                    tr.Rollback();
                    throw;
                }
            }

            RemoveSuccessedPreparedOrder(original);
        }
        public static void AddPreparedDataNew(
            UnpreparedOrder original, AddressSearchInfoGos addrNew, SqlTransaction tr)
        {
            using (SqlCommand cmd = new SqlCommand(string.Empty, tr.Connection, tr))
            {
                try
                {
                    cmd.CommandText = @"INSERT INTO [dbo].[Pipeline]
                                    (
                                        [ID_Request],
                                        [Source],
                                        [AddressRecivedAt],
                                        [CadastralNumber],
                                        [Address],
                                        [Square],
                                        [Result],
                                        [District],
                                        [City],
                                        [Town],
                                        [Street],
                                        [Home],
										[Corp],
                                        [Flat],
                                        [R_FullAddress],
                                        [R_ObjType],
                                        [R_Square],
                                        [R_SteadCategory],
                                        [R_SteadKind],
                                        [R_FuncName],
                                        [R_Status],
                                        [R_CadastralCost],
                                        [R_CadastralCostDate],
                                        [R_NumStoreys],
                                        [R_UpdateInfoDate],
                                        [R_LiterBTI]
                                    )
                                    VALUES
                                    (
                                        @ID_Request,
                                        @Source,
                                        @RecivedAt,
                                        @CadastralNumber,
                                        @Address,
                                        @Square,
                                        'Кадастровый номер получен',
                                        @District,
                                        @City,
                                        @Town,
                                        @Street,
                                        @Home,
										@Corp,
                                        @Flat,
                                        @Ros_FullAddress,
                                        @Ros_ObjType,
                                        @Ros_Square,
                                        @Ros_SteadCategory,
                                        @Ros_SteadKind,
                                        @Ros_FuncName,
                                        @Ros_Status,
                                        @Ros_CadastralCost,
                                        @Ros_CadastralCostDate,
                                        @Ros_NumStoreys,
                                        @Ros_UpdateInfoDate,
                                        @Ros_LiterBTI
                                    )";

                    cmd.Parameters.AddWithValue("@ID_Request", original.ID_Request);
                    cmd.Parameters.AddWithValue("@RecivedAt", original.RecivedAt);
                    cmd.Parameters.AddWithValue("@Source", original.Source);
                    cmd.Parameters.AddWithValue("@CadastralNumber", addrNew.CadastralNumber);
                    cmd.Parameters.AddWithValue("@Address", original.Address);
                    cmd.Parameters.AddWithValue("@Square", original.Square);
                    cmd.Parameters.AddWithValue("@District", original.District, true);
                    cmd.Parameters.AddWithValue("@City", original.Town == "Ростов-на-Дону" ? original.Town : original.City, true);
                    cmd.Parameters.AddWithValue("@Town", original.Town == "Ростов-на-Дону" ? string.Empty : original.Town, true);
                    cmd.Parameters.AddWithValue("@Street", original.Street);
                    cmd.Parameters.AddWithValue("@Home", original.Home, true);
                    cmd.Parameters.AddWithValue("@Corp", original.Corp, true);
                    cmd.Parameters.AddWithValue("@Flat", original.Flat, true);
                    cmd.Parameters.AddWithValue("@Ros_FullAddress", addrNew.FullAddress);
                    cmd.Parameters.AddWithValue("@Ros_ObjType", null, true);
                    cmd.Parameters.AddWithValue("@Ros_Square", null, true);
                    cmd.Parameters.AddWithValue("@Ros_SteadCategory", null, true);
                    cmd.Parameters.AddWithValue("@Ros_SteadKind", null, true);
                    cmd.Parameters.AddWithValue("@Ros_FuncName", null, true);
                    cmd.Parameters.AddWithValue("@Ros_Status", null, true);
                    cmd.Parameters.AddWithValue("@Ros_CadastralCost", null, true);
                    cmd.Parameters.AddWithValue("@Ros_CadastralCostDate", null, true);
                    cmd.Parameters.AddWithValue("@Ros_NumStoreys", null, true);
                    cmd.Parameters.AddWithValue("@Ros_UpdateInfoDate", null, true);
                    cmd.Parameters.AddWithValue("@Ros_LiterBTI", null, true);
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    SetNotFoundData(original);
                    throw;
                }
            }
        }

        public static void AddPreparedData(
            UnpreparedOrder original, AddressSearchInfo addr, SqlTransaction tr)
        {
            using (SqlCommand cmd = new SqlCommand(string.Empty, tr.Connection, tr))
            {
                try
                {
                    cmd.CommandText = @"INSERT INTO [dbo].[Pipeline]
                                    (
                                        [ID_Request],
                                        [Source],
                                        [AddressRecivedAt],
                                        [CadastralNumber],
                                        [Address],
                                        [Square],
                                        [Result],
                                        [District],
                                        [City],
                                        [Town],
                                        [Street],
                                        [Home],
										[Corp],
                                        [Flat],
                                        [R_FullAddress],
                                        [R_ObjType],
                                        [R_Square],
                                        [R_SteadCategory],
                                        [R_SteadKind],
                                        [R_FuncName],
                                        [R_Status],
                                        [R_CadastralCost],
                                        [R_CadastralCostDate],
                                        [R_NumStoreys],
                                        [R_UpdateInfoDate],
                                        [R_LiterBTI]
                                    )
                                    VALUES
                                    (
                                        @ID_Request,
                                        @Source,
                                        @RecivedAt,
                                        @CadastralNumber,
                                        @Address,
                                        @Square,
                                        'Кадастровый номер получен',
                                        @District,
                                        @City,
                                        @Town,
                                        @Street,
                                        @Home,
										@Corp,
                                        @Flat,
                                        @Ros_FullAddress,
                                        @Ros_ObjType,
                                        @Ros_Square,
                                        @Ros_SteadCategory,
                                        @Ros_SteadKind,
                                        @Ros_FuncName,
                                        @Ros_Status,
                                        @Ros_CadastralCost,
                                        @Ros_CadastralCostDate,
                                        @Ros_NumStoreys,
                                        @Ros_UpdateInfoDate,
                                        @Ros_LiterBTI
                                    )";

                    cmd.Parameters.AddWithValue("@ID_Request", original.ID_Request);
                    cmd.Parameters.AddWithValue("@RecivedAt", original.RecivedAt);
                    cmd.Parameters.AddWithValue("@Source", original.Source);
                    cmd.Parameters.AddWithValue("@CadastralNumber", addr.CadastralNumber);
                    cmd.Parameters.AddWithValue("@Address", original.Address);
                    cmd.Parameters.AddWithValue("@Square", original.Square);
                    cmd.Parameters.AddWithValue("@District", original.District, true);
                    cmd.Parameters.AddWithValue("@City", original.Town == "Ростов-на-Дону" ? original.Town : original.City, true);
                    cmd.Parameters.AddWithValue("@Town", original.Town == "Ростов-на-Дону" ? string.Empty : original.Town, true);
                    cmd.Parameters.AddWithValue("@Street", original.Street);
                    cmd.Parameters.AddWithValue("@Home", original.Home, true);
                    cmd.Parameters.AddWithValue("@Corp", original.Corp, true);
                    cmd.Parameters.AddWithValue("@Flat", original.Flat, true);
                    cmd.Parameters.AddWithValue("@Ros_FullAddress", addr.FullAddress);
                    cmd.Parameters.AddWithValue("@Ros_ObjType", addr.ObjType, true);
                    cmd.Parameters.AddWithValue("@Ros_Square", addr.Square, true);
                    cmd.Parameters.AddWithValue("@Ros_SteadCategory", addr.SteadCategory, true);
                    cmd.Parameters.AddWithValue("@Ros_SteadKind", addr.SteadKind, true);
                    cmd.Parameters.AddWithValue("@Ros_FuncName", addr.FuncName, true);
                    cmd.Parameters.AddWithValue("@Ros_Status", addr.Status, true);
                    cmd.Parameters.AddWithValue("@Ros_CadastralCost", addr.CadastralCost, true);
                    cmd.Parameters.AddWithValue("@Ros_CadastralCostDate", addr.CadastralCostDate, true);
                    cmd.Parameters.AddWithValue("@Ros_NumStoreys", addr.NumStoreys, true);
                    cmd.Parameters.AddWithValue("@Ros_UpdateInfoDate", addr.UpdateInfoDate, true);
                    cmd.Parameters.AddWithValue("@Ros_LiterBTI", addr.LiterBTI, true);

                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
            }
        }


        public static void RemoveSuccessedPreparedOrder(UnpreparedOrder order)
        {
            using (SqlConnection connection = Repository.GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"DELETE Pipeline 
                                    WHERE [ID] = @ID
                                    AND [Source] = @Source";

                cmd.Parameters.AddWithValue("@ID", order.ID);
                cmd.Parameters.AddWithValue("@Source", order.Source);

                cmd.ExecuteScalar();
            }
        }


        public static void SetAsPrepared(PreparedOrder order, DateTime time, string numReq, string worker)
        {
            //numReq = numReq.Insert(2, "-");

            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE [dbo].[Pipeline]
                                        SET [Worker] = @Worker,
                                            [Result] = 'В ожидании XML',
                                            [NumRequest] = @NumRequest,
                                            [RequestRecivedAt] = @DateSend
                                        WHERE [ID] = @id
                                        AND [Source] = @Source";

                cmd.Parameters.AddWithValue("@Worker", worker);
                cmd.Parameters.AddWithValue("@id", order.ID);
                cmd.Parameters.AddWithValue("@DateSend", time, true);
                cmd.Parameters.AddWithValue("@NumRequest", numReq, true);
                cmd.Parameters.AddWithValue("@Source", order.Source, true);

                cmd.ExecuteNonQuery();
            }
        }


        public static void SetIncorrect(PreparedOrder order, string worker)
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE [dbo].[Pipeline]
                                        SET [Worker] = @Worker,
                                            [Result] = 'Не корректный кадастровый',
                                            [IsChecked] = 0
                                        WHERE [ID] = @id
                                        AND [Source] = @Source";

                cmd.Parameters.AddWithValue("@Worker", worker);
                cmd.Parameters.AddWithValue("@id", order.ID);
                cmd.Parameters.AddWithValue("@Source", order.Source, true);

                cmd.ExecuteNonQuery();
            }
        }


        public static void SetNoAddressesFound(PreparedOrder order, string worker)
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE [dbo].[Pipeline]
                                        SET [Worker] = @Worker,
                                            [Result] = 'Не корректный кадастровый. Адресса не найдены',
                                            [IsChecked] = 0
                                        WHERE [ID] = @id
                                        AND [Source] = @Source";

                cmd.Parameters.AddWithValue("@Worker", worker);
                cmd.Parameters.AddWithValue("@id", order.ID);
                cmd.Parameters.AddWithValue("@Source", order.Source, true);

                cmd.ExecuteNonQuery();
            }
        }


        public static void SetMoreThanOneAddressesFound(PreparedOrder order, string worker)
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE [dbo].[Pipeline]
                                        SET [Worker] = @Worker,
                                            [Result] = 'Не корректный кадастровый. Больше одного адреса',
                                            [IsChecked] = 0
                                        WHERE [ID] = @id
                                        AND [Source] = @Source";

                cmd.Parameters.AddWithValue("@Worker", worker);
                cmd.Parameters.AddWithValue("@id", order.ID);
                cmd.Parameters.AddWithValue("@Source", order.Source, true);

                cmd.ExecuteNonQuery();
            }
        }


        public static IEnumerable<ObjPair<string, string>> GetXmlHrefs()
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"SELECT [Href],
                                           [FactoryType]
                                    FROM [dbo].[xmlhref]";

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new ObjPair<string, string>(reader.GetData<string>(0), reader.GetData<string>(1));
                    }
                }
            }
        }


        public static byte[] CompressDoc(string doc)
        {
            byte[] lightCompress = Encoding.UTF8.GetBytes(doc);

            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"SELECT dbo.compress(@lightbytes)";

                cmd.Parameters.AddWithValue(@"lightbytes", lightCompress);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        byte[] strongCompress = reader.GetData<byte[]>(0);
                        return strongCompress;
                    }
                }
            }
            throw new InvalidOperationException("Не удалось сжать документ");
        }

        public static byte[] Compress(string doc)
        {
            byte[] lightCompress = Encoding.GetEncoding(1251).GetBytes(doc);

            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"SELECT dbo.compress(@lightbytes)";

                cmd.Parameters.AddWithValue(@"lightbytes", lightCompress);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        byte[] strongCompress = reader.GetData<byte[]>(0);
                        return strongCompress;
                    }
                }
            }
            throw new InvalidOperationException("Не удалось сжать документ");
        }

        public static byte[] DeCompressDoc(byte[] doc)
        {


            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"SELECT dbo.decompress(@strongbytes)";

                cmd.Parameters.AddWithValue(@"strongbytes", doc);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        byte[] strongCompress = reader.GetData<byte[]>(0);
                        return strongCompress;
                    }
                }
            }
            throw new InvalidOperationException("Не удалось сжать документ");
        }

        public static void AddXmlData(int id, string xml, string html, StreamReader pdfMemoryReader, string href)
        {
            //byte pdfBytest = Convert.ToByte(pdfMemoryReader.ReadToEnd()); 
            //byte[] pdfBytest = Encoding.GetEncoding(1251).GetBytes(pdfMemoryReader.ReadToEnd());


            string pdfText = pdfMemoryReader.ReadToEnd();
            byte[] PdfByteS = Repository.Compress(pdfText);

            byte[] xmlByteS = CompressDoc(xml);
            byte[] htmlByteS = !string.IsNullOrWhiteSpace(html) ? CompressDoc(html) : null;
            //byte[] pdfBytes = !string.IsNullOrWhiteSpace(pdf) ? CompressDoc(pdf) : null;    1

            using (SqlCommand cmd = new SqlCommand(string.Empty, Repository.GetDebtConnection()))
            {
                try
                {
                    cmd.CommandText = @"INSERT INTO [dbo].[Xml]
                                    (
                                       [ID_Pipeline],
                                       [XmlData],
                                       [HtmlData],
									   [PdfData],
                                       [XslPath],
                                       [XmlSize],
                                       [HtmlSize]
                                    )
                                    VALUES
                                    (
                                       @ID,
                                       @XmlData,
                                       @HtmlData,
									   @PdfData,
                                       @XslPath,
                                       @XmlSize,
                                       @HtmlSize
                                    )

                                    IF 
                                    (SELECT COUNT(*) FROM Pipeline where ID_Request = (select ID_Request from Pipeline where ID = @ID)) = 1
	                                update Pipeline
	                                set IsResult = 1
	                                where ID = @ID";

                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.Parameters.AddWithValue("@XmlData", xmlByteS).SqlDbType = SqlDbType.VarBinary;
                    cmd.Parameters.AddWithValue("@HtmlData", htmlByteS, true).SqlDbType = SqlDbType.VarBinary;
                    cmd.Parameters.AddWithValue("@PdfData", PdfByteS, true).SqlDbType = SqlDbType.VarBinary;
                    cmd.Parameters.AddWithValue("@XslPath", href);
                    cmd.Parameters.AddWithValue("@XmlSize", (short)(xmlByteS.Length >> 10)); // размер в Кб
                    if (PdfByteS != null)
                    {
                        cmd.Parameters.AddWithValue("@HtmlSize", PdfByteS != null ? (short)(PdfByteS.Length >> 10) : (short?)null, true);
                    }
                    else
                        cmd.Parameters.AddWithValue("@HtmlSize", htmlByteS != null ? (short)(htmlByteS.Length >> 10) : (short?)null, true); // размер в Кб

                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
            }

            //byte[] PdfBytes = null;

            //         byte[] xmlBytes = CompressDoc(xml);
            //byte[] htmlBytes = !string.IsNullOrWhiteSpace(html) ? CompressDoc(html) : null;
            ////byte[] pdfBytes = !string.IsNullOrWhiteSpace(pdf) ? CompressDoc(pdf) : null;    1

            //using (SqlCommand cmd = new SqlCommand(string.Empty, Repository.GetDebtConnection()))
            //{
            //	try
            //	{
            //		cmd.CommandText = @"INSERT INTO [dbo].[Xml]
            //                                 (
            //                                    [ID_Pipeline],
            //                                    [XmlData],
            //                                    [HtmlData],
            //						   [PdfData],
            //                                    [XslPath],
            //                                    [XmlSize],
            //                                    [HtmlSize]
            //                                 )
            //                                 VALUES
            //                                 (
            //                                    @ID,
            //                                    @XmlData,
            //                                    @HtmlData,
            //						   @PdfData,
            //                                    @XslPath,
            //                                    @XmlSize,
            //                                    @HtmlSize
            //                                 )

            //                                 IF 
            //                                 (SELECT COUNT(*) FROM Pipeline where ID_Request = (select ID_Request from Pipeline where ID = @ID)) = 1
            //                              update Pipeline
            //                              set IsResult = 1
            //                              where ID = @ID";

            //		cmd.Parameters.AddWithValue("@ID", id);
            //		cmd.Parameters.AddWithValue("@XmlData", xmlBytes).SqlDbType = SqlDbType.VarBinary;
            //		cmd.Parameters.AddWithValue("@HtmlData", htmlBytes, true).SqlDbType = SqlDbType.VarBinary;
            //		cmd.Parameters.AddWithValue("@PdfData", PdfBytes, true).SqlDbType=SqlDbType.VarBinary;
            //		cmd.Parameters.AddWithValue("@XslPath", href);
            //		cmd.Parameters.AddWithValue("@XmlSize", (short)(xmlBytes.Length >> 10)); // размер в Кб
            //		if (PdfBytes != null)
            //		{
            //			cmd.Parameters.AddWithValue("@HtmlSize", PdfBytes != null ? (short)(PdfBytes.Length >> 10) : (short?)null, true);
            //		}
            //		else
            //			cmd.Parameters.AddWithValue("@HtmlSize", htmlBytes != null ? (short)(htmlBytes.Length >> 10) : (short?)null, true); // размер в Кб

            //                 cmd.ExecuteNonQuery();
            //	}
            //	catch
            //	{
            //		throw;
            //	}
            //}
        }


        public static void CreateEGRP(IEnumerable<EGRP> EGRPs)
        {
            using (RepositoryTransaction tr = Repository.BeginTransaction())
            {
                try
                {
                    foreach (var item in EGRPs)
                        CreateEGRP(item, tr.Transaction);

                    tr.Commit();
                }
                catch
                {
                    tr.Rollback();
                    throw;
                }
            }
        }


        public static void CreateEGRP(EGRP egrp, SqlTransaction tr)
        {
            using (SqlCommand cmd = new SqlCommand(string.Empty, tr.Connection, tr))
            {
                try
                {
                    cmd.CommandText = @"INSERT INTO [dbo].[EGRP]
                                    (
                                        [ID_Pipeline],
                                        [FIO],
                                        [RegDate],
                                        [Numerator],
                                        [Denominator],
										[Comment]
                                    )
                                    VALUES
                                    (
                                        @ID_Pipeline,
                                        @FIO,
                                        @RegDate,
                                        @Numerator,
                                        @Denominator,
										@Comment
                                    )

                                    UPDATE Pipeline
                                    SET [Result] = @Result,
                                        [HasXml] = 1
                                    WHERE [ID] = @ID_Pipeline";

                    cmd.Parameters.AddWithValue("@ID_Pipeline", egrp.ID_Pipeline);
                    cmd.Parameters.AddWithValue("@FIO", egrp.FIO);
                    cmd.Parameters.AddWithValue("@RegDate", egrp.DateReg);
                    cmd.Parameters.AddWithValue("@Numerator", egrp.Fraction.Numerator);
                    cmd.Parameters.AddWithValue("@Denominator", egrp.Fraction.Denominator);
                    cmd.Parameters.AddWithValue("@Comment", egrp.FullFraction, true);
                    cmd.Parameters.AddWithValue("@Result", "Получены сведения о собственнике");

                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
            }
        }

        public static void CreateEGRP_NoXmlData(int id)
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                try
                {
                    cmd.CommandText = @"INSERT INTO [dbo].[EGRP]
                                    (
                                        [ID_Pipeline],
                                        [FIO],
                                        [RegDate],
                                        [Numerator],
                                        [Denominator],
										[Comment]
                                    )
                                    VALUES
                                    (
                                        @ID_Pipeline,
                                        @FIO,
                                        @RegDate,
                                        @Numerator,
                                        @Denominator,
										@Comment
                                    )";

                    cmd.Parameters.AddWithValue("@ID_Pipeline", id);
                    cmd.Parameters.AddWithValue("@FIO", "Отсутствуют сведения о собственнике");
                    cmd.Parameters.AddWithValue("@RegDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@Numerator", 0);
                    cmd.Parameters.AddWithValue("@Denominator", 0);
                    cmd.Parameters.AddWithValue("@Comment", 0);

                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
            }
        }

        public static void SetGovResult(int id)
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE Pipeline
                                    SET [Result] = 'Собственник ДИЗО',
                                        [HasXml] = 1
                                    WHERE [ID] = @ID";

                cmd.Parameters.AddWithValue("@ID", id);

                cmd.ExecuteScalar();
            }
        }


        public static void SetOrganizationResult(int id)
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE Pipeline
                                    SET [Result] = 'Собственник ДИЗО',
                                        [HasXml] = 1
                                    WHERE [ID] = @ID";

                cmd.Parameters.AddWithValue("@ID", id);

                cmd.ExecuteScalar();
            }
        }

        public static void SetNoXmlData(int id)
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE Pipeline
                                    SET [Result] = 'Отсутствуют сведения о собственнике',
                                        [HasXml] = 1
                                    WHERE [ID] = @ID";

                cmd.Parameters.AddWithValue("@ID", id);

                cmd.ExecuteScalar();
            }
        }


        public static void UpdateLastUploadAttempt(int id)
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE Pipeline
                                    SET [LastUploadAttempt] = @time
                                    WHERE [ID] = @ID";

                cmd.Parameters.AddWithValue("@ID", id);
                cmd.Parameters.AddWithValue("@time", DateTime.Now);

                cmd.ExecuteNonQuery();
            }
        }


        public static void SetAnul(PreparedOrder order)
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE Pipeline
                                    SET [Result] = 'Анулирован',
                                    [IsChecked] = 0
                                    WHERE [ID] = @ID
                                    AND [Source] = @Source";

                cmd.Parameters.AddWithValue("@ID", order.ID);
                cmd.Parameters.AddWithValue("@Source", order.Source);

                cmd.ExecuteScalar();
            }
        }


        public static void RollBackToPrepared(int id)
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE Pipeline
                                    SET [Worker] = null,
                                        [Result] = 'Потерян Росреестром',
                                        [NumRequest] = null,
                                        [RequestRecivedAt] = null,
                                        [IsChecked] = 0
                                    WHERE [ID] = @ID";

                cmd.Parameters.AddWithValue("@ID", id);

                cmd.ExecuteScalar();
            }
        }

        public static void RollbackToThePreviousVersion(int id)
        {
            using (SqlConnection connection = GetDebtConnection())
            using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
            {
                cmd.CommandText = @"UPDATE Pipeline
									SET [NumRequest] = null,
										[Result] = 'Кадастровый номер получен'
									WHERE [ID] = @ID";
                cmd.Parameters.AddWithValue("@ID", id);
                cmd.ExecuteScalar();
            }

        }


        public static Tuple<string, string> GetEmailAndCadastral(string source, int id)
        {
            try
            {
                using (SqlConnection connection = GetDebtConnection())
                using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
                {
                    cmd.CommandText = @"SELECT Sources.Email, Pipeline.CadastralNumber
                                        FROM [dbo].[Sources]
                                        join Pipeline ON Pipeline.ID = @ID
                                        WHERE Sources.Source = @Source";

                    cmd.Parameters.AddWithValue(@"Source", source);
                    cmd.Parameters.AddWithValue(@"ID", id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string email = reader.GetData<string>(0);
                            string cadastral = reader.GetData<string>(1);
                            return new Tuple<string, string>(email, cadastral);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }


        public static string GetEmail(string source)
        {
            try
            {
                using (SqlConnection connection = GetDebtConnection())
                using (SqlCommand cmd = new SqlCommand(string.Empty, connection))
                {
                    cmd.CommandText = @"SELECT Email
                                        FROM [dbo].[Sources]
                                        WHERE Source = @Source";

                    cmd.Parameters.AddWithValue(@"Source", source);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetData<string>(0);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}