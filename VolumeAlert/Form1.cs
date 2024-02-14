using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Xml;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Linq;

namespace VolumeAlert
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void LogWpis(string text)
        {
            richTextBox1.BeginInvoke(new Action(() =>
            {
                richTextBox1.Text += text.ToString() + Environment.NewLine;
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
            }));
        }

        private void AlertWpis(string text)
        {
            richTextBox2.BeginInvoke(new Action(() =>
            {
                richTextBox2.Text += text.ToString() + Environment.NewLine;
                richTextBox2.SelectionStart = richTextBox2.Text.Length;
                richTextBox2.ScrollToCaret();
            }));
        }

        public static MongoClientSettings mongoClientSettings = new MongoClientSettings
        {
            Server = new MongoServerAddress("127.0.0.1", 27017),
            MaxConnectionPoolSize = 100000
        };

        public static MongoClient dbClient = new MongoClient(mongoClientSettings);

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    textBox_Sciezka.Text = openFileDialog1.FileName;
                }
                catch (Exception ex)
                {
                    LogWpis(ex.ToString());
                }
            }
        }

        async Task WyslijPolecenieDoAPI(string Polecenie)
        {
            using (var client = new WebClient())
            {
                // Log(Polecenie);
                await client.DownloadStringTaskAsync(Polecenie);
                Thread.Sleep(2);
            }
        }

        public string SendHttpRequest(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = client.GetAsync(url).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        return response.Content.ReadAsStringAsync().Result;
                    }
                    else
                    {
                        LogWpis(url);
                        throw new Exception("Błąd podczas pobierania danych. Kod odpowiedzi: " + response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                LogWpis(ex.ToString());
                return null;
            }
        }

        public Level1Data ParseLevel1Data(string xmlData)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlData);

                XmlNode level1DataNode = xmlDoc.SelectSingleNode("/Response/Content/Level1Data");

                Level1Data level1Data = new Level1Data();

                level1Data.Message = level1DataNode.Attributes["Message"].Value;
                level1Data.MarketTime = level1DataNode.Attributes["MarketTime"].Value;
                level1Data.Symbol = level1DataNode.Attributes["Symbol"].Value;
                level1Data.BidPrice = double.Parse(level1DataNode.Attributes["BidPrice"].Value.Replace('.', ','));
                level1Data.AskPrice = double.Parse(level1DataNode.Attributes["AskPrice"].Value.Replace('.', ','));
                level1Data.BidSize = int.Parse(level1DataNode.Attributes["BidSize"].Value);
                level1Data.AskSize = int.Parse(level1DataNode.Attributes["AskSize"].Value);
                level1Data.Volume = int.Parse(level1DataNode.Attributes["Volume"].Value);
                level1Data.MinPrice = double.Parse(level1DataNode.Attributes["MinPrice"].Value.Replace('.', ','));
                level1Data.MaxPrice = double.Parse(level1DataNode.Attributes["MaxPrice"].Value.Replace('.', ','));
                level1Data.LowPrice = double.Parse(level1DataNode.Attributes["LowPrice"].Value.Replace('.', ','));
                level1Data.HighPrice = double.Parse(level1DataNode.Attributes["HighPrice"].Value.Replace('.', ','));
                level1Data.FirstPrice = double.Parse(level1DataNode.Attributes["FirstPrice"].Value.Replace('.', ','));
                level1Data.OpenPrice = double.Parse(level1DataNode.Attributes["OpenPrice"].Value.Replace('.', ','));
                level1Data.ClosePrice = double.Parse(level1DataNode.Attributes["ClosePrice"].Value.Replace('.', ','));
                level1Data.MaxPermittedPrice = double.Parse(level1DataNode.Attributes["MaxPermittedPrice"].Value.Replace('.', ','));
                level1Data.MinPermittedPrice = double.Parse(level1DataNode.Attributes["MinPermittedPrice"].Value.Replace('.', ','));
                level1Data.LotSize = int.Parse(level1DataNode.Attributes["LotSize"].Value);
                level1Data.LastPrice = double.Parse(level1DataNode.Attributes["LastPrice"].Value.Replace('.', ','));
                level1Data.InstrumentState = level1DataNode.Attributes["InstrumentState"].Value;
                level1Data.AssetClass = level1DataNode.Attributes["AssetClass"].Value;
                level1Data.TickValue = double.Parse(level1DataNode.Attributes["TickValue"].Value.Replace('.', ','));
                level1Data.TickSize = double.Parse(level1DataNode.Attributes["TickSize"].Value.Replace('.', ','));
                level1Data.Currency = level1DataNode.Attributes["Currency"].Value;
                level1Data.Tick = level1DataNode.Attributes["Tick"].Value;
                level1Data.TAP = double.Parse(level1DataNode.Attributes["TAP"].Value.Replace('.', ','));
                level1Data.TAV = int.Parse(level1DataNode.Attributes["TAV"].Value);
                level1Data.TAT = level1DataNode.Attributes["TAT"].Value;
                level1Data.SSR = level1DataNode.Attributes["SSR"].Value;

                return level1Data;
            }
            catch (Exception ex)
            {
                LogWpis(ex.ToString());
                return null;
            }
        }

        static bool IsUdpPortAvailable(int port)
        {
            try
            {
                using (var udpClient = new UdpClient(port))
                {
                    return true;
                }
            }
            catch (SocketException)
            {
                return false;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (backgroundWorker_rejestruj_TOS.IsBusy == false)
            {
                backgroundWorker_rejestruj_TOS.RunWorkerAsync();
            }
        }

        async Task ListenToPortAsync(int port)
        {
            using (var udpClient = new UdpClient(port))
            {
                while (true)
                {
                    try
                    {
                        // Odbieranie danych
                        var result = await udpClient.ReceiveAsync();
                        udpClient.Client.ReceiveBufferSize = 4194304;//1310720;
                        string data = Encoding.ASCII.GetString(result.Buffer);

                        // Przetwarzanie danych i zapis do bazy danych
                        await ProcessDataAsync(data);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                        LogWpis(ex.ToString());
                        continue;
                    }
                }
            }
        }

        async Task ProcessDataAsync(string data)
        {
            try
            {
                Dictionary<string, string> parsedData = ParseData(data);

                string message;
                if (!parsedData.TryGetValue("Message", out message))
                {
                    return;
                }

                switch (message)
                {
                    case "TOS":
                        await ProcessTOS(parsedData);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                LogWpis(ex.ToString());
            }
        }

        private Dictionary<string, string> ParseData(string data)
        {
            string[] splitData = data.Split(',', '=');
            Dictionary<string, string> parsedData = new Dictionary<string, string>();

            for (int i = 0; i < splitData.Length - 1; i += 2)
            {
                parsedData[splitData[i]] = splitData[i + 1];
            }

            return parsedData;
        }

        private async Task ProcessTOS(Dictionary<string, string> data)
        {
            if (data["Type"] == "0")
            {
                var document = new Feed_TOS
                {
                    localTime = Convert.ToDateTime(data["LocalTime"]),
                    marketTime = Convert.ToDateTime(data["MarketTime"]),
                    message = data["Message"],
                    symbol = data["Symbol"],
                    type = data["Type"],
                    price = double.Parse(data["Price"].Replace('.', ',')),
                    size = Convert.ToInt32(data["Size"]),
                    source = data["Source"],
                    condition = data["Condition"],
                    tick = data["Tick"],
                    mmid = data["Mmid"],
                    sub_market_id = data["SubMarketId"]
                };

                var db = dbClient.GetDatabase("TOS");
                var collection_TOS = db.GetCollection<Feed_TOS>(document.symbol);
                await collection_TOS.InsertOneAsync(document);
            }
        }

        private async void backgroundWorker_rejestruj_TOS_DoWork(object sender, DoWorkEventArgs e)
        {

            int basePort = 3000;
            List<int> listaPortow = new List<int>();
            int LicznikSymboli = 0;
            int licznik = 1;
            int Port = 0;
            int IleSymboliNaPort = 40;

            foreach (var Symbol in File.ReadLines(textBox_Sciezka.Text))
            {
                try
                {
                    if (Port != basePort)
                    {
                        listaPortow.Add(basePort);
                    }

                    Port = basePort;

                    while (!IsUdpPortAvailable(Port))
                    {
                        Port++;
                    }

                    //-------------------------------------------------------
                    LogWpis(licznik.ToString() + ": TOS dla " + Symbol + " na porcie " + Port.ToString());

                    string rejestracjaTOS = "http://localhost:8081/Register?symbol=" + Symbol + "&feedtype=TOS";

                    await WyslijPolecenieDoAPI(rejestracjaTOS);

                    //Tworzenie komendy skierowana danych z TOS na port
                    string TOS_ON = "http://localhost:8081/SetOutput?symbol=" + Symbol + "&feedtype=TOS&output=" + Port + "&status=on";

                    await WyslijPolecenieDoAPI(TOS_ON);

                    licznik++;
                    LicznikSymboli++;

                    if (LicznikSymboli % IleSymboliNaPort == 1)
                    {
                        basePort = Port + 1;
                    }

                }
                catch (Exception ex2)
                {
                    LogWpis(ex2.ToString());
                    continue;
                }
            }

            foreach (var item in listaPortow)
            {
                try
                {
                    ThreadPool.QueueUserWorkItem(async a => await ListenToPortAsync(item));
                    LogWpis("Uruchomiono nasłuch na porcie: " + item);
                }
                catch (Exception ex3)
                {
                    LogWpis(ex3.ToString());
                    continue;
                }
            }

            Port++;

            while (!IsUdpPortAvailable(Port))
            {
                Port++;
            }
        }

        private async void backgroundWorker_VolYahoo_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                await dbClient.DropDatabaseAsync("VolumeInfo_db");

                var db = dbClient.GetDatabase("VolumeInfo_db");
                var collection = db.GetCollection<AvgVol>("VolumeInfo");
                string Data_String = DateTime.Now.ToString("dd-MM-yyyy");

                foreach (var Symbol in File.ReadLines(textBox_Sciezka.Text))
                {
                    try
                    {
                        string[] Tablica = Symbol.Split('.');
                        string SymbolYahoo = Tablica[0];
                        string apiUrl = $"https://finance.yahoo.com/quote/{SymbolYahoo}";

                        using (HttpClient client = new HttpClient())
                        {
                            try
                            {
                                HttpResponseMessage response = await client.GetAsync(apiUrl);

                                if (response.IsSuccessStatusCode)
                                {
                                    string htmlContent = await response.Content.ReadAsStringAsync();

                                    var htmlDocument = new HtmlAgilityPack.HtmlDocument();
                                    htmlDocument.LoadHtml(htmlContent);

                                    var avgVolumeNode = htmlDocument.DocumentNode.SelectSingleNode("//td[@data-test='AVERAGE_VOLUME_3MONTH-value']");

                                    if (avgVolumeNode != null)
                                    {
                                        string avgVolumeValue = avgVolumeNode.InnerText.Trim();
                                        LogWpis($"Średni wolumen dla {SymbolYahoo}: {avgVolumeValue}");

                                        long SrednoVol = Convert.ToInt64(avgVolumeValue.Replace(",", string.Empty));

                                        //zapis do bazy
                                        var document = new AvgVol
                                        {
                                            Data = DateTime.Now.Date,
                                            Data_String = Data_String,
                                            Symbol = Symbol,
                                            AvgVolume = SrednoVol
                                        };

                                        collection.InsertOne(document);

                                        LogWpis("Zapisano dane dla " + Symbol);
                                    }
                                    else
                                    {
                                        LogWpis(SymbolYahoo + ": Nie znaleziono informacji o średnim wolumenie.");
                                    }
                                }
                                else
                                {
                                    LogWpis($"Błąd podczas pobierania danych. Kod: {response.StatusCode}");
                                }
                            }
                            catch (Exception ex)
                            {
                                LogWpis($"Wystąpił błąd: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex2)
                    {
                        LogWpis("Błąd dla " + Symbol + ": " + ex2.ToString());
                        continue;
                    }
                }
                LogWpis("Zakończono zapis do bazy.");
            }
            catch (Exception ex)
            {
                LogWpis(ex.ToString());
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (textBox_Sciezka.Text != string.Empty)
            {
                if (backgroundWorker_VolYahoo.IsBusy == false)
                {
                    backgroundWorker_VolYahoo.RunWorkerAsync();
                }
            }
            else
            {
                MessageBox.Show("Nie podano scieżki do pliku.");
            }
        }

        private async void backgroundWorker_Skaner_DoWork(object sender, DoWorkEventArgs e)
        {
            var database = dbClient.GetDatabase("VolumeInfo_db");
            var collection = database.GetCollection<AvgVol>("VolumeInfo");

            var filter = Builders<AvgVol>.Filter.Empty;
            var result = collection.Find(filter).ToList();

            Dictionary<string, long> SymbolVol = new Dictionary<string, long>();

            AlertWpis("Ładuję średnie wolumeny do listy.");

            foreach (var item in result)
            {
                SymbolVol.Add(item.Symbol, item.AvgVolume);
            }

            var databaseTOS = dbClient.GetDatabase("TOS");
            var filterTOS = Builders<Feed_TOS>.Filter.Empty;

            AlertWpis("Uruchamiam skaner.");
            while (true)
            {
                //AlertWpis(DateTime.Now + ": Nowy cykl.");

                foreach (var Symbol in SymbolVol.Keys.ToList())
                {
                    var collectionTOS = databaseTOS.GetCollection<Feed_TOS>(Symbol);
                    var resultTOS = collectionTOS.Find(filterTOS).ToList();

                    if (resultTOS.Count > 0)
                    {
                        long sumOfSize = resultTOS.Sum(item => item.size);
                        double VolumeLimit = double.Parse(textBox_VolDzis.Text) * SymbolVol[Symbol];
                        double minVol = double.Parse(textBox_minVol.Text);

                        if (sumOfSize > VolumeLimit && sumOfSize > minVol)
                        {
                            int CountPrint = resultTOS.Count();

                            if (CountPrint >= Convert.ToInt32(textBox_ilePrintow.Text))
                            {
                                double roznica = 0;

                                string rejestracjaL1 = "http://localhost:8081/Register?symbol=" + Symbol + "&feedtype=L1";
                                await WyslijPolecenieDoAPI(rejestracjaL1);

                                Level1Data level1Data = new Level1Data();
                                string apiUrl = "http://localhost:8081/GetLv1?symbol=" + Symbol;
                                string xmlData = SendHttpRequest(apiUrl);
                                level1Data = ParseLevel1Data(xmlData);

                                string DerejestracjaL1 = "http://localhost:8081/Deregister?symbol=" + Symbol + "&feedtype=L1";
                                await WyslijPolecenieDoAPI(DerejestracjaL1);

                                if (level1Data != null)
                                {
                                    roznica = (level1Data.LastPrice / level1Data.ClosePrice) - 1;
                                    roznica = Math.Round(roznica * 100, 0);
                                }

                                if (roznica >= Convert.ToDouble(textBox_IleProcent.Text))
                                {
                                    AlertWpis("Alert! " + Symbol + ": UP  " + roznica.ToString() + "%, Vol dziś: " + sumOfSize.ToString() + ", średni Vol: " + SymbolVol[Symbol] + ", printów: " + CountPrint);

                                    SymbolVol.Remove(Symbol);

                                    if (checkBox_AutoTrading.Checked == true)
                                    {
                                        if (level1Data.AskPrice <= 3)
                                        {
                                            int sizePozycja = int.Parse(textBox_Size.Text);

                                            string zlecenie = "ARCA Buy ARCX Limit Far DAY";

                                            string Polecenie = "http://localhost:8081/ExecuteOrder?symbol=" + Symbol + "&ordername=" + zlecenie + "&shares=" + sizePozycja + "&priceadjust=0.01";
                                            await WyslijPolecenieDoAPI(Polecenie);

                                            AlertWpis("Auto trading ==> " + Polecenie);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (backgroundWorker_Skaner.IsBusy == false)
            {
                backgroundWorker_Skaner.RunWorkerAsync();
            }
        }
    }
}
