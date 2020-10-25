
    public class ReturnDataArray
    {
        public double[] arrayPriceClose = null;
        public double[] arrayPriceHigh = null;
        public double[] arrayPriceLow = null;
        public double[] arrayPriceOpen = null;
        public double[] arrayVolume = null;
        public double[] arrayDate = null;
        public double[] arrayQuoteVolume = null;

    }
    public static ReturnDataArray getDataArray(string coin, string timeGraph)
    {
        int i = 0;
        try
        {

            ReturnDataArray returnDataArray = new ReturnDataArray();



            String jsonAsStringRSI = "";
            if (source == "CACHE")
            {
                DateTime begin = DateTime.Parse("2017-08-01");
                if (!System.IO.File.Exists(Program.location + @"\cache\" + coin + timeGraph + ".txt"))
                {
                    System.IO.StreamWriter w = new System.IO.StreamWriter(Program.location + @"\cache\" + coin + timeGraph + ".txt", true);
                    while (begin != DateTime.Parse("2020-10-10"))
                    {
                        jsonAsStringRSI = Http.get("https://api.binance.com/api/v1/klines?limit=1000&symbol=" + coin + "&interval=" + timeGraph + "&startTime=" + DatetimeToUnix(DateTime.Parse(begin.ToString("yyyy-MM-dd") + " 00:00:00")) + "&endTime=" + DatetimeToUnix(DateTime.Parse(begin.ToString("yyyy-MM-dd") + " 23:59:59")), false).Replace("[[", "[").Replace("]]", "]") + ",";
                        //jsonAsStringRSI += Http.get("https://api.binance.com/api/v1/klines?limit=1000&symbol=" + coin + "&interval=" + timeGraph + "&startTime=" + DatetimeToUnix(DateTime.Parse(begin.ToString("yyyy-MM-dd") + " 13:00:00")) + "&endTime=" + DatetimeToUnix(DateTime.Parse(begin.ToString("yyyy-MM-dd") + " 23:59:59")), false).Replace("[[", "[").Replace("]]", "]") + ",";

                        w.Write(jsonAsStringRSI);

                        begin = begin.AddDays(1);
                        System.Threading.Thread.Sleep(500);
                        Console.WriteLine(begin);
                    }
                    w.Close();
                    w.Dispose();
                    jsonAsStringRSI = "[" + System.IO.File.ReadAllText(Program.location + @"\cache\" + coin + timeGraph + ".txt") + "]";
                    jsonAsStringRSI = jsonAsStringRSI.Substring(0, jsonAsStringRSI.Length - 1);
                    System.IO.File.Delete(Program.location + @"\cache\" + coin + timeGraph + ".txt");

                    w = new System.IO.StreamWriter(Program.location + @"\cache\" + coin + timeGraph + ".txt", true);
                    w.Write(jsonAsStringRSI);
                    w.Close();
                    w.Dispose();

                }


                jsonAsStringRSI = System.IO.File.ReadAllText(Program.location + @"\cache\" + coin + timeGraph + ".txt");
            }
            else
                jsonAsStringRSI = Http.get("https://api.binance.com/api/v1/klines?symbol=" + coin + "&interval=" + timeGraph + "&limit=1000&startTime=" + DatetimeToUnix(DateTime.Parse(DateTime.UtcNow.AddDays(-50).ToString("yyyy-MM-dd") + " 02:00:00")), true);



            Newtonsoft.Json.Linq.JContainer jsonRSI = (Newtonsoft.Json.Linq.JContainer)JsonConvert.DeserializeObject(jsonAsStringRSI);

            i = 0;
            foreach (JContainer element in jsonRSI.Children())
                //if (UnixTimeStampToDateTime(double.Parse(element[6].ToString().Replace(".", ","))) > DateTime.Parse(DateTime.UtcNow.ToString("yyyy-MM-dd")))
                i++;

            returnDataArray.arrayPriceClose = new double[i];
            returnDataArray.arrayPriceHigh = new double[i];
            returnDataArray.arrayPriceLow = new double[i];
            returnDataArray.arrayPriceOpen = new double[i];
            returnDataArray.arrayVolume = new double[i];
            returnDataArray.arrayDate = new double[i];
            returnDataArray.arrayQuoteVolume = new double[i];

            i = 0;
            foreach (JContainer element in jsonRSI.Children())
            {
                //if (UnixTimeStampToDateTime(double.Parse(element[6].ToString().Replace(".", ","))) > DateTime.Parse(DateTime.UtcNow.ToString("yyyy-MM-dd")))
                {
                    returnDataArray.arrayPriceClose[i] = double.Parse(element[4].ToString().Replace(".", ","));
                    returnDataArray.arrayPriceHigh[i] = double.Parse(element[2].ToString().Replace(".", ","));
                    returnDataArray.arrayPriceLow[i] = double.Parse(element[3].ToString().Replace(".", ","));
                    returnDataArray.arrayPriceOpen[i] = double.Parse(element[1].ToString().Replace(".", ","));
                    returnDataArray.arrayVolume[i] = double.Parse(element[5].ToString().Replace(".", ","));
                    returnDataArray.arrayQuoteVolume[i] = double.Parse(element[7].ToString().Replace(".", ","));
                    returnDataArray.arrayDate[i] = double.Parse(element[6].ToString().Replace(".", ","));

                    i++;
                }
            }

            return returnDataArray;
        }
        catch (Exception ex)
        {
            return null;
        }

    }
