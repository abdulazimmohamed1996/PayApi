using Newtonsoft.Json;
using PayArabic.Core.Model;
using System.Text;
using System.Text.RegularExpressions;
using static Dapper.SqlMapper;

namespace PayArabic.DAO;

public class UserDao : BaseDao, IUserDao
{
    public ResponseDTO Register(UserDTO.Register entity)
    {
        //if (!Utility.ValidateCaptcha(entity.recaptchaToken, "signUpAction", AppSettings.Instance.RecaptchaSecretKey, AppSettings.Instance.RecaptchaPassword).Result)
        //    return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidRecaptcha", Response = null };
        if (string.IsNullOrEmpty(entity.Name)) return new ResponseDTO() { IsValid = false, ErrorKey = "NameRequired", Response = null };
        if (!Regex.IsMatch(entity.Name, @"^[a-zA-Z \u0621-\u064A]+$")) return new ResponseDTO() { IsValid = false, ErrorKey = "NameAcceptOnlyLetters", Response = null };
        if (string.IsNullOrEmpty(entity.Email)) return new ResponseDTO() { IsValid = false, ErrorKey = "EmailRequired", Response = null };
        var regex = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z";
        if (!Regex.IsMatch(entity.Email, regex, RegexOptions.IgnoreCase)) return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidEmailAddress", Response = null };
        if (!string.IsNullOrEmpty(entity.Mobile)) entity.Mobile = Utility.toEnglishNumber(entity.Mobile);
        if (string.IsNullOrEmpty(entity.Password)) return new ResponseDTO() { IsValid = false, ErrorKey = "PasswordRequired", Response = null };
        if (string.IsNullOrEmpty(entity.Mobile)) return new ResponseDTO() { IsValid = false, ErrorKey = "MobileRequired", Response = null };
        if (string.IsNullOrEmpty(entity.AccountType) || (entity.AccountType != "HomeBusiness" && entity.AccountType != "Business")) return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidBusiness", Response = null };
        if (entity.AccountType == "Business" && string.IsNullOrEmpty(entity.CompanyName)) return new ResponseDTO() { IsValid = false, ErrorKey = "CompanyNameRequired", Response = null };

        byte[] passwordHash, passwordSalt;
        entity.Password = entity.Password + AppSettings.Instance.UserPasswordKey;
        Utility.CreatePasswordHash(entity.Password, out passwordHash, out passwordSalt);

        string emailActivationKey = Utility.GetRandomEmailKey();
        string mobileActivationKey = Utility.GetRandomMobileKey();
        string validateQuery = @"   DECLARE @Error NVARCHAR(MAX)='';
                                    IF (SELECT COUNT(Id) FROM [User] WHERE Email = '" + Utility.Wrap(entity.Email) + @"') > 0 
                                        BEGIN SET @Error = N'EmailAlreadyExists'; END
                                    ELSE IF (SELECT COUNT(Id) FROM [User] WHERE Mobile = '" + Utility.Wrap(entity.Mobile) + @"') > 0
                                         BEGIN SET @Error = N'MobileAlreadyExists'; END
                        ";
        if (!string.IsNullOrEmpty(entity.CompanyName))
            validateQuery += @" ELSE IF (SELECT COUNT(Id) FROM [Vendor] WHERE CompanyName LIKE N'" + Utility.Wrap(entity.CompanyName) + @"') > 0 
                                    BEGIN SET @Error = N'CompanyNameAlreadyExists'; END ";
        validateQuery += "  SELECT @Error";
        var error = DB.ExecuteScalar<string>(validateQuery);
        if (error != null && !string.IsNullOrEmpty(error))
            return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };

        //, CompanyName, WorkPhone, WebSiteUrl
        //                    , AccountType , InvoiceLang, InvoiceValidDays, 

        string default_PaymentMethods = new ConfigurationDao().GetConfigurationByName("DefaultVendorsPaymentMethods").ToString();
        string[] payment_Methods = default_PaymentMethods.Split(",");

        StringBuilder query = new StringBuilder();
        query.AppendLine("BEGIN TRANSACTION [Register]");
        query.AppendLine("BEGIN TRY");
        query.AppendLine(@" 
                            -- Insert user record
                                INSERT INTO [User] (ParentId, UserType, [Name]
                                        , Mobile, Email, PasswordHash, PasswordSalt
                                        , EmailActivationKey, MobileActivationKey, ContactActivated, AccountType
                                        , InActive, CreatedBy, CreateDate, UpdateDate)
                                Values(0, N'" + UserType.Vendor.ToString() + "', N'" + Utility.Wrap(entity.Name) + @"'
                                        , N'" + Utility.Wrap(entity.Mobile) + @"', N'" + Utility.Wrap(entity.Email) + @"', @passwordHash, @passwordSalt
                                        , '" + emailActivationKey + "', '" + mobileActivationKey + @"', 0, N'" + Utility.Wrap(entity.AccountType) + @"'
                                        , 0, 0, GETDATE(), GETDATE());
                                DECLARE @NewUserId BIGINT = 0;
                                SELECT @NewUserId = SCOPE_IDENTITY();
                                EXECUTE dbo.GenerateEntityCode 'User', @NewUserId;

                            -- Insert user permission record
                                INSERT INTO UserPermission(UserId, ModuleId, FunctionId, FunctionPermission, FunctionAuditable
                                    , InActive, CreatedBy, CreateDate, UpdatedBy, UpdateDate)
                                SELECT @NewUserId, f.ModuleId, fu.FunctionId, f.Permission, f.Auditable
                                    , 0, 0, GETDATE(), 0, GETDATE()
                                FROM SysFunction f
	                                INNER JOIN SysFunctionUserType fu ON f.Id = fu.FunctionId
                                WHERE fu.UserType = 'Vendor';

                            -- Insert vendor record
                                INSERT INTO Vendor (UserId, CompanyName, WorkPhone
                                                , WebSiteUrl, InvoiceLang, InvoiceValidDays
                                                , InActive, CreatedBy, CreateDate, UpdateDate)
                                Values(@NewUserId, N'" + Utility.Wrap(entity.CompanyName) + @"', N'" + Utility.Wrap(entity.WorkPhone) + @"'
                                                , N'" + Utility.Wrap(entity.WebSiteUrl) + @"', 'En', 30
                                                , 0, 0, GETDATE(), GETDATE());

                            -- Insert event record for email
                                INSERT INTO [Event] ([Type], SendType, EntityType, EntityId
                                            , Sent, InActive, CreatedBy, CreateDate, UpdateDate)
                                VALUES ('" + EventType.UserRegister + @"', '" + SendType.Email + @"', N'" + EntityType.User + @"', @NewUserId
                                            , 0, 0, 0, GETDATE(), GETDATE());

                            -- Insert event record for sms
                                INSERT INTO [Event] ([Type], SendType, EntityType, EntityId
                                            , Sent, InActive, CreatedBy, CreateDate, UpdateDate)
                                VALUES ('" + EventType.UserRegister + @"', '" + SendType.SMS + @"', N'" + EntityType.User + @"', @NewUserId
                                            , 0, 0, 0, GETDATE(), GETDATE());
                        
                        ");
        foreach (string payment_Method in payment_Methods)
        {
            string[] options = payment_Method.Split("|");
            query.AppendLine(@"
                                INSERT INTO [UserPaymentMethod] ([UserId], PaymentMethodId, FeesType, FeesFixedAmount, FeesPercentAmount
                                        , PaidBy, InActive, CreatedBy, CreateDate, UpdateDate)
                                VALUES (@NewUserId," + options[0] + @", '" + options[1] + @"', " + options[2] + @"," + options[3] + @"
                                        ,'" + options[4] + @"', 0, 0, GETDATE(), GETDATE()); 
                            ");
        }

        query.AppendLine(@" SELECT @NewUserId;
                            COMMIT TRANSACTION [Register]
                            END TRY
                                BEGIN CATCH
                                ROLLBACK TRANSACTION [User_Register]
                                --SELECT  ERROR_NUMBER() AS ErrorNumber, ERROR_SEVERITY() AS ErrorSeverity ,ERROR_STATE() AS ErrorState, ERROR_PROCEDURE() AS ErrorProcedure, ERROR_LINE() AS ErrorLine, ERROR_MESSAGE() AS ErrorMessage;  
                            END CATCH");
        var userId = DB.ExecuteScalar(query.ToString(), new { passwordHash = passwordHash, passwordSalt = passwordSalt });
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = userId };
    }
    public ResponseDTO Login(UserDTO.Login entity)
    {
        //if (!Utility.ValidateCaptcha(entity.recaptchaToken, "loginAction", AppSettings.Instance.RecaptchaSecretKey, AppSettings.Instance.RecaptchaPassword).Result)
        //    return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidRecaptcha", Response = null };

        if (string.IsNullOrEmpty(entity.Email))
            return new ResponseDTO() { IsValid = false, ErrorKey = "EmailRequired", Response = null };
        if (string.IsNullOrEmpty(entity.Password))
            return new ResponseDTO() { IsValid = false, ErrorKey = "PasswordRequired", Response = null };

        int LoginLockDuration = Convert.ToInt32(new ConfigurationDao().GetConfigurationByName("LoginLockDuration")) * 60;
        string query = @"   DECLARE @Error NVARCHAR(MAX)='';
                            DECLARE @UserId BIGINT = 0;
                            DECLARE @LoginLockDate DATETIME = NULL;
                            DECLARE @LoginLockEnabled BIT = 0;
                            SELECT @UserId = Id, @LoginLockDate = ISNULL(LoginLockDate, GETDATE())
                                    , @LoginLockEnabled = ISNULL(LoginLockEnabled, 0)
                            FROM [User]
                            WHERE DeletedBy IS NULL AND InActive = 0
	                            AND Email = N'" + Utility.Wrap(entity.Email) + @"'

                            IF  @LoginLockEnabled = 1 AND DATEDIFF(SECOND, @LoginLockDate, GETDATE()) < " + LoginLockDuration.ToString() + @"
	                            BEGIN SET @Error = N'AccountLoginTempLock'; END
                            ELSE IF  @LoginLockEnabled = 1 AND DATEDIFF(SECOND, @LoginLockDate, GETDATE()) > " + LoginLockDuration.ToString() + @"
	                            BEGIN UPDATE [User] SET LoginAccessFailed = 0, LoginLockDate = GETDATE(), LoginLockEnabled = 0 WHERE Id = @UserId; END

                            SELECT @Error Error, u.Id, u.ParentId, u.UserType
                                , u.[Name], u.Mobile, u.Email, ISNULL(u.Reviewed, 0) Reviewed
                                , u.PasswordHash, u.PasswordSalt, u.LoginAccessFailed
                                , u.LoginLockDate, u.LoginLockEnabled, u.ContactActivated, u.InActive
                                , v.CompanyName
                            FROM [User] u
                                LEFT JOIN Vendor v ON v.UserId = u.Id
                            WHERE u.Id = @UserId;
                        ";
        UserDTO.Info userObj = DB.Query<UserDTO.Info>(query.ToString()).FirstOrDefault();
        if (userObj == null)
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidEmailOrPassword", Response = null };
        else if (!string.IsNullOrEmpty(userObj.Error))
            return new ResponseDTO() { IsValid = false, ErrorKey = userObj.Error, Response = null };
        else
        {
            bool loginFailed = true;
            string error = string.Empty;
            string password = entity.Password + AppSettings.Instance.UserPasswordKey;
            bool isValidPassword = Utility.VerifyPasswordHash(password, userObj.PasswordHash, userObj.PasswordSalt);

            if (!isValidPassword)
                error = "InvalidEmailOrPassword";
            else if (!userObj.ContactActivated)
                error = "ContactActivationRequired";
            else if (userObj.InActive)
                error = "AccountNotActive";
            else
                loginFailed = false;
            if (loginFailed)
            {
                query = @"  UPDATE [User] SET 
                                LoginAccessFailed = ISNULL(LoginAccessFailed, 0) + 1
                                , LoginLockDate = GETDATE()
                                , LoginLockEnabled = 0 
                            WHERE Id = " + userObj.Id + @"
                            IF (SELECT ISNULL(LoginAccessFailed, 0) FROM [User] WHERE Id = " + userObj.Id + @") >= 3
                                BEGIN UPDATE [User] SET LoginLockDate = GETDATE(), LoginLockEnabled = 1 WHERE Id = " + userObj.Id + @" END";
                DB.Execute(query.ToString());

                return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };
            }
            UserDTO.Light user = new UserDTO.Light()
            {
                Id = userObj.Id,
                Email = userObj.Email,
                Mobile = userObj.Mobile,
                Name = userObj.Name,
                UserType = userObj.UserType,
                ParentId = userObj.ParentId,
                CompanyName = userObj.CompanyName,
                Reviewed = userObj.Reviewed
            };
            return new ResponseDTO() { IsValid = true, ErrorKey = error, Response = user };
        }
    }
    public ResponseDTO Activate(long currentUserId, string currentUserType, long id, bool activate)
    {
        string query = @"   
                            DECLARE @Error NVARCHAR(MAX)='';
                            DECLARE @UserType NVARCHAR(50) = '';
                            
                            SELECT @UserType = UserType 
                            FROM [User] WHERE Id = " + id + @"

                            IF '" + currentUserType + "' <> '" + UserType.SystemAdmin.ToString() + @"'
                            BEGIN
                                IF (@UserType = '" + UserType.SuperAdmin.ToString() + @"' OR @UserType = '" + UserType.Admin.ToString() + @"')
                                    AND '" + currentUserType + "' <> '" + UserType.SuperAdmin.ToString() + @"'
                                    BEGIN SET @Error = N'UnauthorizedAccess'; END
                            
                                ELSE IF @UserType = '" + UserType.Vendor.ToString() + @"' 
                                    AND NOT ('" + currentUserType + "' = '" + UserType.SuperAdmin.ToString() + @"' 
                                            OR '" + currentUserType + "' = '" + UserType.Admin.ToString() + @"' 
                                            )
                                    BEGIN SET @Error = N'UnauthorizedAccess'; END

                                ELSE IF @UserType = '" + UserType.User.ToString() + @"' 
                                    AND NOT (
                                            --'" + currentUserType + "' = '" + UserType.SuperAdmin.ToString() + @"' 
                                            -- OR '" + currentUserType + "' = '" + UserType.Admin.ToString() + @"' 
                                            -- OR 
                                            '" + currentUserType + "' = '" + UserType.Vendor.ToString() + @"' 
                                            )
                                    BEGIN SET @Error = N'UnauthorizedAccess'; END
    
                                ELSE
                                BEGIN
                                    Update [User] SET ParentId =" + currentUserId + @"
                                        , UpdatedBy = " + currentUserId + @"
                                        , InActive = '" + activate + @"'
                                        --, ContactActivated = '" + activate + @"' 
                                    WHERE Id = " + id + @"
                                END
                            END
                            ELSE
                                BEGIN
                                   Update [User] SET ParentId =" + currentUserId + @"
                                        , UpdatedBy = " + currentUserId + @"
                                        , InActive = '" + activate + @"'
                                        --, ContactActivated = '" + activate + @"' 
                                    WHERE Id = " + id + @"
                                END
                            
                            SELECT @Error;";
        var error = DB.ExecuteScalar<string>(query);
        if (error != null && !string.IsNullOrEmpty(error))
            return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };

        return new ResponseDTO() { IsValid = true };
    }
    public ResponseDTO ActivateEmail(string emailActivationKey, string mobileActivationKey)
    {
        string query = @"   DECLARE @Error NVARCHAR(MAX)='';
                            DECLARE @Id BIGINT = 0;
                            SELECT @Id = ISNULL(Id, 0) 
                            FROM [User] WHERE EmailActivationKey = '" + emailActivationKey + @"' 
                                AND MobileActivationKey = '" + mobileActivationKey + @"'
                            IF @Id <= 0
                                BEGIN SET @Error = N'WrongActivationKey'; END
                            ELSE
                                BEGIN
                                    Update [User] SET ContactActivated = 1 WHERE Id = @Id;
                                END 
                            SELECT @Error;";
        var error = DB.ExecuteScalar<string>(query);
        if (error != null && !string.IsNullOrEmpty(error))
            return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };

        return new ResponseDTO() { IsValid = true };
    }
    public ResponseDTO ForgetPassword(string email, string mobile, string recaptchaToken)
    {
        if (!Utility.ValidateCaptcha(recaptchaToken, "forgetPasswordAction", AppSettings.Instance.RecaptchaSecretKey, AppSettings.Instance.RecaptchaPassword).Result)
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidRecaptcha", Response = null };

        string emailActivationKey = Utility.GetRandomEmailKey();
        string mobileActivationKey = Utility.GetRandomMobileKey();

        email = Utility.HtmlEntitesDecode(email);
        mobile = Utility.HtmlEntitesDecode(mobile);

        string query = @"   DECLARE @Error NVARCHAR(MAX)='';
                            DECLARE @UserId BIGINT = 0;
                            DECLARE @ContactActivated BIT = 0;
                            SELECT @UserId = ISNULL(Id, 0), @ContactActivated = ISNULL(ContactActivated, 0) 
                            FROM [User] WHERE Email = '" + Utility.Wrap(email) + "' AND Mobile = '" + Utility.Wrap(mobile) + @"'
                            IF @UserId <= 0
                                BEGIN SET @Error = N'InvalidEmailOrMobile'; END
                            ELSE
                                BEGIN
                                    -- Update user activation keys
                                    UPDATE [User] SET 
                                        EmailActivationKey='" + emailActivationKey + @"'
                                        , MobileActivationKey='" + mobileActivationKey + @"'
                                    WHERE Id = @UserId;

                                    -- Add event records
                                   
                                    INSERT INTO [Event] ([Type], SendType, EntityType, EntityId
                                                , Sent, InActive, CreatedBy, CreateDate, UpdateDate)
                                    VALUES ('" + EventType.UserForgetPassword + @"', '" + SendType.Email + @"', N'" + EntityType.User + @"', @UserId
                                                , 0, 0, 0, GETDATE(), GETDATE());

                                    INSERT INTO [Event] ([Type], SendType, EntityType, EntityId
                                                , Sent, InActive, CreatedBy, CreateDate, UpdateDate)
                                    VALUES ('" + EventType.UserForgetPassword + @"', '" + SendType.SMS + @"', N'" + EntityType.User + @"', @UserId
                                                , 0, 0, 0, GETDATE(), GETDATE());
                                END
                            SELECT @Error;";
        var error = DB.ExecuteScalar<string>(query);
        if (error != null && !string.IsNullOrEmpty(error))
            return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };

        return new ResponseDTO() { IsValid = true };
    }
    public ResponseDTO ForgetPasswordRecovery(UserDTO.RestPassword entity)
    {
        if (string.IsNullOrEmpty(entity.EmailActivationKey)) return new ResponseDTO() { IsValid = false, ErrorKey = "EmailActivationKeyRequired", Response = null };
        if (string.IsNullOrEmpty(entity.MobileActivationCode)) return new ResponseDTO() { IsValid = false, ErrorKey = "MobileActivationKey", Response = null };

        string password = entity.Password;
        string password_with_key = password + AppSettings.Instance.UserPasswordKey;
        byte[] passwordHash, passwordSalt;
        Utility.CreatePasswordHash(password_with_key, out passwordHash, out passwordSalt);

        string query = @"   DECLARE @Error NVARCHAR(MAX)='';
                            DECLARE @UserId BIGINT = 0;
                            SELECT @UserId = ISNULL(Id, 0) FROM [User] 
                            WHERE EmailActivationKey='" + entity.EmailActivationKey + @"'
                                AND MobileActivationKey='" + entity.MobileActivationCode + @"'
                            IF @UserId <= 0
                                BEGIN SET @Error = N'Sadad_WrongActivationKey'; END
                            ELSE
                                BEGIN
                                    UPDATE [User] SET PasswordHash = @passwordHash, PasswordSalt = @passwordSalt
                                        , EmailActivationKey='', MobileActivationKey='' 
                                    WHERE Id = @UserId;
                                    --INSERT INTO [Event] ([Type], SendType
                                    --    , EntityType, EntityId
                                    --    , Sent, [Data], InActive, CreatedBy, CreateDate, UpdateDate)
                                    --VALUES ('" + EventType.UserForgetPasswordRecovery.ToString() + @"', '" + SendType.Email + @"'
                                    --    , N'" + EntityType.User + @"', @UserId
                                    --    , 0, '" + password + @"' , 0, 0, GETDATE(), GETDATE());
                                END
                            SELECT @Error;";
        //DB.Execute(query.ToString(), new { passwordHash = passwordHash, passwordSalt = passwordSalt });

        var error = DB.ExecuteScalar<string>(query, new { passwordHash = passwordHash, passwordSalt = passwordSalt });
        if (error != null && !string.IsNullOrEmpty(error))
            return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };

        return new ResponseDTO() { IsValid = true };
    }
    public IEnumerable<UserDTO.UserList> GetAllUser(
        long currentUserId,
        string currentUserType,
        string name,
        string mobile,
        string email,
        string userType,
        bool? inactive,
        string listOptions = null)
    {
        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT u.Id, u.UserType, u.[Name], u.Mobile, u.Email, u.InActive, u.CreateDate
                                , v.CompanyName, v.WorkEmail, v.WorkPhone, v.WebSiteUrl
                            FROM [User] u 
                                LEFT JOIN Vendor v ON v.UserId = u.Id
                            WHERE u.Id > 0 AND ISNULL(u.DeletedBy, 0) = 0 ");
        if (currentUserType != UserType.SuperAdmin.ToString() && currentUserType != UserType.Admin.ToString())
            query.AppendLine(@" AND u.ParentId = " + currentUserId);

        if (!string.IsNullOrEmpty(userType)) query.AppendLine(@" AND u.[UserType] = '" + userType + "'");
        if (inactive.HasValue) query.AppendLine(@"  AND u.InActive = '" + inactive.Value + "'");
        if (!string.IsNullOrEmpty(name)) query.AppendLine(@"  AND u.[Name] LIKE '%" + Utility.Wrap(name) + "%' ");
        if (!string.IsNullOrEmpty(mobile)) query.AppendLine(@"  AND u.Mobile LIKE '%" + Utility.Wrap(mobile) + "%' ");
        if (!string.IsNullOrEmpty(email)) query.AppendLine(@"  AND u.Email LIKE '%" + Utility.Wrap(email) + "%'");

        List<UserDTO.UserList> result = null;
        if (!string.IsNullOrEmpty(listOptions))
        {
            string filterQuery = GetFilterQuery(query, listOptions);
            result = DB.Query<UserDTO.UserList>(filterQuery).ToList();
        }
        else
        {
            query.AppendLine("  ORDER BY u.Id DESC ");
            result = DB.Query<UserDTO.UserList>(query.ToString()).ToList();
        }
        return result;
    }
    public ResponseDTO GetById(string currentUserType, long id)
    {
        string query = @"   SELECT u.Id, u.UserType, u.[Name], u.Email, u.Mobile, u.AccountType
                                , v.CompanyName, v.WorkEmail, v.WorkPhone, v.WebSiteUrl
                                , v.InvoiceValidDays, v.InvoiceLang, v.TermsCondition 
                                , v.AccountHolderName, v.AccountNumber, v.IBAN
                                , v.SalesReferralCode
                                , v.SocialLinksJson, v.SMSId, v.SMSSender
                                , b.Id BankId, b.NameAr AS BankNameAr, b.NameEn AS BankNameEn
                                , u.InActive
                            FROM [User] u
                                LEFT JOIN Vendor v ON v.UserId = u.Id
                                LEFT JOIN [Bank] b ON v.BankId = b.Id
                            WHERE ISNULL(u.DeletedBy, 0) = 0 AND u.Id = " + id;
        var entity = DB.Query<UserDTO.UserGetById>(query).FirstOrDefault();
        if (entity != null)
        {
            if (entity.UserType == UserType.SuperAdmin.ToString() 
                && !(currentUserType == UserType.SystemAdmin.ToString() || currentUserType == UserType.SuperAdmin.ToString()))
                return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };
            else if (entity.UserType == UserType.Admin.ToString()
                && !(currentUserType == UserType.SystemAdmin.ToString() || currentUserType == UserType.SuperAdmin.ToString()))
                return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };
            else if (entity.UserType == UserType.Vendor.ToString() 
                && !(currentUserType == UserType.SystemAdmin.ToString() || currentUserType == UserType.SuperAdmin.ToString() || currentUserType == UserType.Admin.ToString()))
                return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };

            query = @"  SELECT intg.Id
                            , ISNULL(intg.NameEn, '') NameEn
	                        , ISNULL(intg.NameAr, ISNULL(intg.NameEn, '')) NameAr
                            , ISNULL(intg.Code, '') Code
                            , ISNULL(intg.PaymentMethodType, 'Normal') PaymentMethodType
	                        , ISNULL(upm.FeesType, '') FeesType
	                        , ISNULL(upm.FeesFixedAmount, 0) FeesFixedAmount
	                        , ISNULL(upm.FeesPercentAmount, 0) FeesPercentAmount
	                        , ISNULL(upm.PaidBy, 'Vendor') PaidBy
	                        , ISNULL(upm.InActive, 1) InActive
                        FROM Integration intg 
	                        LEFT JOIN UserPaymentMethod upm ON intg.Id = upm.PaymentMethodId
	                        AND ISNULL(upm.DeletedBy, 0) = 0 
	                        AND ISNULL(upm.InActive, 0) = 0 
                            AND ISNULL(intg.DeletedBy, 0) = 0 
	                        AND ISNULL(intg.InActive, 0) = 0 
	                        AND UserId = " + id + @"
                        WHERE ISNULL(intg.[Type], '') = 'PaymentMethod'";
            var paymentMethods = DB.Query<UserPaymentMethodDTO>(query).ToList();
            entity.PaymentMethods = paymentMethods;

            if (entity.UserType == UserType.Vendor.ToString())
            {
                // Get Child Users
                query = @"  SELECT Id, ParentId, UserType, [Name], Mobile, Email
                            FROM [User]
                            WHERE ISNULL(DeletedBy, 0) = 0 AND ParentId= " + id;
                var users = DB.Query<UserDTO.Light>(query).ToList();
                entity.Users = users;

                // Get Attachments
                query = @"  SELECT Id,'" + AppSettings.Instance.AttachmentUrl + "Vendor/" + @"'+ [Name] [Name], DisplayName, Notes, EntityFieldName
	                        FROM Attachment
	                        WHERE DeletedBy IS NULL AND InActive = 0 AND EntityType = 'Vendor' AND EntityId = " + id;
                var attachments = DB.Query<AttachmentDTOLight>(query.ToString()).ToList();
                entity.Attachments = attachments;
            }

            if (string.IsNullOrEmpty(entity.SocialLinksJson))
            {
                entity.SocialLinksJson = "{}";
            }
        }
        return new ResponseDTO() { IsValid = true, ErrorKey = string.Empty, Response = entity };
    }
    public UserDTO.LightInfo GetLightInfo(long userId)
    {
        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT [Name], Email, Mobile, EmailActivationKey, MobileActivationKey  
                            FROM [User] WHERE Id = " + userId);
        return DB.Query<UserDTO.LightInfo>(query.ToString()).FirstOrDefault();
    }
    public ResponseDTO GetPaymentMethodByUserId(long userId)
    {
        string query = @"   SELECT intg.Id, ISNULL(intg.NameEn, '') NameEn
	                            , ISNULL(intg.NameAr, ISNULL(intg.NameEn, '')) NameAr
                                , ISNULL(intg.Code, '') Code
	                            , ISNULL(upm.FeesType, '') FeesType
	                            , ISNULL(upm.FeesFixedAmount, 0) FeesFixedAmount
	                            , ISNULL(upm.FeesPercentAmount, 0) FeesPercentAmount
	                            , ISNULL(upm.PaidBy, 'Vendor') PaidBy
	                            , ISNULL(upm.InActive, 1) InActive
                            FROM Integration intg 
	                            INNER JOIN UserPaymentMethod upm ON intg.Id = upm.PaymentMethodId
	                            AND ISNULL(upm.DeletedBy, 0) = 0 
	                            AND ISNULL(upm.InActive, 0) = 0 
                                AND ISNULL(intg.DeletedBy, 0) = 0 
	                            AND ISNULL(intg.InActive, 0) = 0 
	                            AND UserId = " + userId + @"
                            WHERE ISNULL(intg.[Type], '') = 'PaymentMethod'";

        var paymentMethods = DB.Query<UserPaymentMethodDTO>(query).ToList();
        return new ResponseDTO() { IsValid = true, ErrorKey = string.Empty, Response = paymentMethods };
    }
    public ResponseDTO Insert(long currentUserId, string currentUserType, UserDTO.UserInsert entity)
    {
        if (entity.UserType != UserType.SuperAdmin.ToString()
            && entity.UserType != UserType.Admin.ToString()
            && entity.UserType != UserType.Vendor.ToString()
            && entity.UserType != UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidUserType" };
        if (entity.UserType == UserType.SuperAdmin.ToString() 
            && !(currentUserType == UserType.SystemAdmin.ToString() || currentUserType == UserType.SuperAdmin.ToString()))
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess" };
        if (entity.UserType == UserType.Admin.ToString() && currentUserType != UserType.SuperAdmin.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess" };
        if (entity.UserType == UserType.Vendor.ToString()
            && !(currentUserType == UserType.SuperAdmin.ToString() || currentUserType == UserType.Admin.ToString()))
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess" };
        if (entity.UserType == UserType.User.ToString()
            && !(currentUserType == UserType.SuperAdmin.ToString() || currentUserType == UserType.Admin.ToString() || currentUserType == UserType.Vendor.ToString()))
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess" };
        if (string.IsNullOrEmpty(entity.Name))
            return new ResponseDTO() { IsValid = false, ErrorKey = "NameRequired" };
        if (!Regex.IsMatch(entity.Name, @"^[a-zA-Z \u0621-\u064A]+$"))
            return new ResponseDTO() { IsValid = false, ErrorKey = "NameAcceptOnlyLetters" };
        if (string.IsNullOrEmpty(entity.Email))
            return new ResponseDTO() { IsValid = false, ErrorKey = "EmailRequired" };
        var regex = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z";
        if (!Regex.IsMatch(entity.Email, regex, RegexOptions.IgnoreCase))
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidEmailAddress" };
        if (string.IsNullOrEmpty(entity.Password))
            return new ResponseDTO() { IsValid = false, ErrorKey = "PasswordRequired" };
        if (!string.IsNullOrEmpty(entity.Mobile)) entity.Mobile = Utility.toEnglishNumber(entity.Mobile);
        if (string.IsNullOrEmpty(entity.Mobile))
            return new ResponseDTO() { IsValid = false, ErrorKey = "MobileRequired" };
        if (entity.AccountHolderName != null && !Regex.IsMatch(entity.AccountHolderName, "^[a-zA-Z ]*$"))
            return new ResponseDTO() { IsValid = false, ErrorKey = "AccountHolderNameEnglishOnly" };
        if (!string.IsNullOrEmpty(entity.InvoiceLang) && entity.InvoiceLang.Length != 2)
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidLanguageCode" };
        if (string.IsNullOrEmpty(entity.AccountType) || (entity.AccountType != "HomeBusiness" && entity.AccountType != "Business"))
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidBusiness", Response = null };

        string[] attachmentFields = new string[10] {
            "logo","commerciallicense","articleassociation","signatureauthorization",
            "commercialregister","civilidowner", "civilidmanager","bankaccountletter","kycdoc","other"};
        if (entity.Attachments != null && entity.Attachments.Count > 0)
            foreach (var obj in entity.Attachments)
            {
                if (!string.IsNullOrEmpty(obj.Attachment))
                {
                    if (string.IsNullOrEmpty(attachmentFields.FirstOrDefault(x => x == obj.EntityFieldName.ToLower())))
                        return new ResponseDTO() { IsValid = false, ErrorKey = "WrongAttachmentField-" + obj.EntityFieldName, Response = null };
                    string fileError = Utility.ValidateFileUpload(obj.Attachment);
                    if (!string.IsNullOrEmpty(fileError))
                        return new ResponseDTO() { IsValid = false, ErrorKey = fileError, Response = null };
                }
            }

        byte[] passwordHash, passwordSalt;
        entity.Password = entity.Password + AppSettings.Instance.UserPasswordKey;
        Utility.CreatePasswordHash(entity.Password, out passwordHash, out passwordSalt);

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" 
                            DECLARE @Error NVARCHAR(MAX)='';
                            IF (SELECT COUNT(Id) FROM [User] WHERE Email = '" + Utility.Wrap(entity.Email) + @"' ) > 0 
                                BEGIN SET @Error = N'EmailAlreadyExists'; END 
                            IF (SELECT COUNT(Id) FROM [User] WHERE Mobile = '" + Utility.Wrap(entity.Mobile) + @"') > 0 
                                BEGIN SET @Error = N'MobileAlreadyExists'; END ");
        if (entity.BankId > 0)
            query.AppendLine(@" IF (SELECT Id FROM [Bank] WHERE Id = " + entity.BankId + @") <= 0 
                                    BEGIN SET @Error = N'BankNotExists'; END ");
        if (!string.IsNullOrEmpty(entity.SocialLinksJson))
        {
            var Social_Links_Json = JsonConvert.DeserializeObject<IDictionary<string, string>>(entity.SocialLinksJson);
            foreach (var lnk in Social_Links_Json)
            {
                if (!string.IsNullOrEmpty(lnk.Value))
                {
                    query.AppendLine(@" IF (SELECT COUNT(Id) FROM [Vendor] WHERE SocialLinksJson LIKE '%" + Utility.Wrap(lnk.Value) + @"%' ) > 0 
                                        BEGIN SET @Error = N'SocialLinksAlreadyExists'; END ");
                }
            }
        }
        if (!string.IsNullOrEmpty(entity.WebSiteUrl))
            query.AppendLine(@" IF (SELECT COUNT(Id) FROM [Vendor] WHERE WebSiteUrl LIKE '" + Utility.Wrap(entity.WebSiteUrl) + @"' ) > 0 
                                BEGIN SET @Error = N'WebsiteURLAlreadyExists'; END ");

        if (!string.IsNullOrEmpty(entity.CompanyName) && entity.UserType == UserType.Vendor.ToString())
            query.AppendLine(@" IF (SELECT COUNT(Id) FROM [Vendor] WHERE CompanyName LIKE N'" + Utility.Wrap(entity.CompanyName) + @"') > 0 
                                BEGIN SET @Error = N'CompanyNameAlreadyExists'; END ");

        var error = DB.ExecuteScalar<string>(query.ToString());
        if (error != null && !string.IsNullOrEmpty(error))
            return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };

        //, Logo,
        //                    , CommercialLicense, SignatureAuthorization, ArticleAssociation
        //                    , CommercialRegister, CivilIdOwner, CivilIdManager
        //                    , SalesReferralCode, BankAccountLetter, KYCdoc, Other, SocialLinksJson

        query.Clear();
        entity.InvoiceValidDays = (entity.InvoiceValidDays <= 0) ? 30 : entity.InvoiceValidDays;
        entity.InvoiceLang = (string.IsNullOrEmpty(entity.InvoiceLang) ? "Ar" : entity.InvoiceLang);
        query.AppendLine(@" DECLARE @CurrentDate DATETIME = (SELECT GETDATE())
                            DECLARE @CurrentUserId BIGINT = " + currentUserId + @";
                            DECLARE @ParentId BIGINT = 0;
                            DECLARE @NewRowId BIGINT = 0;
                            SELECT @ParentId = CASE
                                                WHEN UserType = '" + UserType.User + @"' THEN ParentId
                                                ELSE Id
                                               END
                            FROM [User] 
                            WHERE Id = @CurrentUserId;

                            BEGIN TRANSACTION [UserInsert]
                            BEGIN TRY
                            
                                INSERT INTO [User] (ParentId, UserType, [Name]
                                    , Mobile, Email, AccountType
                                    , PasswordHash, PasswordSalt, ContactActivated
                                    , InActive, CreatedBy, CreateDate, UpdatedBy, UpdateDate)
                                Values(@ParentId, N'" + entity.UserType + "', N'" + Utility.Wrap(entity.Name) + @"'
                                    , N'" + Utility.Wrap(entity.Mobile) + "', N'" + Utility.Wrap(entity.Email) + "', N'" + Utility.Wrap(entity.AccountType) + @"'
                                    , @passwordHash, @passwordSalt, 0
                                    , 0, @CurrentUserId, @CurrentDate, @CurrentUserId, @CurrentDate); 
                            
                                SELECT @NewRowId = SCOPE_IDENTITY();
                                EXECUTE dbo.GenerateEntityCode 'User', @NewRowId;

                            -- Insert user permission record
                                INSERT INTO UserPermission(UserId, ModuleId, FunctionId, FunctionPermission, FunctionAuditable
                                    , InActive, CreatedBy, CreateDate, UpdatedBy, UpdateDate)
                                SELECT @NewRowId, f.ModuleId, fu.FunctionId, f.Permission, f.Auditable
                                    , 0, 0, GETDATE(), 0, GETDATE()
                                FROM SysFunction f
	                                INNER JOIN SysFunctionUserType fu ON f.Id = fu.FunctionId
                                WHERE fu.UserType = '" + entity.UserType + @"';
                            ");
        if (entity.UserType == UserType.SuperAdmin.ToString() || entity.UserType == UserType.Admin.ToString())
        {
            // Activate the record
            query.AppendLine(@"
                                Update [User] SET ContactActivated = 1, Reviewed = 1 WHERE Id = @NewRowId;
                            ");
        }
        else if (entity.UserType == UserType.Vendor.ToString())
        {
            // Vendor Info
            query.AppendLine(@"
                                INSERT INTO Vendor (UserId, EmailId, SMSId, SMSSender, CompanyName
                                    , WorkEmail, WorkPhone
                                    , WebSiteUrl, SalesReferralCode
                                    , InvoiceValidDays, InvoiceLang, TermsCondition
                                    , BankId, AccountHolderName, AccountNumber, IBAN
                                    , InActive, CreatedBy, CreateDate, UpdatedBy, UpdateDate)
                                    
                                VALUES(@NewRowId, " + entity.EmailId + @", " + entity.SMSId + @", '" + Utility.Wrap(entity.SMSSender) + @"', '" + Utility.Wrap(entity.CompanyName) + @"'
                                    , '" + Utility.Wrap(entity.WorkEmail) + @"', '" + Utility.Wrap(entity.WorkPhone) + @"'
                                    , '" + Utility.Wrap(entity.WebSiteUrl) + @"', '" + Utility.Wrap(entity.SalesReferralCode) + @"'
                                    , " + entity.InvoiceValidDays + @", '" + Utility.Wrap(entity.InvoiceLang) + @"', '" + Utility.Wrap(entity.TermsCondition) + @"'
                                    , " + entity.BankId + @", N'" + Utility.Wrap(entity.AccountHolderName) + "', N'" + Utility.Wrap(entity.AccountNumber) + "', N'" + Utility.Wrap(entity.IBAN) + @"'
                                    , 0, @CurrentUserId, @CurrentDate, @CurrentUserId, @CurrentDate); 
                            ");
            // Payment methods
            if (entity.PaymentMethods != null && entity.PaymentMethods.Count > 0)
                entity.PaymentMethods.ForEach(p => query.AppendLine(@"  INSERT INTO UserPaymentMethod (UserId, PaymentMethodId, FeesType
                                                                            , FeesFixedAmount, FeesPercentAmount, PaidBy
                                                                            , InActive, CreatedBy, CreateDate, UpdatedBy, UpdateDate) 
                                                                        VALUES (@NewRowId, " + p.Id + ", N'" + p.FeesType + @"'
                                                                            , " + p.FeesFixedAmount + @", " + p.FeesPercentAmount + @", N'" + p.PaidBy + @"'
                                                                            , 0, @CurrentUserId, @CurrentDate, @CurrentUserId, @CurrentDate);"));
            // Attachments
            if (entity.Attachments != null && entity.Attachments.Count > 0)
            {
                query.AppendLine("IF @NewRowId > 0 ");
                query.AppendLine("  BEGIN");
                foreach (var obj in entity.Attachments)
                {
                    if (!string.IsNullOrEmpty(obj.Attachment))
                    {
                        query.AppendLine(@" INSERT INTO Attachment (EntityType, EntityId, EntityFieldName
                                                , DisplayName, Notes
                                                , InActive, CreatedBy, CreateDate, UpdateDate)  
                                            VALUES ('Vendor', @NewRowId, N'" + Utility.Wrap(obj.EntityFieldName) + @"'
                                                , N'" + Utility.Wrap(obj.DisplayName) + @"', N'" + Utility.Wrap(obj.Notes) + @"'
                                                , 0, @CurrentUserId, @CurrentDate, @CurrentDate);");
                    }
                }
                query.AppendLine("  END");
            }
        }

        query.AppendLine(@" COMMIT TRANSACTION [UserInsert]
                            SELECT @NewRowId
                            END TRY
                                BEGIN CATCH
                                ROLLBACK TRANSACTION [UserInsert]
                            END CATCH");
        object userId = DB.ExecuteScalar(query.ToString(), new { passwordHash = passwordHash, passwordSalt = passwordSalt });

        string attachment_sql = "";
        if (entity.Attachments != null && entity.Attachments.Count > 0)
        {
            foreach (var obj in entity.Attachments)
            {
                if (!string.IsNullOrEmpty(obj.Attachment))
                {
                    string newFileName = UploadFile(userId, "Vendor", obj);
                    attachment_sql += " UPDATE Attachment SET [Name] = '" + newFileName + @"' 
                                            WHERE EntityType = 'Vendor' 
                                                AND [EntityId] = " + userId + @" 
                                                AND ISNULL(EntityFieldName, '') = '" + obj.EntityFieldName + @"'
                                                AND DisplayName = '" + obj.DisplayName + @"'
                                                AND InActive = 0
                                                AND CreatedBy =" + currentUserId + @"
                            ";
                }
            }
        }
        if (!string.IsNullOrEmpty(attachment_sql))
            DB.Execute(attachment_sql);

        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = userId };
    }
    public ResponseDTO Update(long currentUserId, string currentUserType, UserDTO.UserUpdate entity)
    {
        if (entity.Id <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "UserIdRequired" };
        if (!string.IsNullOrEmpty(entity.UserType)
            && entity.UserType != UserType.SuperAdmin.ToString()
            && entity.UserType != UserType.Admin.ToString()
            && entity.UserType != UserType.Vendor.ToString()
            && entity.UserType != UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidUserType" };

        if (entity.UserType == UserType.SuperAdmin.ToString()
             && !(currentUserType == UserType.SystemAdmin.ToString() || currentUserType == UserType.SuperAdmin.ToString()))
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess" };
        if (entity.UserType == UserType.Admin.ToString() && currentUserType != UserType.SuperAdmin.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess" };
        if (entity.UserType == UserType.Vendor.ToString()
            && !(currentUserType == UserType.SuperAdmin.ToString() || currentUserType == UserType.Admin.ToString()))
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess" };
        if (entity.UserType == UserType.User.ToString()
            && !(currentUserType == UserType.SuperAdmin.ToString() || currentUserType == UserType.Admin.ToString() || currentUserType == UserType.Vendor.ToString()))
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess" };

        if (string.IsNullOrEmpty(entity.Name))
            return new ResponseDTO() { IsValid = false, ErrorKey = "NameRequired" };
        if (!Regex.IsMatch(entity.Name, @"^[a-zA-Z \u0621-\u064A]+$"))
            return new ResponseDTO() { IsValid = false, ErrorKey = "NameAcceptOnlyLetters" };
        if (string.IsNullOrEmpty(entity.Email))
            return new ResponseDTO() { IsValid = false, ErrorKey = "EmailRequired" };
        var regex = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z";
        if (!Regex.IsMatch(entity.Email, regex, RegexOptions.IgnoreCase))
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidEmailAddress" };
        if (string.IsNullOrEmpty(entity.Password))
            return new ResponseDTO() { IsValid = false, ErrorKey = "PasswordRequired" };

        if (string.IsNullOrEmpty(entity.Mobile))
            return new ResponseDTO() { IsValid = false, ErrorKey = "MobileRequired" };
        if (entity.AccountHolderName != null && !Regex.IsMatch(entity.AccountHolderName, "^[a-zA-Z ]*$"))
            return new ResponseDTO() { IsValid = false, ErrorKey = "AccountHolderNameEnglishOnly" };
        if (!string.IsNullOrEmpty(entity.InvoiceLang) && entity.InvoiceLang.Length != 2)
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidLanguageCode" };
        if (string.IsNullOrEmpty(entity.AccountType) || (entity.AccountType != "HomeBusiness" && entity.AccountType != "Business"))
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidBusiness", Response = null };

        if (entity.PaymentMethods != null && entity.PaymentMethods.Count > 0
            && (currentUserType == UserType.SuperAdmin.ToString() || currentUserType == UserType.Admin.ToString())
            && entity.UserType == UserType.Vendor.ToString())
        {
            if (entity.PaymentMethods.Any(x => x.Id <= 0))
                return new ResponseDTO() { IsValid = false, ErrorKey = "PaymentMethodIdRequired", Response = null };
            else if (entity.PaymentMethods.Any(x => x.FeesType != FeesType.Amount.ToString() && x.FeesType != FeesType.Percent.ToString() && x.FeesType != FeesType.AmountAndPercent.ToString()))
                return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidFeesType", Response = null };
            else if (entity.PaymentMethods.Any(x => x.FeesType == FeesType.Amount.ToString() && x.FeesFixedAmount <= 0))
                return new ResponseDTO() { IsValid = false, ErrorKey = "FeesFixedAmountRequired", Response = null };
            else if (entity.PaymentMethods.Any(x => x.FeesType == FeesType.Percent.ToString() && x.FeesPercentAmount <= 0))
                return new ResponseDTO() { IsValid = false, ErrorKey = "FeesPercentAmountRequired", Response = null };
            else if (entity.PaymentMethods.Any(x => x.FeesType == FeesType.AmountAndPercent.ToString() && x.FeesFixedAmount <= 0 && x.FeesPercentAmount <= 0))
                return new ResponseDTO() { IsValid = false, ErrorKey = "FeesFixedAndPercentAmountRequired", Response = null };
            else if (entity.PaymentMethods.Any(x => x.PaidBy != PaymentMethodsPaidBy.Vendor.ToString() && x.PaidBy != PaymentMethodsPaidBy.Customer.ToString() && x.PaidBy != PaymentMethodsPaidBy.Split.ToString()))
                return new ResponseDTO() { IsValid = false, ErrorKey = "MethodNotAllowed", Response = null };
        }
        if (!string.IsNullOrEmpty(entity.Mobile)) entity.Mobile = Utility.toEnglishNumber(entity.Mobile);

        int reviewed = Convert.ToInt32(DB.ExecuteScalar("SELECT Reviewed FROM [User] WHERE Id=" + entity.Id));
        if (reviewed == 0 || currentUserType == UserType.SuperAdmin.ToString() || currentUserType == UserType.Admin.ToString())
        {
            string[] attachmentFields = new string[10] {
            "logo","commerciallicense","articleassociation","signatureauthorization",
            "commercialregister","civilidowner", "civilidmanager","bankaccountletter","kycdoc","other"};
            if (entity.Attachments != null && entity.Attachments.Count > 0)
                foreach (var obj in entity.Attachments)
                {
                    if (obj.Id <= 0 && !string.IsNullOrEmpty(obj.Attachment))
                    {
                        if (string.IsNullOrEmpty(attachmentFields.FirstOrDefault(x => x == obj.EntityFieldName.ToLower())))
                            return new ResponseDTO() { IsValid = false, ErrorKey = "WrongAttachmentField-" + obj.EntityFieldName, Response = null };
                        string fileError = Utility.ValidateFileUpload(obj.Attachment);
                        if (!string.IsNullOrEmpty(fileError))
                            return new ResponseDTO() { IsValid = false, ErrorKey = fileError, Response = null };
                    }
                }
        }

        byte[] passwordHash = default, passwordSalt = default;
        if (!string.IsNullOrEmpty(entity.Password))
        {
            entity.Password = entity.Password + AppSettings.Instance.UserPasswordKey;
            Utility.CreatePasswordHash(entity.Password, out passwordHash, out passwordSalt);
        }

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" 
                            DECLARE @Error NVARCHAR(MAX)='';
                            IF (SELECT COUNT(Id) FROM [User] WHERE Id = " + entity.Id + @" )  <= 0
                                BEGIN SET @Error = N'UserNotExists'; END 
                            ELSE IF (SELECT COUNT(Id) FROM [User] WHERE Email = '" + Utility.Wrap(entity.Email) + @"' AND Id <> " + entity.Id + @" ) > 0 
                                BEGIN SET @Error = N'EmailAlreadyExists'; END 
                            ELSE IF (SELECT COUNT(Id) FROM [User] WHERE Mobile = '" + Utility.Wrap(entity.Mobile) + @"' AND Id <> " + entity.Id + @") > 0 
                                BEGIN SET @Error = N'MobileAlreadyExists'; END ");
        if (entity.BankId > 0)
            query.AppendLine(@" ELSE IF (SELECT Id FROM [Bank] WHERE Id = " + entity.BankId + @") <= 0 
                                    BEGIN SET @Error = N'BankNotExists'; END ");
        if (!string.IsNullOrEmpty(entity.SocialLinksJson))
        {
            var Social_Links_Json = JsonConvert.DeserializeObject<IDictionary<string, string>>(entity.SocialLinksJson);
            foreach (var lnk in Social_Links_Json)
            {
                if (!string.IsNullOrEmpty(lnk.Value))
                {
                    query.AppendLine(@" ELSE IF (SELECT COUNT(Id) FROM [Vendor] WHERE SocialLinksJson LIKE '%" + Utility.Wrap(lnk.Value) + @"%'  AND UserId <> " + entity.Id + @" ) > 0 
                                        BEGIN SET @Error = N'SocialLinksAlreadyExists'; END ");
                }
            }
        }
        if (!string.IsNullOrEmpty(entity.WebSiteUrl))
            query.AppendLine(@" ELSE IF (SELECT COUNT(Id) FROM [Vendor] WHERE WebSiteUrl LIKE '" + Utility.Wrap(entity.WebSiteUrl) + @"'  AND UserId <> " + entity.Id + @" ) > 0 
                                BEGIN SET @Error = N'WebsiteURLAlreadyExists'; END ");

        if (!string.IsNullOrEmpty(entity.CompanyName) && entity.UserType == UserType.Vendor.ToString())
            query.AppendLine(@" ELSE IF (SELECT COUNT(Id) FROM [Vendor] WHERE CompanyName LIKE N'" + Utility.Wrap(entity.CompanyName) + @"'  AND UserId <> " + entity.Id + @" ) > 0 
                                BEGIN SET @Error = N'CompanyNameAlreadyExists'; END ");

        query.AppendLine("SELECT @Error");
        var error = DB.ExecuteScalar<string>(query.ToString());
        if (error != null && !string.IsNullOrEmpty(error))
            return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };

        // Start Updating User
        query.Clear();
        query.AppendLine(@" DECLARE @CurrentDate DATETIME = (SELECT GETDATE())
                            DECLARE @CurrentUserId BIGINT = " + currentUserId + @";
                            BEGIN TRANSACTION [UserUpdate]
                            BEGIN TRY
                                DECLARE @RowId BIGINT = " + entity.Id + @";
                                UPDATE [User] SET UpdateDate = @CurrentDate ");
        if (entity.UserType != UserType.Vendor.ToString())
        {
            if (!string.IsNullOrEmpty(entity.Name)) query.AppendLine(" , Name = N'" + Utility.Wrap(entity.Name) + "'");
            if (!string.IsNullOrEmpty(entity.Email)) query.AppendLine(" , Email = N'" + Utility.Wrap(entity.Email) + "'");
        }
        if (!string.IsNullOrEmpty(entity.Mobile)) query.AppendLine(" , Mobile = N'" + Utility.Wrap(entity.Mobile) + "'");
        if (!string.IsNullOrEmpty(entity.AccountType)) query.AppendLine(" , AccountType = '" + Utility.Wrap(entity.AccountType) + "'");
        if (
                currentUserType == UserType.SuperAdmin.ToString()
                || currentUserType == UserType.Admin.ToString()
                || (entity.UserType == UserType.User.ToString() && currentUserType == UserType.Vendor.ToString())
            )
        {
            query.AppendLine(" , InActive = " + (entity.InActive ? 1 : 0) + "");
            query.AppendLine(" , Reviewed = " + (entity.Reviewed ? 1 : 0) + "");
        }
        if (!string.IsNullOrEmpty(entity.Password))
            query.AppendLine(", PasswordHash = @passwordHash, PasswordSalt = @passwordSalt ");
        query.AppendLine("WHERE Id = @RowId");

        if (entity.UserType == UserType.Vendor.ToString() || entity.UserType == UserType.User.ToString())
        {
            query.AppendLine(@" UPDATE [Vendor] SET UpdateDate = @CurrentDate ");
            if (!string.IsNullOrEmpty(entity.WorkEmail)) query.AppendLine(" , WorkEmail = N'" + Utility.Wrap(entity.WorkEmail) + "'");
            if (entity.UserType != UserType.Vendor.ToString() && !string.IsNullOrEmpty(entity.CompanyName)) query.AppendLine(" , CompanyName = N'" + Utility.Wrap(entity.CompanyName) + "'");
            if (!string.IsNullOrEmpty(entity.WorkPhone)) query.AppendLine(" , WorkPhone = N'" + Utility.Wrap(entity.WorkPhone) + "'");
            if (entity.WebSiteUrl != null) query.AppendLine(" , WebSiteUrl = N'" + Utility.Wrap(entity.WebSiteUrl) + "'");
            if (entity.InvoiceValidDays > 0) query.AppendLine(" , InvoiceValidDays = " + entity.InvoiceValidDays + "");
            if (!string.IsNullOrEmpty(entity.InvoiceLang)) query.AppendLine(" , InvoiceLang = N'" + Utility.Wrap(entity.InvoiceLang) + "'");
            if (!string.IsNullOrEmpty(entity.TermsCondition)) query.AppendLine(" , TermsCondition = N'" + Utility.Wrap(entity.TermsCondition) + "'");
            if (entity.BankId > 0) query.AppendLine(" , BankId = " + entity.BankId);
            if (!string.IsNullOrEmpty(entity.AccountHolderName)) query.AppendLine(" , AccountHolderName = N'" + Utility.Wrap(entity.AccountHolderName) + "'");
            if (!string.IsNullOrEmpty(entity.AccountNumber)) query.AppendLine(" , AccountNumber = N'" + Utility.Wrap(entity.AccountNumber) + "'");
            if (!string.IsNullOrEmpty(entity.IBAN)) query.AppendLine(" , IBAN = N'" + Utility.Wrap(entity.IBAN) + "'");
            if (!string.IsNullOrEmpty(entity.SalesReferralCode)) query.AppendLine(" , SalesReferralCode = '" + Utility.Wrap(entity.SalesReferralCode) + "'");
            if (!string.IsNullOrEmpty(entity.SocialLinksJson)) query.AppendLine(" , SocialLinksJson = N'" + Utility.Wrap(entity.SocialLinksJson) + "'");

            if (currentUserType == UserType.SuperAdmin.ToString() || currentUserType == UserType.Admin.ToString())
            {
                if (entity.SMSId > 0) query.AppendLine(" , SMSId = " + entity.SMSId + "");
                if (entity.SMSSender != null) query.AppendLine(" , SMSSender = N'" + Utility.Wrap(entity.SMSSender) + "'");
                if (entity.EmailId > 0) query.AppendLine(" , EmailId = " + entity.EmailId + "");
            }
            query.AppendLine("WHERE UserId = @RowId");
        }
        if (entity.PaymentMethods != null && entity.PaymentMethods.Count > 0)
        {
            if (currentUserType != UserType.Vendor.ToString() && currentUserType != UserType.User.ToString())
                query.AppendLine("UPDATE UserPaymentMethod SET DeletedBy = @CurrentUserId, DeleteDate=@CurrentDate, InActive = 1 WHERE UserId = @RowId; ");
            foreach (var paymentMethod in entity.PaymentMethods)
            {
                query.AppendLine("IF EXISTS (SELECT Id FROM UserPaymentMethod WHERE UserId = @RowId AND PaymentMethodId = " + paymentMethod.Id + ")");
                query.AppendLine("  BEGIN");
                query.AppendLine(@"     UPDATE UserPaymentMethod SET ");
                if (currentUserType != UserType.Vendor.ToString() && currentUserType != UserType.User.ToString())
                {
                    query.AppendLine("  FeesType = N'" + paymentMethod.FeesType + "', FeesFixedAmount = " + paymentMethod.FeesFixedAmount + @"
                                            , FeesPercentAmount=" + paymentMethod.FeesPercentAmount + @",
                                             ");
                }
                query.AppendLine(@"     DeletedBy = NULL, DeleteDate=NULL, InActive = 0, UpdateDate = @CurrentDate, UpdatedBy= @CurrentUserId
                                        , PaidBy=N'" + paymentMethod.PaidBy + @"'
                                        WHERE UserId = @RowId AND PaymentMethodId = " + paymentMethod.Id + @"
                                    END");
                if (currentUserType != UserType.Vendor.ToString() && currentUserType != UserType.User.ToString())
                {
                    query.AppendLine("ELSE");
                    query.AppendLine("  BEGIN");
                    query.AppendLine(@"     INSERT INTO UserPaymentMethod (UserId, PaymentMethodId, FeesType
                                                , FeesFixedAmount, FeesPercentAmount, PaidBy
                                                , InActive, CreatedBy, CreateDate, UpdateDate) 
                                            VALUES (@RowId, " + paymentMethod.Id + ", N'" + paymentMethod.FeesType + @"'
                                                , " + paymentMethod.FeesFixedAmount + @", " + paymentMethod.FeesPercentAmount + @", N'" + paymentMethod.PaidBy + @"'
                                                , 0, @CurrentUserId, @CurrentDate, @CurrentDate);");
                    query.AppendLine("  END");
                }
            }
        }
        if (reviewed == 0 || currentUserType == UserType.SuperAdmin.ToString() || currentUserType == UserType.Admin.ToString())
            if (entity.Attachments != null && entity.Attachments.Count > 0)
            {
                foreach (var obj in entity.Attachments)
                {
                    if (obj.Id <= 0 && !string.IsNullOrEmpty(obj.Attachment))
                    {
                        query.AppendLine(@" INSERT INTO Attachment (EntityType, EntityId, EntityFieldName
                                            , DisplayName, Notes
                                            , InActive, CreatedBy, CreateDate, UpdateDate)  
                                        VALUES ('Vendor', @RowId, N'" + Utility.Wrap(obj.EntityFieldName) + @"'
                                            , N'" + Utility.Wrap(obj.DisplayName) + @"', N'" + Utility.Wrap(obj.Notes) + @"'
                                            , 0, @CurrentUserId, @CurrentDate, @CurrentDate);");
                    }
                }
            }

        query.AppendLine(@" COMMIT TRANSACTION [UserUpdate]
                            SELECT @RowId;
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [UserUpdate]
                                SELECT ERROR_MESSAGE()
                                --SELECT  ERROR_NUMBER() AS ErrorNumber, ERROR_SEVERITY() AS ErrorSeverity ,ERROR_STATE() AS ErrorState, ERROR_PROCEDURE() AS ErrorProcedure, ERROR_LINE() AS ErrorLine, ERROR_MESSAGE() AS ErrorMessage;  
                            END CATCH");
        object result = null;
        if (!string.IsNullOrEmpty(entity.Password))
            result = DB.ExecuteScalar(query.ToString(), new { passwordHash = passwordHash, passwordSalt = passwordSalt });
        else
            result = DB.ExecuteScalar(query.ToString());

        try
        {
            result = Convert.ToInt64(result);
            string attachment_sql = "";
            if (reviewed == 0 || currentUserType == UserType.SuperAdmin.ToString() || currentUserType == UserType.Admin.ToString())
            {
                if (entity.Attachments != null && entity.Attachments.Count > 0)
                {
                    foreach (var obj in entity.Attachments)
                    {
                        if (obj.Id <= 0 && !string.IsNullOrEmpty(obj.Attachment))
                        {
                            if (!string.IsNullOrEmpty(obj.Attachment))
                            {
                                string newFileName = UploadFile(result, "Vendor", obj);
                                attachment_sql += " UPDATE Attachment SET [Name] = '" + newFileName + @"' 
                                                WHERE EntityType = 'Vendor' 
                                                    AND [EntityId] = " + result + @" 
                                                    AND ISNULL(EntityFieldName, '') = '" + obj.EntityFieldName + @"'
                                                    AND DisplayName = '" + obj.DisplayName + @"'
                                                    AND InActive = 0
                                                    AND CreatedBy =" + currentUserId + @"
                            ";
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(attachment_sql))
                    DB.Execute(attachment_sql);
            }
            return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
        }
        catch
        {
            return new ResponseDTO() { IsValid = false, ErrorKey = result.ToString(), Response = null };
        }
    }
    public ResponseDTO Delete(long currentUserId, string currentUserType, long id)
    {
        string query = @"   
                            DECLARE @Error NVARCHAR(MAX)='';
                            DECLARE @UserType NVARCHAR(50);
                            
                            SELECT @UserType = UserType 
                            FROM [User] WHERE Id = " + id + @"

                            IF @UserType = '" + UserType.Vendor.ToString() + @"'
                                BEGIN SET @Error = N'MethodNotAllowed'; END
                            
                            ELSE IF (@UserType = '" + UserType.SuperAdmin.ToString() + @"' OR @UserType = '" + UserType.Admin.ToString() + @"')
                                    AND '" + currentUserType + "' <> '" + UserType.SuperAdmin.ToString() + @"'
                                BEGIN SET @Error = 'UnauthorizedAccess'; END
                            
                            ELSE
                            BEGIN
                                Update [User] SET DeletedBy = " + currentUserId + @", DeleteDate = GETDATE()
                                WHERE Id = " + id + @"
                            END
                            SELECT @Error;";

        var error = DB.ExecuteScalar<string>(query);
        if (error != null && !string.IsNullOrEmpty(error))
            return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };

        return new ResponseDTO() { IsValid = true };
    }
    //public ResponseDTO GetUserPermission_oLD(long currentUserId, string currentUserType, long userId)
    //{
    //    string query = "";
    //    if (userId > 0)
    //    {
    //        query = @"   
    //                        DECLARE @Error NVARCHAR(MAX)='';
    //                        DECLARE @UserType NVARCHAR(50);
    //                        DECLARE @RegisteredUserId BIGINT = 0;


    //                        SELECT @UserType = UserType
    //                        FROM [User] 
    //                        WHERE Id = " + userId + @"

    //                        IF '" + currentUserType + "' <> '" + UserType.SystemAdmin.ToString() + @"'
    //                        BEGIN

    //                            SELECT @RegisteredUserId = ISNULL(Id, 0) 
    //                            FROM [User] 
    //                            WHERE Id = " + userId + @" AND (Id = " + currentUserId + @" OR ParentId = " + currentUserId + @")

    //                            IF @RegisteredUserId <= 0
    //                                BEGIN SET @Error = 'UnauthorizedAccess'; END

    //                            IF @UserType = '" + UserType.SuperAdmin.ToString() + @"' AND '" + currentUserType + "' <> '" + UserType.SuperAdmin.ToString() + @"'
    //                                BEGIN SET @Error = 'UnauthorizedAccess'; END
    //                            ELSE IF @UserType = '" + UserType.Admin.ToString() + @"' AND '" + currentUserType + "' <> '" + UserType.SuperAdmin.ToString() + @"'
    //                                BEGIN SET @Error = 'UnauthorizedAccess'; END
    //                            ELSE IF @UserType = '" + UserType.Vendor.ToString() + @"'
    //                                    AND NOT 
    //                                        (
    //                                        '" + currentUserType + "' = '" + UserType.SuperAdmin.ToString() + @"'
    //                                        OR
    //                                        '" + currentUserType + "' = '" + UserType.Admin.ToString() + @"'
    //                                        OR
    //                                        '" + currentUserType + "' = '" + UserType.Vendor.ToString() + @"'
    //                                        )
    //                                BEGIN SET @Error = 'UnauthorizedAccess'; END
    //                            ELSE IF @UserType = '" + UserType.User.ToString() + @"'
    //                                    AND NOT 
    //                                        (
    //                                        '" + currentUserType + "' = '" + UserType.SuperAdmin.ToString() + @"'
    //                                        OR
    //                                        '" + currentUserType + "' = '" + UserType.Admin.ToString() + @"'
    //                                        OR
    //                                        '" + currentUserType + "' = '" + UserType.Vendor.ToString() + @"'
    //                                        )
    //                                BEGIN SET @Error = 'UnauthorizedAccess'; END


    //                        END
    //                        SELECT @Error;

    //                    ";
    //        var error = DB.ExecuteScalar<string>(query);
    //        if (error != null && !string.IsNullOrEmpty(error))
    //            return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };
    //    }
    //    List<UserModuleDTO> list = new List<UserModuleDTO>();
    //    query = @"   SELECT u.UserId UserId
    //                         , u.ModuleId, u.ModuleName, m.DisplayName ModuleDisplayName
    //                         , u.ModulePermission, u.ModuleAuditable
    //                         , u.FunctionId, u.FunctionName, f.DisplayName FunctionDisplayName
    //                         , u.FunctionPermission, u.FunctionAuditable
    //                         , u.InActive FunctionInActive
    //                        INTO #LoggedUserPermission
    //                        FROM SysModule m 
    //                         INNER JOIN SysFunction f ON f.ModuleId = m.Id 
    //                         INNER JOIN UserPermission up ON up.ModuleId = m.Id AND up.UserId = " + currentUserId + @"
    //                          AND ISNULL(up.DeletedBy, 0) = 0 
    //                          AND up.InActive = 0 
    //                         INNER JOIN UserPermissionDetail u ON u.UserPermissionId = up.Id
    //                          AND u.FunctionId = f.Id
    //                          AND ISNULL(u.DeletedBy, 0) = 0
    //                          AND u.InActive = 0
    //                        --SELECT * FROM #LoggedUserPermission

    //                        SELECT ISNULL(u.UserId, 0) UserId
    //                         , lp.ModuleId, lp.ModuleName, lp.ModuleDisplayName
    //                         , lp.ModulePermission, lp.ModuleAuditable
    //                         , lp.FunctionId, lp.FunctionName, lp.FunctionDisplayName
    //                         , lp.FunctionPermission, lp.FunctionAuditable
    //                         , lp.FunctionInActive
    //                        FROM #LoggedUserPermission lp
    //                         LEFT JOIN UserPermissionDetail u ON u.ModuleId = lp.ModuleId
    //                          AND u.FunctionId = lp.FunctionId
    //                          AND ISNULL(u.DeletedBy, 0) = 0
    //                          AND u.InActive = 0
    //                          AND u.UserId = " + userId + @"

    //                        DROP TABLE #LoggedUserPermission
    //                                                     ";

    //    var result = DB.Query<UserPermissionDTO>(query).ToList();
    //    if (result != null && result.Count > 0)
    //    {
    //        foreach (var m in result)
    //        {
    //            var currentModule = list.Where(x => x.Module == m.ModuleName).FirstOrDefault();
    //            if (currentModule == null)
    //            {
    //                currentModule = new UserModuleDTO()
    //                {
    //                    UserId = m.UserId,
    //                    ModuleId = m.ModuleId,
    //                    Module = m.ModuleName,
    //                    Permission = m.ModulePermission,
    //                    Auditable = m.ModuleAuditable,
    //                    InActive = m.ModuleInActive,
    //                    Functions = new List<UserFunctionDTO>()
    //                };
    //                list.Add(currentModule);
    //            }
    //            UserFunctionDTO function = new UserFunctionDTO()
    //            {
    //                UserId = m.UserId,
    //                FunctionId = m.FunctionId,
    //                Function = m.FunctionName,
    //                Permission = m.FunctionPermission,
    //                Auditable = m.FunctionAuditable,
    //                InActive = m.FunctionInActive,
    //            };
    //            currentModule.Functions.Add(function);
    //        }
    //    }
    //    return new ResponseDTO() { IsValid = true, ErrorKey = string.Empty, Response = list }; ;
    //}
    public ResponseDTO GetUserPermission(long currentUserId, string currentUserType, long userId)
    {
        string query = "";
        if (userId > 0)
        {
            query = @"   
                            DECLARE @Error NVARCHAR(MAX)='';
                            DECLARE @UserType NVARCHAR(50);
                            DECLARE @RegisteredUserId BIGINT = 0;

                           
                            SELECT @UserType = UserType
                            FROM [User] 
                            WHERE Id = " + userId + @"
                        
                            IF '" + currentUserType + "' <> '" + UserType.SystemAdmin.ToString() + @"'
                            BEGIN
                                    
                                SELECT @RegisteredUserId = ISNULL(Id, 0) 
                                FROM [User] 
                                WHERE Id = " + userId + @" AND (Id = " + currentUserId + @" OR ParentId = " + currentUserId + @")
                            
                                IF @RegisteredUserId <= 0
                                    BEGIN SET @Error = 'UnauthorizedAccess'; END

                                IF @UserType = '" + UserType.SuperAdmin.ToString() + @"' AND '" + currentUserType + "' <> '" + UserType.SuperAdmin.ToString() + @"'
                                    BEGIN SET @Error = 'UnauthorizedAccess'; END
                                ELSE IF @UserType = '" + UserType.Admin.ToString() + @"' AND '" + currentUserType + "' <> '" + UserType.SuperAdmin.ToString() + @"'
                                    BEGIN SET @Error = 'UnauthorizedAccess'; END
                                ELSE IF @UserType = '" + UserType.Vendor.ToString() + @"'
                                        AND NOT 
                                            (
                                            '" + currentUserType + "' = '" + UserType.SuperAdmin.ToString() + @"'
                                            OR
                                            '" + currentUserType + "' = '" + UserType.Admin.ToString() + @"'
                                            OR
                                            '" + currentUserType + "' = '" + UserType.Vendor.ToString() + @"'
                                            )
                                    BEGIN SET @Error = 'UnauthorizedAccess'; END
                                ELSE IF @UserType = '" + UserType.User.ToString() + @"'
                                        AND NOT 
                                            (
                                            '" + currentUserType + "' = '" + UserType.SuperAdmin.ToString() + @"'
                                            OR
                                            '" + currentUserType + "' = '" + UserType.Admin.ToString() + @"'
                                            OR
                                            '" + currentUserType + "' = '" + UserType.Vendor.ToString() + @"'
                                            )
                                    BEGIN SET @Error = 'UnauthorizedAccess'; END

                                    
                            END
                            SELECT @Error;
                           
                        ";
            var error = DB.ExecuteScalar<string>(query);
            if (error != null && !string.IsNullOrEmpty(error))
                return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };
        }
        List<UserModuleDTO> list = new List<UserModuleDTO>();
        query = @"   SELECT u.UserId UserId
	                    , u.ModuleId, m.[Name] ModuleName, m.DisplayName ModuleDisplayName
	                    , u.FunctionId, f.[Name] FunctionName, f.DisplayName FunctionDisplayName
	                    , u.FunctionPermission, u.FunctionAuditable
	                    , u.InActive FunctionInActive
                    INTO #LoggedUserPermission
                    FROM SysModule m 
	                    INNER JOIN SysFunction f ON f.ModuleId = m.Id 
	                    INNER JOIN UserPermission u ON u.ModuleId = m.Id 
		                    AND u.FunctionId = f.Id
		                    AND u.UserId = " + currentUserId + @"
		                    AND u.DeletedBy IS NULL
		                    AND u.InActive = 0 

                    SELECT ISNULL(u.UserId, 0) UserId
	                    , l.ModuleId, l.ModuleName, l.ModuleDisplayName
	                    , l.FunctionId, l.FunctionName, l.FunctionDisplayName
	                    , l.FunctionPermission, l.FunctionAuditable
	                    , l.FunctionInActive
                    FROM #LoggedUserPermission l
	                    LEFT JOIN UserPermission u ON u.ModuleId = l.ModuleId
		                    AND u.FunctionId = l.FunctionId
		                    AND u.DeletedBy IS NULL
		                    AND u.InActive = 0
		                    AND u.UserId = " + userId + @"

                    DROP TABLE #LoggedUserPermission
	                                                        ";

        var result = DB.Query<UserPermissionDTO>(query).ToList();
        if (result != null && result.Count > 0)
        {
            foreach (var m in result)
            {
                var currentModule = list.Where(x => x.Module == m.ModuleName).FirstOrDefault();
                if (currentModule == null)
                {
                    currentModule = new UserModuleDTO()
                    {
                        UserId = m.UserId,
                        ModuleId = m.ModuleId,
                        Module = m.ModuleName,
                        Functions = new List<UserFunctionDTO>()
                    };
                    list.Add(currentModule);
                }
                UserFunctionDTO function = new UserFunctionDTO()
                {
                    UserId = m.UserId,
                    FunctionId = m.FunctionId,
                    Function = m.FunctionName,
                    Permission = m.FunctionPermission,
                    //Auditable = m.FunctionAuditable,
                    InActive = m.FunctionInActive,
                };
                currentModule.Functions.Add(function);
            }
        }
        return new ResponseDTO() { IsValid = true, ErrorKey = string.Empty, Response = list }; ;
    }
    //public List<UserPermissionDTO> GetCurrentUserPermission(long userId)
    //{
    //    string query = @"   
    //                        SELECT u.UserId UserId
    //                             , u.ModuleId, u.ModuleName, m.DisplayName ModuleDisplayName
    //                             , u.ModulePermission, u.ModuleAuditable
    //                             , u.FunctionId, u.FunctionName, f.DisplayName FunctionDisplayName
    //                             , u.FunctionPermission, u.FunctionAuditable
    //                             , u.InActive FunctionInActive
    //                        FROM SysModule m 
    //                         INNER JOIN SysFunction f ON f.ModuleId = m.Id 
    //                         INNER JOIN UserPermission up ON up.ModuleId = m.Id AND up.UserId = "+ userId + @"
    //                          AND ISNULL(up.DeletedBy, 0) = 0 
    //                          AND up.InActive = 0 
    //                         INNER JOIN UserPermissionDetail u ON u.UserPermissionId = up.Id
    //                          AND u.FunctionId = f.Id
    //                          AND ISNULL(u.DeletedBy, 0) = 0
    //                          AND u.InActive = 0

    //                    ";

    //    return DB.Query<UserPermissionDTO>(query).ToList();
    //}
    //public ResponseDTO SaveUserPermission_Old(long currentUserId, List<UserPermissionDTO> permissions)
    //{
    //    var res = ValidateList_Old(currentUserId, permissions);
    //    if (!res.IsValid)
    //        return res;
    //    StringBuilder query = new StringBuilder();
    //    query.AppendLine(@" DECLARE @CurrentUserId BIGINT = " + currentUserId + @"
    //                        DECLARE @UserPermissionId BIGINT = 0;
    //                        DECLARE @UserPermissionDetailId BIGINT = 0;
    //                        DECLARE @Error NVARCHAR(MAX) = '';

    //                        BEGIN TRANSACTION [SavePermission]
    //                        BEGIN TRY


    //                    ");
    //    foreach (UserPermissionDTO obj in permissions)
    //    {
    //        query.AppendLine(@" SET @UserPermissionId = 0;
    //                            SET @UserPermissionDetailId = 0; 

    //                            IF NOT EXISTS ( SELECT Id FROM SysFunction 
    //                                        WHERE ModuleId = " + obj.ModuleId + @" AND Id = " + obj.FunctionId + @" AND Name = '" + obj.FunctionName + @"'
    //                                        )
    //                            BEGIN RAISERROR(N'Function (" + obj.FunctionName + @") not exists in module (" + obj.ModuleName + @")', 11, 1); END
    //                            ELSE
    //                            BEGIN

    //                                SELECT @UserPermissionId = ISNULL(Id, 0) 
    //                                FROM UserPermission WHERE UserId = " + obj.UserId + @" AND ModuleId = " + obj.ModuleId + @";

    //                                IF @UserPermissionId = 0
    //                                BEGIN
    //                                    INSERT INTO UserPermission(UserId, ModuleId, InActive, CreatedBy, CreateDate, UpdatedBy, UpdateDate)
    //                                    VALUES (" + obj.UserId + @", " + obj.ModuleId + @", 0, @CurrentUserId, GETDATE(), @CurrentUserId, GETDATE());  
    //                                    SELECT @UserPermissionId = SCOPE_IDENTITY();
    //                                END

    //                                SELECT @UserPermissionDetailId = ISNULL(Id, 0) 
    //                                FROM UserPermissionDetail 
    //                                WHERE UserPermissionId = @UserPermissionId 
    //                                    AND FunctionId = " + obj.FunctionId + @"
    //                                    AND UserId = " + obj.UserId + @"
    //                                    AND ModuleId = " + obj.ModuleId + @";
    //                                IF @UserPermissionDetailId = 0
    //                                BEGIN
    //                                    INSERT INTO UserPermissionDetail(UserPermissionId, UserId, ModuleId, ModuleName
    //                                        , ModulePermission, ModuleAuditable
    //                                        , FunctionId, FunctionName, FunctionPermission, FunctionAuditable
    //                                        , InActive, CreatedBy, CreateDate, UpdatedBy, UpdateDate)
    //                                    VALUES (@UserPermissionId, " + obj.UserId + @", " + obj.ModuleId + @", '" + obj.ModuleName + @"'
    //                                        , '" + obj.ModulePermission + @"', '" + obj.ModuleAuditable + @"'
    //                                        , " + obj.FunctionId + @", '" + obj.FunctionName + @"', '" + obj.FunctionPermission + @"', '" + obj.FunctionAuditable + @"'
    //                                        , '" + obj.FunctionInActive + @"', @CurrentUserId, GETDATE(), @CurrentUserId, GETDATE());  
    //                                END
    //                                ELSE
    //                                BEGIN
    //                                    UPDATE UserPermissionDetail SET
    //                                        ModulePermission = '" + obj.ModulePermission + @"'
    //                                        , ModuleAuditable = '" + obj.ModuleAuditable + @"'
    //                                        , FunctionPermission = '" + obj.FunctionPermission + @"'
    //                                        , FunctionAuditable = '" + obj.FunctionAuditable + @"'
    //                                        , InActive = '" + obj.FunctionInActive + @"'
    //                                        , UpdatedBy = @CurrentUserId
    //                                        , UpdateDate = GETDATE()
    //                                    WHERE UserPermissionId = @UserPermissionId 
    //                                        AND FunctionId = " + obj.FunctionId + @"
    //                                        AND UserId = " + obj.UserId + @"
    //                                        AND ModuleId = " + obj.ModuleId + @";
    //                                END
    //                            END

    //                            ");
    //    }
    //    query.AppendLine(@" COMMIT TRANSACTION [SavePermission] 
    //                        SELECT @Error;
    //                        END TRY
    //                        BEGIN CATCH
    //                            ROLLBACK TRANSACTION [SavePermission]
    //                            SELECT @Error=ERROR_MESSAGE();
    //                            SELECT @Error;
    //                        END CATCH");
    //    string error = DB.ExecuteScalar<string>(query.ToString());
    //    if (!string.IsNullOrEmpty(error))
    //        return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };
    //    return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = null };
    //}
    public ResponseDTO SaveUserPermission(long currentUserId, long parentUserId, List<SaveUserPermissionDTO> permissions)
    {
        var res = ValidateList(currentUserId, parentUserId, permissions);
        if (!res.IsValid)
            return res;
        List<long> userIds = permissions.Select(x => x.UserId).Distinct().ToList();
        StringBuilder query = new StringBuilder();
        query.AppendLine(@" DECLARE @CurrentUserId BIGINT = " + currentUserId + @"
                            DECLARE @UserPermissionId BIGINT = 0;
                            DECLARE @UserId BIGINT = 0;
                            DECLARE @ModuleId BIGINT = 0;
                            DECLARE @FunctionId BIGINT = 0;
                            DECLARE @Error NVARCHAR(MAX) = '';
                            DECLARE @CurrentDate DATETIME = (SELECT GETDATE());
                            BEGIN TRANSACTION [SavePermission]
                            BEGIN TRY
                            
                                CREATE TABLE #FinalIds(Id BIGINT);
                            
                        ");
        foreach (var obj in permissions)
        {
            query.AppendLine(@" SET @UserPermissionId = 0;
                                SET @UserId = " + obj.UserId + @";
                                SET @ModuleId = " + obj.ModuleId + @";
                                SET @FunctionId = " + obj.FunctionId + @";

                                IF NOT EXISTS ( 
                                                SELECT Id FROM SysFunction 
                                                WHERE ModuleId = @ModuleId AND Id = @FunctionId
                                            )
                                BEGIN RAISERROR(N'Function (@FunctionId) not exists in module (@ModuleId)', 11, 1); END
                                ELSE
                                BEGIN
    
                                    UPDATE UserPermission 
                                    SET InActive = 1, DeletedBy = @CurrentUserId, DeleteDate = @CurrentDate
                                    WHERE UserId = @UserId AND ModuleId = @ModuleId AND FunctionId = @FunctionId; 

                                    SELECT @UserPermissionId = ISNULL(Id, 0) 
                                    FROM UserPermission 
                                    WHERE UserId = @UserId AND ModuleId = @ModuleId AND FunctionId = @FunctionId;
                                
                                    IF @UserPermissionId = 0
                                    BEGIN
                                        INSERT INTO UserPermission(UserId, ModuleId, FunctionId, FunctionPermission, FunctionAuditable
                                                , InActive, CreatedBy, CreateDate, UpdatedBy, UpdateDate)
                                        SELECT @UserId, @ModuleId, @FunctionId, '" + obj.FunctionPermission + @"', Auditable
                                                , '" + obj.FunctionInActive + @"', @CurrentUserId, @CurrentDate, @CurrentUserId, @CurrentDate
                                        FROM SysFunction
                                        WHERE ModuleId = @ModuleId AND Id = @FunctionId;

                                        SELECT @UserPermissionId = SCOPE_IDENTITY();
                                    END
                                    ELSE
                                    BEGIN
                                        UPDATE UserPermission SET
                                            FunctionPermission = '" + obj.FunctionPermission + @"'
                                            --, FunctionAuditable = 
                                            , InActive = '" + obj.FunctionInActive + @"'
                                            , UpdatedBy = @CurrentUserId
                                            , UpdateDate = @CurrentDate
                                            , DeletedBy = NULL
                                            , DeleteDate = NULL
                                        WHERE Id = @UserPermissionId AND UserId = @UserId AND ModuleId = @ModuleId AND FunctionId = @FunctionId
                                    END

                                    INSERT INTO #FinalIds VALUES(@UserPermissionId);

                                END
                                
                                ");
        }
        query.AppendLine(@" 
                            UPDATE UserPermission 
                            SET InActive = 1, DeletedBy = @CurrentUserId, DeleteDate = @CurrentDate
                            WHERE UserId IN (" + string.Join(",", userIds) + @") 
                                AND Id NOT IN (SELECT Id FROM #FinalIds);
                            DROP TABLE #FinalIds;

                            COMMIT TRANSACTION [SavePermission] 
                            SELECT @Error;
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [SavePermission]
                                SELECT @Error=ERROR_MESSAGE();
                                SELECT @Error;
                            END CATCH");
        string error = DB.ExecuteScalar<string>(query.ToString());
        if (!string.IsNullOrEmpty(error))
            return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = null };
    }

    private ResponseDTO ValidateList(long currentUserId, long parentUserId, List<SaveUserPermissionDTO> permissions)
    {
        if (permissions == null || permissions.Count <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "Empty List", Response = null };

        List<long> userIds = permissions.Select(x => x.UserId).Distinct().ToList();
        List<long> moduleIds = permissions.Select(x => x.ModuleId).Distinct().ToList();
        List<long> functionIds = permissions.Select(x => x.FunctionId).Distinct().ToList();

        // Validate that the current user is authorized to give access permission to the list of users
        string validateQuery = @"   CREATE TABLE #ValidateTable(Id BIGINT);";
        foreach (var userId in userIds)
            validateQuery += @" 
                                INSERT INTO #ValidateTable VALUES('" + userId + @"');
                              ";
        validateQuery += @" SELECT Id 
                            FROM #ValidateTable 
                            WHERE Id NOT IN 
                                (
                                    SELECT Id FROM [User] 
                                    WHERE Id IN (" + string.Join(",", userIds) + @") 
                                        AND (ParentId = " + currentUserId + @" OR ParentId = " + parentUserId + @")
                                );
                            DROP TABLE #ValidateTable;";
        var result = DB.Query<long>(validateQuery).ToList();
        if (result != null && result.Count > 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "YouAreNotAuthorizedToEditTheseUsers", Response = result };

        //Validate that module Ids are valid
        validateQuery = @"  CREATE TABLE #ValidateTable(Id BIGINT);";
        foreach (var moduleId in moduleIds)
            validateQuery += @" INSERT INTO #ValidateTable VALUES('" + moduleId + @"');
                              ";
        validateQuery += @" SELECT Id 
                            FROM #ValidateTable 
                            WHERE Id NOT IN (
                                                SELECT ModuleId 
                                                FROM UserPermission
                                                WHERE UserId = " + currentUserId + @"   
                                             )
                            DROP TABLE #ValidateTable;";
        var moduleResult = DB.Query<string>(validateQuery).ToList();
        if (moduleResult != null && moduleResult.Count > 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "YouAreNotAuthorizedToEditTheseModules", Response = moduleResult };

        //Validate that function Ids are valid
        validateQuery = @"  CREATE TABLE #ValidateTable(Id BIGINT);";
        foreach (var functionId in functionIds)
            validateQuery += @" INSERT INTO #ValidateTable VALUES('" + functionId + @"');
                              ";
        validateQuery += @" 
                            SELECT Id 
                            FROM #ValidateTable
                            WHERE Id NOT IN (
                                                SELECT FunctionId 
                                                FROM UserPermission
                                                WHERE UserId = " + currentUserId + @"   
                                            )
                            DROP TABLE #ValidateTable;";
        var functionResult = DB.Query<string>(validateQuery).ToList();
        if (functionResult != null && functionResult.Count > 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "YouAreNotAuthorizedToEditTheseFunctions", Response = functionResult };

        return new ResponseDTO() { IsValid = true };
    }
    private ResponseDTO ValidateList_Old(long currentUserId, List<UserPermissionDTO> permissions)
    {
        if (permissions == null || permissions.Count <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "Empty List", Response = null };

        List<long> userIds = permissions.Select(x => x.UserId).Distinct().ToList();
        List<string> moduleNames = permissions.Select(x => x.ModuleName).Distinct().ToList();
        List<string> functionNames = permissions.Select(x => x.FunctionName).Distinct().ToList();

        // Validate that the current user is authorized to give access permission to the list of users
        string validateQuery = @"   CREATE TABLE #ValidateTable([Name] NVARCHAR(MAX));";
        foreach (var id in userIds)
            validateQuery += @" INSERT INTO #ValidateTable VALUES('" + id + @"');
                              ";
        validateQuery += @" SELECT [Name] 
                            FROM #ValidateTable 
                            WHERE [NAME] NOT IN 
                                (
                                    SELECT Id FROM [User] 
                                    WHERE Id IN (" + string.Join(",", userIds) + @") 
                                        AND ParentId = " + currentUserId + @" 
                                );
                            DROP TABLE #ValidateTable;";
        var result = DB.Query<long>(validateQuery).ToList();
        if (result != null && result.Count > 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "YouAreNotAuthorizedToEditTheseUsers", Response = result };

        //Validate that module names are valid
        validateQuery = @"  CREATE TABLE #ValidateTable([Name] NVARCHAR(MAX));";
        foreach (var name in moduleNames)
            validateQuery += @" INSERT INTO #ValidateTable VALUES('" + name + @"');
                              ";
        validateQuery += @" SELECT [Name] 
                            FROM #ValidateTable 
                            WHERE [NAME] NOT IN (
                                                 SELECT [Name] FROM SysModule m
						                            INNER JOIN UserPermission u ON u.ModuleId = m.Id
						                            AND u.UserId = " + currentUserId + @"   
                                                )
                            DROP TABLE #ValidateTable;";
        var moduleResult = DB.Query<string>(validateQuery).ToList();
        if (moduleResult != null && moduleResult.Count > 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidModuleNames", Response = moduleResult };

        //Validate that function names are valid
        validateQuery = @"  CREATE TABLE #ValidateTable([Name] NVARCHAR(MAX));";
        foreach (var name in functionNames)
            validateQuery += @" INSERT INTO #ValidateTable VALUES('" + name + @"');
                              ";
        validateQuery += @" 
                            SELECT v.[Name] 
                            FROM #ValidateTable v
                            WHERE v.[Name] NOT IN (
                                                    SELECT f.[Name] FROM SysFunction f
                                                        INNER JOIN SysModule m ON m.Id = f.ModuleId
                                                        INNER JOIN UserPermission u ON u.ModuleId = m.Id
						                                    AND u.UserId = " + currentUserId + @"   
                                                )
                            DROP TABLE #ValidateTable;";
        var functionResult = DB.Query<string>(validateQuery).ToList();
        if (functionResult != null && functionResult.Count > 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidFunctionNames", Response = functionResult };

        return new ResponseDTO() { IsValid = true };
    }

    public async Task<UserDTO.RequestInfo> GetUserInfoPerRequest(long userId, string token, string moduleName, string functionName)
    {
        string query = @"   DECLARE @UserId BIGINT = " + userId + @";
                            DECLARE @Token NVARCHAR(MAX) = '" + token + @"';
                            DECLARE @ValidTokenMinutes INT = " + AppSettings.Instance.ValidTokenMinutes + @";
                            DECLARE @ModuleName NVARCHAR(MAX) = '" + moduleName + @"';
                            DECLARE @FunctionName NVARCHAR(MAX) = '" + functionName + @"';
                            DECLARE @ModuleId NVARCHAR(MAX) = 0;
                            DECLARE @FunctionId NVARCHAR(MAX) = 0;

                            
                            DECLARE @TokenDate DATETIME = NULL;
                            DECLARE @TokenValue NVARCHAR(50) = '';
                            DECLARE @PermissionValue NVARCHAR(50) = '';
                            DECLARE @AuditValue BIT = 0;
                            
                            BEGIN TRANSACTION [UserToken]
                            BEGIN TRY
                                -- Validate token
                                SELECT @TokenDate = UpdateDate FROM Token WHERE [Text] = @Token AND UserId = @UserId;

                                IF @TokenDate IS NULL 
	                                SET @TokenValue = 'InvalidToken';
                                ELSE IF DATEADD(MI, @ValidTokenMinutes, @TokenDate) < GETDATE()
	                                SET @TokenValue = 'TokenExpired';
                                ELSE
                                BEGIN
                                -- Update token date
                                    UPDATE Token SET UpdateDate = GETDATE() WHERE [Text] = @Token AND UserId = @UserId;

                                -- Delete all token related to this user other than the current token
                                    DELETE FROM Token WHERE [Text] <> @Token AND UserId = @UserId

                                -- Read module id and function id based on controller name and action name
                                    SELECT @ModuleId = ISNULL(f.ModuleId, 0), @FunctionId = ISNULL(f.Id, 0)
                                    FROM SysFunction f 
                                        INNER JOIN SysModule m ON m.Id = f.ModuleId
                                    WHERE m.[Name] = @ModuleName AND f.[Name] = @FunctionName
                                        
                                -- Validate permission
                                    SELECT @PermissionValue = ISNULL(FunctionPermission, '')
	                                    , @AuditValue = ISNULL(FunctionAuditable, 0)
                                    FROM UserPermission 
                                    WHERE UserId = @UserId 
                                        AND ModuleId = @ModuleId AND FunctionId = @FunctionId AND InActive = 0

                                    IF @PermissionValue = ''
	                                    SET @PermissionValue = 'AccessPermissionDenied';
                                END
	                            
                            COMMIT TRANSACTION [UserToken] 
                                SELECT @UserId UserId, @TokenValue TokenValue, @PermissionValue PermissionValue, @AuditValue AuditValue
                                    , @ModuleName Controller, @FunctionName Action
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [UserToken]
                            END CATCH
                            
                            
                        ";
        var userRequestInfo = await DB.QueryAsync<UserDTO.RequestInfo>(query);
        return userRequestInfo.FirstOrDefault();
    }
}