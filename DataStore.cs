﻿using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Tomlet;
using DataStoreDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, long>>;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Runtime;
using Amazon;

namespace ModStats
{
    public static class DataStore
    {
        static DataStoreDict data = new();
        static Dictionary<string, long> secrets;
        static AmazonS3Client s3;
        static TransferUtility uploadUtil;
        static HashAlgorithm hasher = MD5.Create();
        static readonly TimeSpan cloudUploadDelay = TimeSpan.FromHours(6);
        static volatile bool LOCKACQUIRED = false;

        //public static bool LOCKACQUIRED 
        //{ 
        //    get => __locked; 
        //    set => __locked = value;
        //}

        static DataStore()
        {
            //return;
            var envs = Environment.GetEnvironmentVariables();
            
            Console.WriteLine("AWS Config: ");
            Console.WriteLine(" - CLD_BKT = " + EnvironVars.CloudBucket);
            Console.WriteLine(" - CLD_PTH = " + EnvironVars.CloudPath);
            Console.WriteLine(" - CLD_AKY = " + EnvironVars.CloudAccessKey);
            Console.WriteLine(" - CLD_SKY = " + EnvironVars.CloudSecretKey);
            foreach (var item in envs)
            {
                Console.WriteLine($"ENVAR: {item}");
            }
            
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Save();
            if (!File.Exists(EnvironVars.DatastoreLocalPath)) File.Create(EnvironVars.DatastoreLocalPath).Dispose();

            //AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;
            //AWSConfigs.LoggingConfig.LogResponses = ResponseLoggingOption.OnError;

            s3 = new AmazonS3Client(new BasicAWSCredentials(EnvironVars.CloudAccessKey, EnvironVars.CloudSecretKey), Amazon.RegionEndpoint.USEast1);
            uploadUtil = new(s3);
            Console.WriteLine("Created AWS objects");

            Load();
            Console.WriteLine("Loaded DataStore data");

            Console.WriteLine("Internal Bless: " + secrets[EnvironVars.DatastoreInternalsKey]);
            Console.WriteLine("DataStore Path: " + Path.GetFullPath(EnvironVars.DatastoreLocalPath));

            new Thread(UploadThread)
            {
                Priority = ThreadPriority.BelowNormal,
                IsBackground = true,
            }
            .Start();
            Console.WriteLine("Started background upload thread");
        }

        [MemberNotNull(nameof(data), nameof(secrets))]
        static async void Load()
        {
            try
            {
                //Task.Run(DownloadFromCloud).GetAwaiter().GetResult();
#pragma warning disable CS8774 // Member must have a non-null value when exiting.
                await DownloadFromCloud();
#pragma warning restore CS8774 // Member must have a non-null value when exiting.
            }
            catch (Exception e)
            {
                Console.WriteLine($"FAILED TO FETCH FROM AWS! {e}");
            }

            Console.WriteLine("AWS check in finished");

            string serializedData = File.ReadAllText(EnvironVars.DatastoreLocalPath);
            data = TomletMain.To<DataStoreDict>(serializedData);
            Console.WriteLine("Read file, converted to C# object");

            if (data.ContainsKey(EnvironVars.DatastoreInternalsKey))
            {
                secrets = data[EnvironVars.DatastoreInternalsKey];
            }
            else
            {
                secrets = new();
                data[EnvironVars.DatastoreInternalsKey] = secrets;
            }
            secrets[EnvironVars.DatastoreInternalsKey] = Hash(EnvironVars.DatastireInternalsPass);
            
            Console.WriteLine("Secrets set successfully");
        }

        static void Save()
        {
            while (LOCKACQUIRED) { }
            LOCKACQUIRED = true;

            try 
            {
                string serializedData = TomletMain.TomlStringFrom(data);
                File.WriteAllText(EnvironVars.DatastoreLocalPath, serializedData);
            }
            finally { LOCKACQUIRED = false; }
        }

        internal static void CollectionModified()
        {
            new Thread(Save)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
            }.Start();
        }

        // https://stackoverflow.com/questions/8820399/c-sharp-4-0-how-to-get-64-bit-hash-code-of-given-string
        static long Hash(string str)
        {
            var bytes = hasher.ComputeHash(Encoding.Default.GetBytes(str));
            Array.Resize(ref bytes, bytes.Length + bytes.Length % 8); //make multiple of 8 if hash is not, for exampel SHA1 creates 20 bytes. 
            return Enumerable.Range(0, bytes.Length / 8) // create a counter for de number of 8 bytes in the bytearray
                .Select(i => BitConverter.ToInt64(bytes, i * 8)) // combine 8 bytes at a time into a integer
                .Aggregate((x, y) => x ^ y); //xor the bytes together so you end up with a long (64-bit int)
        }

        internal static void CreateCategory(string name, string pass, long blessing)
        {
            if (name == EnvironVars.DatastoreInternalsKey) throw new Exception("Unauthorized - reserved name");

            if (blessing != secrets[EnvironVars.DatastoreInternalsKey]) throw new Exception("Unauthorized - bless unmatched");
            secrets[name] = Hash(pass);
            data[name] = new();
            CollectionModified();
        }

        internal static void DeleteCategory(string name, string pass)
        {
            if (name == EnvironVars.DatastoreInternalsKey) throw new Exception("Unauthorized - reserved name");

            if (secrets[name] != Hash(pass)) throw new Exception("Unauthorized - pass hash unmatched");
            data.Remove(name);
            CollectionModified();
        }

        internal static bool PassMatchesCategory(string name, string pass)
        {
            if (name == EnvironVars.DatastoreInternalsKey) throw new Exception("Unauthorized - reserved name");

            if (secrets[name] == Hash(pass))
                return true;
            return false;
        }

        internal static Dictionary<string, long> GetCategory(string name)
        {
            if (name == EnvironVars.DatastoreInternalsKey) throw new Exception("Unauthorized - reserved name");

            return data[name];
        }

        internal static Task SaveToCloud(long bless)
        {
            if (bless != secrets[EnvironVars.DatastoreInternalsKey]) throw new Exception("Unauthorized - incorrect bless");

            return SaveToCloud();
        }

        private static async Task DownloadFromCloud()
        {
            FileInfo finf = new(EnvironVars.DatastoreLocalPath);
            var metadataReq = new Amazon.S3.Model.GetObjectMetadataRequest()
            {
                BucketName = EnvironVars.CloudBucket,
                Key = EnvironVars.CloudPath,
            };

            var getMetadataRes = await s3.GetObjectMetadataAsync(metadataReq);

            if (getMetadataRes.LastModified < finf.LastWriteTime)
            {
                Console.WriteLine("Skipping AWS S3 download - file was last modified...");
                Console.WriteLine($"--> LOCAL: {finf.LastWriteTime}");
                Console.WriteLine($"--> CLOUD: {getMetadataRes.LastModified}");
                return;
            }

            var objReq = new Amazon.S3.Model.GetObjectRequest()
            {
                BucketName = EnvironVars.CloudBucket,
                Key = EnvironVars.CloudPath,
            };

            Console.WriteLine("Requesting S3 object...");
            var getObjRes = await s3.GetObjectAsync(objReq);
            Console.WriteLine("Got S3 object response");
            File.Delete(EnvironVars.DatastoreLocalPath);
            Console.WriteLine("Deleted local datastore file");
            using FileStream file = File.Create(EnvironVars.DatastoreLocalPath);
            using var stream = getObjRes.ResponseStream;
            await stream.CopyToAsync(file);
            Console.WriteLine("Copied from AWS to local file.");
            //await getObjRes.WriteResponseStreamToFileAsync(EnvironVars.DatastoreLocalPath, false, default);
        }

        private static async Task SaveToCloud()
        {
            while (LOCKACQUIRED) { }
            LOCKACQUIRED = true;

            try
            {
                var uploadReq = new TransferUtilityUploadRequest()
                {
                    BucketName = EnvironVars.CloudBucket,
                    Key = EnvironVars.CloudPath,
                    FilePath = EnvironVars.DatastoreLocalPath
                };

                await uploadUtil.UploadAsync(uploadReq);
                Console.WriteLine("Uploaded to AWS!");
            }
            finally { LOCKACQUIRED = false; }
        }

        private static async void UploadThread()
        {
            while (true)
            {
                await Task.Delay(cloudUploadDelay);

                await SaveToCloud();
            }
        }
    }
}
