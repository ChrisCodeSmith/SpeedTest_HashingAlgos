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

namespace SpeedTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string path = System.IO.Path.GetTempPath() + "\\FileComp";

        private List<Guid> fileList = new List<Guid>();



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
            string[] origFiles = Directory.GetFiles(path);
            uint md5Collsisions = 0;
            uint sha256Collisions = 0;
            StatusBar.Content = "Make copies of files";
            StatusBar.Foreground = Brushes.Black;
            foreach (string file in origFiles) { 
                File.Copy(file, file + ".copy", true);
            }
            //Debug.WriteLine(Directory.GetFiles(path).Length);

            // MD5
            StatusBar.Content = "Start MD5 Calc";
            StatusBar.Foreground = Brushes.Black;
            var timer = new Stopwatch();
            timer.Start();
            foreach (string origFile in origFiles) {
                if (!MD5Calc(origFile, origFile + ".copy"))
                {
                    StatusBar.Content = "Hash missmatch";
                    return;
                }
                else md5Collsisions++;
            }
            timer.Stop();
            TimeMD5.Text = timer.Elapsed.ToString(@"m\:ss\.fff");
            CollisionsMD5.Text = md5Collsisions.ToString();

            // SHA256
            StatusBar.Content = "Start SHA256 Calc";
            StatusBar.Foreground = Brushes.Black;
            timer.Restart();
            foreach (string origFile in origFiles)
            {
                if(!SHA256Calc(origFile, origFile + ".copy"))
                {
                    StatusBar.Content = "Hash missmatch";
                    StatusBar.Foreground = Brushes.Red;
                    return;
                }
                else sha256Collisions++;
            }
            timer.Stop();
            TimeSHA256.Text = timer.Elapsed.ToString(@"m\:ss\.fff");
            CollisionsSHA.Text = sha256Collisions.ToString();

            StatusBar.Content = "All Hash comparisons successfull";
            StatusBar.Foreground = Brushes.Green;
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
    }
}