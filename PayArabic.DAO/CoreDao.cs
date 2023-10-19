using System.Text;

namespace PayArabic.DAO;

public class CoreDao : BaseDao, ICoreDao
{
    public void TokenSave(string tokenText, long userId, bool isRefreshToken = false, bool isAccessToken = false)
    {
        string query = @"   DECLARE @TokenText NVARCHAR(MAX) = '" + tokenText + @"'
                            DECLARE @UserId INT = " + userId + @"
                            DECLARE @IsRefreshToken BIT = '" + isRefreshToken + @"'
                            DECLARE @IsAccessToken BIT = '" + isAccessToken + @"'
                            
                            INSERT INTO Token
		                        (UserId, [Text], InActive, CreatedBy, CreateDate, UpdateDate)
	                        VALUES 
                                (@UserId, @TokenText, 0, @UserId, GETDATE(), GETDATE()); ";
        DB.Execute(query);
    }
    public void TokenDelete(long userId)
    {
        string query = @"   DECLARE @UserId INT = " + userId + @"
                            DELETE FROM Token WHERE UserId = @UserId; ";
        DB.Execute(query);
    }
    public UserDTO.Light TokenGetUser(string tokenText)
    {
        throw new NotImplementedException();
    }

    
    public void InitSystem(List<SysPermissionDTO.Module> list)
    {
        byte[] passwordHash, passwordSalt;
        Utility.CreatePasswordHash("S#)5%^A~!20ZA_23"+ AppSettings.Instance.UserPasswordKey, out passwordHash, out passwordSalt);
        StringBuilder query = new StringBuilder();
        query.AppendLine(@" BEGIN TRANSACTION [InitSystem]
                            BEGIN TRY
                                DECLARE @UserPermissionId BIGINT = 0;
                                DECLARE @ModuleId BIGINT = 0;
                                DECLARE @FunctionId BIGINT = 0;
                                DECLARE @CurrentDate DATETIME = (SELECT GETDATE());
                                DECLARE @FirstInit BIT = 0;

                                UPDATE SysModule SET InActive = 1, DeletedBy = 0, DeleteDate = @CurrentDate;

                                IF NOT EXISTS (SELECT 1 FROM [User] WHERE Id = -1)
                                BEGIN
                                    SET IDENTITY_INSERT [User] ON 
                                    INSERT [User] ([Id], ParentId, [UserType], [Name]
                                        , Email, PasswordHash, PasswordSalt, ContactActivated, Reviewed
                                        , [InActive], [CreatedBy], [CreateDate], [UpdateDate]) 
                                    VALUES (-1, -1, N'SystemAdmin', N'System Admin'
                                        , 'SystemAdmin@PayArabic.Com',@passwordHash, @passwordSalt, 1, 1
                                        , 0, 0, @CurrentDate, @CurrentDate)
                                    SET IDENTITY_INSERT [User] OFF

                            -- First Initialization
                                    SET @FirstInit = 1;
                                END
                                ");
        foreach (SysPermissionDTO.Module m in list)
        {
            query.AppendLine(@" SET @UserPermissionId = 0;
                                SET @ModuleId = 0;
                               
                                SELECT @ModuleId = ISNULL(Id, 0) FROM SysModule WHERE [Name] = '" + m.Name+ @"';
                                
                                IF @ModuleId = 0
                                BEGIN
                                    INSERT INTO SysModule ([Name], DisplayName, Permission
                                                        , Auditable, InActive, CreatedBy, UpdatedBy, CreateDate, UpdateDate)
                                    VALUES ('" + m.Name + @"', '"+m.DisplayName+@"', '"+m.Permission+@"'
                                                        , '"+m.Auditable+ @"', 0, 0, 0, @CurrentDate, @CurrentDate);
                                    SELECT @ModuleId = SCOPE_IDENTITY();
                                END
                                ELSE
                                BEGIN
                                    UPDATE SysModule SET
                                        [Name] = '" + m.Name + @"',
                                        [DisplayName] = '" + m.DisplayName + @"',
                                        [Permission] = '" + m.Permission + @"',
                                        [Auditable] = '" + m.Auditable + @"',
                                        [InActive] = 0,
                                        [UpdateDate] = @CurrentDate,
                                        [DeletedBy] = NULL,
                                        [DeleteDate] = NULL
                                    WHERE Id = @ModuleId;
                                END
                            ");
            foreach(SysPermissionDTO.Function f in m.Functions)
            {
                query.AppendLine(@" SET @UserPermissionId = 0;
                                    SET @FunctionId = 0;
                                    SELECT @FunctionId = ISNULL(Id, 0) 
                                    FROM SysFunction 
                                    WHERE ModuleId = @ModuleId 
                                        AND [Name] = '" + f.Name + @"'
                                        AND [DisplayName] = '" + f.DisplayName + @"';
                                
                                    IF @FunctionId = 0
                                    BEGIN
                                        INSERT INTO SysFunction (ModuleId, [Name], DisplayName, Permission
                                                            , Auditable, InActive, CreatedBy, UpdatedBy, CreateDate, UpdateDate)
                                        VALUES (@ModuleId, '" + f.Name + @"', '" + f.DisplayName + @"', '" + f.Permission + @"'
                                                            , '" + f.Auditable + @"', 0, 0, 0, @CurrentDate, @CurrentDate);
                                        SELECT @FunctionId = SCOPE_IDENTITY();
                                    END
                                    ELSE
                                    BEGIN
                                        UPDATE SysFunction SET
                                            [Name] = '" + f.Name + @"',
                                            [DisplayName] = '" + f.DisplayName + @"',
                                            [Permission] = '" + f.Permission + @"',
                                            [Auditable] = '" + f.Auditable + @"',
                                            [InActive] = 0,
                                            [UpdateDate] = @CurrentDate
                                        WHERE Id = @FunctionId;
                                    END
                                -- Add default function to system admin
                                    SELECT @UserPermissionId = ISNULL(Id, 0) 
                                    FROM UserPermission 
                                    WHERE UserId = -1 AND ModuleId = @ModuleId AND FunctionId = @FunctionId;
                                    IF @UserPermissionId = 0
                                    BEGIN
                                        INSERT INTO UserPermission(UserId, ModuleId, FunctionId
                                            , FunctionPermission, FunctionAuditable
                                            , InActive, CreatedBy, UpdatedBy, CreateDate, UpdateDate)
                                        VALUES (-1, @ModuleId, @FunctionId
                                            , '" + f.Permission + @"', '" + f.Auditable + @"'
                                            , 0, 0, 0, @CurrentDate, @CurrentDate);  
                                    END
                                ");
            }
        }
        query.AppendLine(@" 
                            IF @FirstInit = 1
                            BEGIN
                            -- First Initialization
                            -- Add default usertype permission

                                -- Super admin will take all permission
                                    INSERT INTO SysFunctionUserType(FunctionId, UserType, InActive, CreatedBy, CreateDate, UpdatedBy, UpdateDate)
                                    SELECT Id, 'SuperAdmin', 0, 0, GETDATE(), 0, GETDATE() 
                                    FROM SysFunction WHERE InActive = 0 AND DeletedBy IS NULL;

                                -- Admin will take all permission except audit and currency
                                    INSERT INTO SysFunctionUserType(FunctionId, UserType, InActive, CreatedBy, CreateDate, UpdatedBy, UpdateDate)
                                    SELECT f.Id, 'Admin', 0, 0, GETDATE(), 0, GETDATE() 
                                    FROM SysFunction f INNER JOIN SysModule m ON f.ModuleId = m.Id
                                    WHERE m.InActive = 0 AND m.DeletedBy IS NULL
                                    AND f.InActive = 0 AND f.DeletedBy IS NULL
                                    AND m.[Name] NOT IN ('Audit','Currency');

                                -- Vendor will take (Deposit, Integration, Invoice, PaymentLink, User)
                                    INSERT INTO SysFunctionUserType(FunctionId, UserType, InActive, CreatedBy, CreateDate, UpdatedBy, UpdateDate)
                                    SELECT f.Id, 'Vendor', 0, 0, GETDATE(), 0, GETDATE() 
                                    FROM SysFunction f INNER JOIN SysModule m ON f.ModuleId = m.Id
                                    WHERE m.InActive = 0 AND m.DeletedBy IS NULL
                                    AND f.InActive = 0 AND f.DeletedBy IS NULL
                                    AND m.[Name] IN ('Deposit','Integration','Invoice','PaymentLink', 'User');

                                -- User will take (Integration, Invoice, User)
                                    INSERT INTO SysFunctionUserType(FunctionId, UserType, InActive, CreatedBy, CreateDate, UpdatedBy, UpdateDate)
                                    SELECT f.Id, 'User', 0, 0, GETDATE(), 0, GETDATE() 
                                    FROM SysFunction f INNER JOIN SysModule m ON f.ModuleId = m.Id
                                    WHERE m.InActive = 0 AND m.DeletedBy IS NULL
                                    AND f.InActive = 0 AND f.DeletedBy IS NULL
                                    AND m.[Name] IN ('Integration','Invoice','User');
                                END
                            COMMIT TRANSACTION [InitSystem] 
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [InitSystem]
                            END CATCH");
        DB.Execute(query.ToString(), new { passwordHash = passwordHash, passwordSalt = passwordSalt });
    }


    //public void InitSystem2(List<SysModuleDTO> list)
    //{
    //    StringBuilder query = new StringBuilder();
    //    query.AppendLine(@" BEGIN TRANSACTION [InitSystem]
    //                        BEGIN TRY
    //                            DECLARE @ModuleId BIGINT = 0;
    //                            TRUNCATE TABLE SysModule;
    //                            TRUNCATE TABLE SysFunction;
    //                    ");
    //    foreach (SysModuleDTO m in list)
    //    {
    //        query.AppendLine(@" SET @ModuleId = 0;
    //                            INSERT INTO SysModule ([Name], DisplayName, Permission
    //                                                , Auditable, InActive, CreatedBy, CreateDate, UpdateDate)
    //                            VALUES ('" + m.ModuleName + @"', '" + m.ModuleDisplayName + @"', '" + m.ModulePermission + @"'
    //                                                , '" + m.ModuleAuditable + @"', 0, 0, GETDATE(), GETDATE());
    //                            SELECT @ModuleId = SCOPE_IDENTITY();
    //                        ");
    //        foreach (SysFunctionDTO f in m.Functions)
    //        {
    //            query.AppendLine(@" INSERT INTO SysFunction (ModuleId, [Name], DisplayName, Permission
    //                                                    , Auditable, InActive, CreatedBy, CreateDate, UpdateDate)
    //                                VALUES (@ModuleId, '" + f.FunctionName + @"', '" + f.FunctionDisplayName + @"', '" + f.FunctionPermission + @"'
    //                                                    , '" + f.FunctionAuditable + @"', 0, 0, GETDATE(), GETDATE());
    //                            ");
    //        }
    //    }
    //    query.AppendLine(@" COMMIT TRANSACTION [InitSystem] 
    //                        END TRY
    //                        BEGIN CATCH
    //                            ROLLBACK TRANSACTION [InitSystem]
    //                        END CATCH");
    //    DB.Execute(query.ToString());
    //}
}