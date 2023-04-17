using System.Diagnostics;
using Tomlet;

namespace ModStats
{
    public class EnvironVars
    {
        private static EnvironVars vals = new();
        const string ENVARSPATH = "./envars.toml";
        static EnvironVars()
        {
            string envarsTxt;
            if (!File.Exists(ENVARSPATH))
            {
                envarsTxt = TomletMain.TomlStringFrom(vals);
                File.WriteAllText(ENVARSPATH, envarsTxt);
                Console.WriteLine("Empty config written to file. Fill it out.");
                //Environment.Exit(1);
            }

            envarsTxt = File.ReadAllText(ENVARSPATH);
            vals = TomletMain.To<EnvironVars>(envarsTxt);
        }

        private string _cloudBucket = "";
        private string _cloudPath = "";
        private string _cloudAccessKey = "";
        private string _cloudSecretKey = "";
        private string _datastorePath = "";
        private string _datastoreInternalsKey = "";
        private string _datastoreInternalsPass = "";

        // prioritize file vars, fallback to env vars

        public static string CloudBucket =>             !string.IsNullOrEmpty(vals._cloudBucket)            ? vals._cloudBucket             : Environment.GetEnvironmentVariable("AWS_BUCKET")                  ?? "";
        public static string CloudPath =>               !string.IsNullOrEmpty(vals._cloudPath)              ? vals._cloudPath               : Environment.GetEnvironmentVariable("AWS_FILE_PATH")               ?? "";
        public static string CloudAccessKey =>          !string.IsNullOrEmpty(vals._cloudAccessKey)         ? vals._cloudAccessKey          : Environment.GetEnvironmentVariable("AWS_ACCESS_KEY")              ?? "";
        public static string CloudSecretKey =>          !string.IsNullOrEmpty(vals._cloudSecretKey)         ? vals._cloudSecretKey          : Environment.GetEnvironmentVariable("AWS_SECRET_KEY")              ?? "";
        public static string DatastoreLocalPath =>      !string.IsNullOrEmpty(vals._datastorePath)          ? vals._datastorePath           : Environment.GetEnvironmentVariable("DATASTORE_LOCAL_FILE_PATH")   ?? "";
        public static string DatastoreInternalsKey =>   !string.IsNullOrEmpty(vals._datastoreInternalsKey)  ? vals._datastoreInternalsKey   : Environment.GetEnvironmentVariable("DATASTORE_INTERNALS_KEY")     ?? "";
        public static string DatastireInternalsPass =>  !string.IsNullOrEmpty(vals._datastoreInternalsPass) ? vals._datastoreInternalsPass  : Environment.GetEnvironmentVariable("DATASTORE_INTERNALS_PASS")    ?? "";
    }
}
