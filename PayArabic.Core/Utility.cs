using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace PayArabic.Core;

public class Utility
{
    public static string GetMessageError(Exception ex)
    {
        string error;
        if (ex.InnerException == null)
            error = ex.Message;
        else
            return GetMessageError(ex.InnerException);
        return error;
    }
    public static string Wrap(string Str)
    {
        if (string.IsNullOrEmpty(Str))
            return "";
        else
        {
            try
            {
                if (Str.IndexOf("'") != -1)
                    Str = Str.Replace("'", "`");
            }
            catch { }
            return Str;
        }
    }
    public static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using (var hmac = new HMACSHA512())
        {
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }
    }
    public static bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        using (var hmac = new HMACSHA512(passwordSalt))
        {
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != passwordHash[i]) return false;
            }
        }
        return true;
    }
    public static string ValidateFileUploadExt(string file_base64)
    {
        string[] splitted = file_base64.Split("base64,");
        string data = splitted.Length > 1?splitted[1]:splitted[0];
        data = data.Substring(0, 5);

        switch (data.ToUpper())
        {
            case "IVBOR":
                return "png";
            case "/9J/4":
                return "jpg";
            case "JVBER":
                return "pdf";            
            default:
                return string.Empty;
        }
    }
    public static double ValidateFileUploadSize(string file_base64)
    {
        // Remove MIME-type from the base64 if exists
        var length = file_base64.Contains("base64,")  ? file_base64.Split(',')[1].Length : file_base64.Length;

        double fileSizeInByte = Math.Ceiling((double)length / 4) * 3;

        return fileSizeInByte;
    }
    public static string ValidateFileUpload(string file_base64)
    {
        if (ValidateFileUploadExt(file_base64) == "")
            return "AllowedExtension";
        if (ValidateFileUploadSize(file_base64) > 1000000)
            return "AllowedSize";
        return string.Empty;
    }
    public static string toEnglishNumber(string input)
    {
        string EnglishNumbers = "";

        for (int i = 0; i < input.Length; i++)
        {
            if (Char.IsDigit(input[i]))
            {
                EnglishNumbers += char.GetNumericValue(input, i);
            }
            else
            {
                EnglishNumbers += input[i].ToString();
            }
        }
        return EnglishNumbers;
    }
    public static string GetRandomEmailKey()
    {
        string LOWER_CASE = "abcdefghijklmnopqursuvwxyz";
        string UPPER_CAES = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string NUMBERS = "1234567890";
        char[] _password = new char[16];
        Random _random = new Random();
        string charSet = LOWER_CASE + UPPER_CAES + NUMBERS;
        for (int i = 0; i < _password.Length; i++)
            _password[i] = charSet[_random.Next(charSet.Length - 1)];
        return string.Join(null, _password);
    }
    public static string GetRandomMobileKey()
    {
        string NUMBERS = "1234567890";
        char[] _password = new char[6];
        Random _random = new Random();
        string charSet = NUMBERS;
        for (int i = 0; i < _password.Length; i++)
            _password[i] = charSet[_random.Next(charSet.Length - 1)];
        return string.Join(null, _password);
    }
    public static string HtmlEntitesDecode(string Str)
    {
        Str = System.Web.HttpUtility.HtmlDecode(Str);
        Str = System.Uri.UnescapeDataString(Str);
        return Str;
    }
    public static bool ValidateDate(object date)
    {
        try
        {
            Convert.ToDateTime(date);
            return true;
        }
        catch
        {
            return false;
        }
    }
    public static string GetRandomInvoiceKey(object invoice_Id)
    {
        string NUMBERS = "1234567890";
        char[] _key = new char[8];
        Random _random = new Random();
        string charSet = NUMBERS;
        for (int i = 0; i < _key.Length; i++)
        {
            _key[i] = charSet[_random.Next(charSet.Length - 1)];
        }
        string key = string.Join(null, _key) + invoice_Id.ToString();
        return key;
    }
    public static async Task<bool> ValidateCaptcha(string token, string userAction, string RecaptchaSecretKey, string RECAPTCHA_PASS)
    {
        //skipping for mobile
        if (token == RECAPTCHA_PASS) return true;

        var dictionary = new Dictionary<string, string>
                {
                    { "secret",  RecaptchaSecretKey},
                    { "response", token }
                };

        var postContent = new FormUrlEncodedContent(dictionary);
        HttpResponseMessage recaptchaResponse = null;
        string stringContent = "";
        // Call recaptcha api and validate the token
        using (var http = new HttpClient())
        {
            recaptchaResponse = await http.PostAsync("https://www.google.com/recaptcha/api/siteverify", postContent);
            stringContent = await recaptchaResponse.Content.ReadAsStringAsync();
        }
        if (!recaptchaResponse.IsSuccessStatusCode)
        {
            //return new SignupResponse() { Success = false, Error = "Unable to verify recaptcha token", ErrorCode = "S03" };
            return false;
        }
        if (string.IsNullOrEmpty(stringContent))
        {
            //return new SignupResponse() { Success = false, Error = "Invalid reCAPTCHA verification response", ErrorCode = "S04" };
            return false;
        }
        var googleReCaptchaResponse = JsonConvert.DeserializeObject<ReCaptchaResponse>(stringContent);
        if (!googleReCaptchaResponse.Success)
        {
            //var errors = string.Join(",", googleReCaptchaResponse.ErrorCodes);
            //return new SignupResponse() { Success = false, Error = errors, ErrorCode = "S05" };
            return false;
        }


        if (!googleReCaptchaResponse.Action.Equals(userAction, StringComparison.OrdinalIgnoreCase))
        {
            // This is important just to verify that the exact action has been performed from the UI
            //return new SignupResponse() { Success = false, Error = "Invalid action", ErrorCode = "S06" };
            return false;
        }

        // Captcha was success , let's check the score, in our case, for example, anything less than 0.5 is considered as a bot user which we would not allow ...
        // the passing score might be higher or lower according to the sensitivity of your action

        if (googleReCaptchaResponse.Score < 0.5)
        {
            //return new SignupResponse() { Success = false, Error = "This is a potential bot. Signup request rejected", ErrorCode = "S07" };
            return false;
        }

        //TODO: Continue with doing the actual signup process, since now we know the request was done by potentially really human

        return true;
    }

    #region Integration
    public static string EncryptString(string stringToEncrypt, string usedKey)
    {
        byte[] key = Encoding.ASCII.GetBytes(usedKey);
        // Instantiate a new Aes object to perform string symmetric encryption
        Aes encryptor = Aes.Create();

        encryptor.Mode = CipherMode.CBC;
        //encryptor.KeySize = 256;
        encryptor.BlockSize = 128;
        //encryptor.Padding = PaddingMode.Zeros;

        // Set key and IV
        encryptor.Key = key; encryptor.IV = key;

        // Instantiate a new MemoryStream object to contain the encrypted bytes
        MemoryStream memoryStream = new MemoryStream();

        // Instantiate a new encryptor from our Aes object
        ICryptoTransform aesEncryptor = encryptor.CreateEncryptor();

        // Instantiate a new CryptoStream object to process the data and write it to the 
        // memory stream
        CryptoStream cryptoStream = new CryptoStream(memoryStream, aesEncryptor, CryptoStreamMode.Write);

        // Convert the plainText string into a byte array
        byte[] plainBytes = Encoding.ASCII.GetBytes(stringToEncrypt);

        // Encrypt the input plaintext string
        cryptoStream.Write(plainBytes, 0, plainBytes.Length);

        // Complete the encryption process
        cryptoStream.FlushFinalBlock();

        // Convert the encrypted data from a MemoryStream to a byte array
        byte[] cipherBytes = memoryStream.ToArray();

        // Close both the MemoryStream and the CryptoStream
        memoryStream.Close();
        cryptoStream.Close();

        // Convert the encrypted byte array to a base64 encoded string
        string cipherText = Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length);


        byte[] data = Convert.FromBase64String(cipherText);


        StringBuilder hex = new StringBuilder(data.Length * 2);
        foreach (byte b in data)
            hex.AppendFormat("{0:x2}", b);

        // Return the encrypted data as a string
        cipherText = System.Web.HttpUtility.UrlEncode(hex.ToString(), Encoding.UTF8);

        return cipherText;
    }

    public static string DecryptString(string stringToDecrypt, string usedKey)
    {
        byte[] key = Encoding.ASCII.GetBytes(usedKey);
        byte[] cipherBytes = Enumerable.Range(0, stringToDecrypt.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(stringToDecrypt.Substring(x, 2), 16))
                .ToArray();

        // Instantiate a new Aes object to perform string symmetric encryption
        Aes encryptor = Aes.Create();

        encryptor.Mode = CipherMode.CBC;
        //encryptor.KeySize = 256;
        encryptor.BlockSize = 128;
        //encryptor.Padding = PaddingMode.Zeros;

        // Set key and IV
        encryptor.Key = key;
        encryptor.IV = key;

        // Instantiate a new MemoryStream object to contain the encrypted bytes
        MemoryStream memoryStream = new MemoryStream();

        // Instantiate a new encryptor from our Aes object
        ICryptoTransform aesDecryptor = encryptor.CreateDecryptor();

        // Instantiate a new CryptoStream object to process the data and write it to the 
        // memory stream
        CryptoStream cryptoStream = new CryptoStream(memoryStream, aesDecryptor, CryptoStreamMode.Write);

        // Will contain decrypted plaintext
        string plainText = String.Empty;

        try
        {
            // Convert the ciphertext string into a byte array
            //byte[] cipherBytes = Convert.FromBase64String(cipherText);

            // Decrypt the input ciphertext string
            cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);

            // Complete the decryption process
            cryptoStream.FlushFinalBlock();

            // Convert the decrypted data from a MemoryStream to a byte array
            byte[] plainBytes = memoryStream.ToArray();

            // Convert the decrypted byte array to string
            plainText = Encoding.ASCII.GetString(plainBytes, 0, plainBytes.Length);
        }
        finally
        {
            // Close both the MemoryStream and the CryptoStream
            memoryStream.Close();
            cryptoStream.Close();
        }

        // Return the decrypted data as a string
        return plainText;
    }
    #endregion

    public static KnetResponse KnetReadData(string knetData)
    {
        KnetResponse response = new KnetResponse();
        string[] data = knetData.Split('&');
        if (data.Length > 0)
        {
            foreach (var item in data)
            {
                string[] content = item.Split('=');
                if (content[0] == "paymentid") response.PaymentId = content[1];
                else if (content[0] == "result") response.Result = content[1];
                else if (content[0] == "auth") response.Auth = content[1];
                else if (content[0] == "avr") response.AVR = content[1];
                else if (content[0] == "ref") response.REF = content[1];
                else if (content[0] == "tranid") response.TranId = content[1];
                else if (content[0] == "postdate") response.PostDate = content[1];
                else if (content[0] == "trackid") response.TrackId = content[1];
                else if (content[0] == "amt") response.AMT = content[1];
                else if (content[0] == "authRespCode") response.AuthRespCode = content[1];
                else if (content[0] == "udf1") response.UDF1 = content[1];
                else if (content[0] == "udf2") response.UDF2 = content[1];
                else if (content[0] == "udf3") response.UDF3 = content[1];
                else if (content[0] == "udf4") response.UDF4 = content[1];
                else if (content[0] == "udf5") response.UDF5 = content[1];
                else if (content[0] == "udf6") response.UDF6 = content[1];
                else if (content[0] == "udf7") response.UDF7 = content[1];
                else if (content[0] == "udf8") response.UDF8 = content[1];
                else if (content[0] == "udf9") response.UDF9 = content[1];
                else if (content[0] == "udf10") response.UDF10 = content[1];
            }
        }
        return response;
    }
}

public class ReCaptchaResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }
    
    [JsonProperty("score")]
    public float Score { get; set; }
    
    [JsonProperty("action")]
    public string Action { get; set; }
    
    [JsonProperty("challenge_ts")]
    public DateTime ChallengeTs { get; set; } // timestamp of the challenge load (ISO format yyyy-MM-dd'T'HH:mm:ssZZ)
    
    [JsonProperty("hostname")]
    public string HostName { get; set; }    // the hostname of the site where the reCAPTCHA was solved
    
    [JsonProperty("error-codes")]
    public string[] ErrorCodes { get; set; }
}