using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace CSBlBrowserInstaller
{
    public partial class Form1 : Form
    {
        public string latestXMLVersion;
        int latestRelease = -1;
        System.Xml.XmlDocument doc = null;
        VersionComboItem[] ci = null;
        public Form1()
        {
            InitializeComponent();
            //Computer\HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam InstallPath
            //Blockland Game ID: 250340
            //Need to check for file @ Steam\SteamApps\appmanifest_250340.acf
            //If not found, check libraryfolders.vdf, and check all directories for said file.

            // For now we'll just assume default location - the user can always set the location.
            doPrint(linkLabel1.Text + "\r\n");
            if(System.IO.File.Exists("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Blockland\\Blockland.exe"))
            {
                textBox2.Text = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Blockland";
                doPrint("Found Blockland.exe in default Steam directory...\r\n");
            } else
            {
                doPrint("Could not locate Blockland.exe, user needs to choose the location of it.\r\n");
            }
            queryLatestRelease();
            client.DownloadProgressChanged += new System.Net.DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_processDownload);
        }
        class VersionComboItem
        {
            public int ID { get; set; }
            public string Text { get; set; }
            public System.Xml.XmlNode node { get; set; }
        }
        public void queryLatestRelease()
        {
            latestXMLVersion = (new System.Net.WebClient().DownloadString("https://www.pahs.site/blb/versions.xml"));
            System.Xml.XmlReaderSettings set = new System.Xml.XmlReaderSettings();
            set.ConformanceLevel = System.Xml.ConformanceLevel.Fragment;
            System.Xml.XmlReader reader = System.Xml.XmlReader.Create(new System.IO.StringReader(latestXMLVersion), set);
            doc = new System.Xml.XmlDocument();
            

            doc.Load(reader);
            reader.Close();
            int count = doc.GetElementsByTagName("release").Count;
            
            doPrint("Found " + count + " release(s).\r\n");
            ci = new VersionComboItem[count];
            int i = 0;
            foreach (System.Xml.XmlNode node in doc.GetElementsByTagName("release"))
            {
                if (node.Attributes != null && node.Attributes["version"] != null)
                {
                    ci[i] = new VersionComboItem { ID = i, Text = "BL Browser Build #" + node.Attributes["version"].Value + " (Blockland " + node.SelectSingleNode(".//blocklandrelease").InnerText + ")", node = node};
                    doPrint("Found Blockland Browser release #" + node.Attributes["version"].Value + "\r\n");
                    Boolean isLatest = false;
                    if(Boolean.TryParse(node.Attributes["latest"] != null ? node.Attributes["latest"].Value:"", out isLatest) && isLatest)
                    {
                        latestRelease = i;
                    }
                    i++;

                }
            }
            comboBox1.DisplayMember = "Text";
            comboBox1.ValueMember = "ID";
            comboBox1.DataSource = ci;
            if (latestRelease == -1)
            {
                doPrint("Was unable to find latest release of Blockland Browser.\r\n");
            } else
            {
                selectRelease(latestRelease, true);
            }
        }
        public void selectRelease(int x, bool changeItem)
        {
            if(changeItem)
                comboBox1.SelectedItem = ci[x];
            System.Data.DataTable table = new System.Data.DataTable();
            table.Columns.Add("Install", typeof(bool)).ReadOnly = false;
            table.Columns.Add("Dir.");
            table.Columns.Add("File");
            table.Columns.Add("Hash");
            table.Columns.Add("Matches", typeof(bool)).ReadOnly = true;
            
            foreach (System.Xml.XmlNode node in ci[x].node.SelectNodes(".//file"))
            {
                System.Data.DataRow row = table.NewRow();
                row["File"] = node.SelectSingleNode(".//name").InnerText;
                row["Hash"] = node.SelectSingleNode(".//sha1").InnerText;
                row["Dir."] = node.SelectSingleNode(".//directory").InnerText;
                String dir = textBox2.Text + "\\" + node.SelectSingleNode(".//directory").InnerText + node.SelectSingleNode(".//name").InnerText;
                String hash = getFileHash(dir);
                //doPrint("Checking file " + dir + ", hash is " + hash + "\r\n");
                row["Matches"] = hash.Equals(node.SelectSingleNode(".//sha1").InnerText);
                row["Install"] = !(bool)row["Matches"];
                table.Rows.Add(row);
            }
            dataGridView1.DataSource = table;
            //dataGridView1.Rows[0].Cells[0].ReadOnly = true;
            dataGridView1.Refresh();
        }
        public void checkBlocklandExecutable()
        {
            if(System.IO.File.Exists(textBox2.Text + "\\Blockland.exe"))
            {
                String hash = getFileHash(textBox2.Text + "\\Blockland.exe");
                doPrint("\r\nNot fully implemented.\r\nBlockland.exe located, hash is " + hash + "\r\n");
            }
        }
        public String getFileHash(String file)
        {
            if(System.IO.File.Exists(file))
            {
                using (System.IO.FileStream stream = System.IO.File.OpenRead(file))
                {
                    using (System.Security.Cryptography.SHA1Managed sha = new System.Security.Cryptography.SHA1Managed())
                    {
                        byte[] checksum = sha.ComputeHash(stream);
                        return BitConverter.ToString(checksum).Replace("-", string.Empty);
                    }
                }
            }
            return "0";
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.pahs.site/blb/");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = textBox2.Text.Equals("") ? "c:\\" : (System.IO.Directory.Exists(textBox2.Text) ? textBox2.Text : "c:\\");
            openFileDialog1.Filter = "Blockland.exe|Blockland.exe";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.RestoreDirectory = true;
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = System.IO.Path.GetDirectoryName(openFileDialog1.FileName);
                selectRelease(comboBox1.SelectedIndex, false);
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectRelease(comboBox1.SelectedIndex, false);
        }
        System.Collections.Generic.List<String> downloadQueue;
        System.Net.WebClient client = new System.Net.WebClient();

        private void button2_Click(object sender, EventArgs e)
        {
            System.Data.DataTable table = (System.Data.DataTable)dataGridView1.DataSource;
            downloadQueue = new System.Collections.Generic.List<String>();
            int i = 0;
            int x = comboBox1.SelectedIndex;
            foreach (System.Data.DataRow row in table.Rows)
            {
                if((Boolean)row["Install"])
                {
                    String url = "https://www.pahs.site/blb/" + ci[x].node.SelectNodes(".//file")[i].SelectSingleNode(".//serverdirectory").InnerText + ci[x].node.SelectNodes(".//file")[i].SelectSingleNode(".//name").InnerText;
                    downloadQueue.Add(url + "\t" + row["File"] + "\t" + row["Dir."] + "\t" + i.ToString());
                    doPrint("Attempting to download file " + url + " \r\n");
                }
                i++;
            }
            currIdx = -1;
            client_doDownload();
            
        }
        String currfile = "N/A";
        int currIdx = -1;
        private void client_doDownload()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                doPrint("Current file count: " + downloadQueue.Count + "\r\n");

                if (downloadQueue.Count <= 0)
                {
                    selectRelease(comboBox1.SelectedIndex, false);
                    doPrint("Finished downloading all files");
                    return;
                }
                String queue = downloadQueue[0];
                String url = queue.Split('\t')[0];
                String file = queue.Split('\t')[1];
                String dir = queue.Split('\t')[2];
                currIdx = int.Parse(queue.Split('\t')[3]);
                doPrint("Downloading file " + dir + file + " from url " + url + "\r\n");
                downloadQueue.RemoveAt(0);
                if(!System.IO.Directory.Exists(textBox2.Text + "\\" + dir))
                {
                    System.IO.Directory.CreateDirectory(textBox2.Text + "\\" + dir);
                }
                if(file.Equals("Blockland.exe"))
                {
                    if(System.IO.File.Exists(textBox2.Text + "\\Blockland.exe"))
                    {
                        System.IO.FileAttributes att = System.IO.File.GetAttributes(textBox2.Text + "\\Blockland.exe");
                        if(att.HasFlag(System.IO.FileAttributes.ReadOnly))
                        {
                            att = att & ~System.IO.FileAttributes.ReadOnly;
                            System.IO.File.SetAttributes(textBox2.Text + "\\Blockland.exe", att);
                        }
                    }
                }
                currfile = file;

                System.Threading.Thread thread = new System.Threading.Thread(() =>
                {
                        client.DownloadFileAsync(new Uri(url), textBox2.Text + "\\" + dir + file);
                    
                });
                thread.Start();
            });
        }
        private void client_processDownload(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                this.Invoke((MethodInvoker)delegate
               {
                   doPrint("Error downloading file " + currfile + "\r\n" + e.Error + "\r\n");
               });
            }
            else
            {
                this.Invoke((MethodInvoker)delegate
               {
                   if(currfile.Equals("Blockland.exe"))
                   {
                       System.IO.FileAttributes att = System.IO.File.GetAttributes(textBox2.Text + "\\Blockland.exe");
                       att = att | System.IO.FileAttributes.ReadOnly;
                       System.IO.File.SetAttributes(textBox2.Text + "\\Blockland.exe", att);

                   }
                   System.Data.DataTable table = (System.Data.DataTable)dataGridView1.DataSource;
                   table.Rows[currIdx]["Install"] = false;
               });
                client_doDownload();
            }
        }
        private void client_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs args)
        {
            this.BeginInvoke((MethodInvoker)delegate
           {
               double bytesIn = double.Parse(args.BytesReceived.ToString());
               double total = double.Parse(args.TotalBytesToReceive.ToString());
               double per = bytesIn / total * 100;
               
               progressBar1.Value = int.Parse(Math.Truncate(per).ToString());
               label3.Text = "Downloading file " + currfile + "... " + progressBar1.Value.ToString() + "%";
           });
        }
        System.IO.FileStream log = System.IO.File.Open("output.log", System.IO.FileMode.Create);
        private void doPrint(String txt)
        {

            textBox1.AppendText(txt);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(txt);
            log.Write(bytes, 0, bytes.Length);
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
