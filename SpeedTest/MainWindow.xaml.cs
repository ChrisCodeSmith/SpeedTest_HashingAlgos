using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SpeedTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string path = System.IO.Path.GetTempPath() + "\\FileComp";

        private List<Guid> fileList = new List<Guid>();

        private readonly object syncObj = new object();



        public MainWindow()
        {
            InitializeComponent();
            
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Directory.Delete(path, true);
        }

        private void CreateFilesButton(object sender, RoutedEventArgs e)
        {
            fileList.Clear();
            //Make dir if not exist
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }


            // Clear all files everytime we press 'Create Files'
            DirectoryInfo di = new DirectoryInfo(path);
            foreach (var file in di.EnumerateFiles())
            {
                file.Delete();           
            }
            di = new DirectoryInfo(path);
            Debug.WriteLine(di.EnumerateFiles().Count());


            int numerberOfFiles = int.Parse(NumberOfFiles.Text);
            int sizeOfFiles = int.Parse(SizeOfFiles.Text);

            
            for (int i = 0; i < numerberOfFiles; i++) { 
                Guid tmp = Guid.NewGuid();
                fileList.Add(tmp);
            }

            Random random = new Random();
            foreach (Guid file in fileList)
            {
                byte[] b = new byte[sizeOfFiles];
                random.NextBytes(b);
                File.WriteAllBytes(path + "/" + file, b);
            }
            StatusBar.Content = "Files written in Dir: " + path;
            StatusBar.Foreground = Brushes.Green;

        }

        private void RunButton(object sender, RoutedEventArgs e)
        {
            lock (syncObj)
            {
                string[] origFiles = Directory.GetFiles(path);
                uint markusCollisions = 0;
                uint md5Collsisions = 0;
                uint sha256Collisions = 0;
                StatusBar.Content = "Make copies of files";
                StatusBar.Foreground = Brushes.Black;
                foreach (string file in origFiles)
                {
                    File.Copy(file, file + ".copy", true);
                    // Uncomment following 3 lines to test if the comparisons generate collisions if files are different
                    //StreamWriter sw = File.AppendText(file);
                    //sw.WriteLine("ffs");
                    //sw.Close();
                }

                // -----------------------------------------------------------------------
                // SHA256
                StatusBar.Content = "Start SHA256 Calc";
                StatusBar.Foreground = Brushes.Black;
                var timer = Stopwatch.StartNew();
                
                foreach (string origFile in origFiles)
                {
                    if (!SHA256Calc(origFile, origFile + ".copy"))
                    {
                        StatusBar.Content = "SHA256 Hash missmatch";
                        StatusBar.Foreground = Brushes.Red;
                        //return;
                    }
                    else sha256Collisions++;
                }
                timer.Stop();
                TimeSHA256.Text = timer.Elapsed.ToString(@"m\:ss\.fff");
                CollisionsSHA.Text = sha256Collisions.ToString();

                // -----------------------------------------------------------------------
                // MD5
                StatusBar.Content = "Start MD5 Calc";
                StatusBar.Foreground = Brushes.Black;
                timer.Restart();
                foreach (string origFile in origFiles)
                {
                    if (!MD5Calc(origFile, origFile + ".copy"))
                    {
                        StatusBar.Content = "MD5 Hash missmatch";
                        StatusBar.Foreground = Brushes.Red;
                        //return;
                    }
                    else md5Collsisions++;
                }
                timer.Stop();
                TimeMD5.Text = timer.Elapsed.ToString(@"m\:ss\.fff");
                CollisionsMD5.Text = md5Collsisions.ToString();


                // -----------------------------------------------------------------------
                // Markus' Algo
                StatusBar.Content = "Start MD5 Calc";
                StatusBar.Foreground = Brushes.Black;
                timer.Restart();
                foreach (string origFile in origFiles)
                {
                    if (!FileCompareMarkus(origFile, origFile + ".copy"))
                    {
                        StatusBar.Content = "Markus File compare missmatch or error";
                        StatusBar.Foreground = Brushes.Red;
                        //return;
                    }
                    else markusCollisions++;

                }
                timer.Stop();
                TimeMarkus.Text = timer.Elapsed.ToString(@"m\:ss\.fff");
                CollisionsMarkus.Text = markusCollisions.ToString();

                

                // -----------------------------------------------------------------------

                StatusBar.Content = "All Hash comparisons successfull";
                StatusBar.Foreground = Brushes.Green;
            }
        }

        private static bool MD5Calc(string origFile, string copyFile) {
            MD5 md5Hash = MD5.Create();
            return HashCalc(md5Hash, origFile, copyFile);
        }
        private static bool SHA256Calc(string origFile, string copyFile) {
            SHA256 sHA256 = SHA256.Create();
            return HashCalc(sHA256, origFile, copyFile);
        }
        private static bool HashCalc(HashAlgorithm hashAlgo, string origFile, string copyFile)
        {
            using (hashAlgo)
            {
                string origFileHash = GetHash(hashAlgo, File.ReadAllBytes(origFile));
                string copyFileHash = GetHash(hashAlgo, File.ReadAllBytes(copyFile));
                //Debug.WriteLine(hashAlgo.ToString() + ":::" + origFileHash + " == " + copyFileHash);
                StringComparer comparer = StringComparer.OrdinalIgnoreCase;
                return comparer.Compare(origFileHash, copyFileHash) == 0;
            }
        }

        private static string GetHash(HashAlgorithm hashAlgorithm, byte[] input) {
            byte[] hashOrig = hashAlgorithm.ComputeHash(input);
            var sBuilder = new StringBuilder();
            for (int i = 0; i != hashOrig.Length; i++)
            {
                sBuilder.Append(hashOrig[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        // Markus' FileCompare Method

        // This method accepts two strings the represent two files to
        // compare. A return value of 0 indicates that the contents of the files
        // are the same. A return value of any other value indicates that the
        // files are not the same.
        private static bool FileCompareMarkus(string file1, string file2)
        {
            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            // Determine if the same file was referenced two times.
            if (file1 == file2)
                // Return true to indicate the files are the same.
                return true;

            // Open the two files.
            fs1 = new FileStream(file1, FileMode.Open);
            fs2 = new FileStream(file2 , FileMode.Open);
            // Check the file sizes. If they are not the same, the files are
            // not the same.
            if(fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();
                // Return false to indicate files are different
                return false;
            }
            do
            {
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            fs1.Close();
            fs2.Close();

            return ((file1byte - file2byte) == 0);
        }
    }
}