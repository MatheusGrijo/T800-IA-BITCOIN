using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;
using Newtonsoft.Json;
using Microsoft.ML.Transforms.TimeSeries;
public class ML
{



    public class OHCL
    {
        public DateTime CurrentDate { get; set; }
        public float PreviousOpen { get; set; }
        public float PreviousHigh { get; set; }
        public float PreviousLow { get; set; }
        public float NextClose { get; set; }
        public float PreviousClose { get; set; }
        public float PreviousVolume { get; set; }
        public float PreviousQuoeteAssetVolume { get; set; }
        public float PreviousTrades { get; set; }
    }

    public class OHCLPrediction
    {
        [ColumnName("Score")]
        public float NextClose { get; set; }
    }

    static OHCL[] loadData(DateTime max)
    {
        if (max == null)
            DateTime.Now.AddDays(1);

        String jsonAsText = System.IO.File.ReadAllText(@"Z:\temp\BTCUSDT.txt");
        Newtonsoft.Json.Linq.JContainer json = (Newtonsoft.Json.Linq.JContainer)JsonConvert.DeserializeObject(jsonAsText);

        List<OHCL> array = new List<OHCL>();
        foreach (var item in json)
        {
            try
            {
                OHCL ohcl = new OHCL();
                ohcl.CurrentDate = UnixTimeStampToDateTime(float.Parse(item[0].ToString().Replace(".", ",").Substring(0,10)   ));
                if (ohcl.CurrentDate > max)
                    break;
                ohcl.PreviousOpen = float.Parse(item.Previous[1].ToString().Replace(".", ","));
                ohcl.PreviousHigh = float.Parse(item.Previous[2].ToString().Replace(".", ","));
                ohcl.PreviousLow = float.Parse(item.Previous[3].ToString().Replace(".", ","));
                ohcl.NextClose = float.Parse(item[4].ToString().Replace(".", ","));
                ohcl.PreviousClose = float.Parse(item.Previous[4].ToString().Replace(".", ","));
                ohcl.PreviousVolume = float.Parse(item.Previous[5].ToString().Replace(".", ","));
                ohcl.PreviousQuoeteAssetVolume = float.Parse(item.Previous[7].ToString().Replace(".", ","));
                ohcl.PreviousTrades = int.Parse(item.Previous[8].ToString().Replace(".", ","));
                array.Add(ohcl);
            }
            catch
            {

            }
        }

        return array.ToArray();
    }

    static OHCL getOHCL(DateTime datetime, OHCL[] array)
    {
        foreach (var item in array)
        {
            if (item.CurrentDate.ToString("yyyyMMdd") == datetime.ToString("yyyyMMdd"))
                return item;
        }

        return null;
    }

    public static void start()
    {
        String lines = "";
        try
        {
            

            OHCL[] list = loadData(DateTime.Now);
            String l = "";
            foreach (var item in list)
            {
                l += item.CurrentDate.ToString("yyyyMMdd") + ";" + item.NextClose + Environment.NewLine;
            }

            DateTime dateBegin = DateTime.Parse("2019-10-01");
            String longOrShort = "null";

            while (dateBegin < DateTime.Now.AddDays(-1))
            {
                //Import context 
                MLContext mlContext = new MLContext();

                // 1. Import or create training data
                OHCL[] ohclData = loadData(dateBegin);
                IDataView trainingData = mlContext.Data.LoadFromEnumerable(ohclData);

                // 2. Specify data preparation and model training pipeline
                var pipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: "NextClose")
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "CurrentDateE", inputColumnName: "CurrentDate"))
                    .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(OHCL.PreviousClose)))
                    .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(OHCL.PreviousVolume)))
                    .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(OHCL.PreviousTrades)))
                    .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(OHCL.PreviousQuoeteAssetVolume)))
                    .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(OHCL.PreviousOpen)))
                    .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(OHCL.PreviousHigh)))
                    .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(OHCL.PreviousLow)))
                    .Append(mlContext.Transforms.Concatenate("Features", "CurrentDateE", nameof(OHCL.PreviousClose), nameof(OHCL.PreviousVolume), nameof(OHCL.PreviousTrades), nameof(OHCL.PreviousQuoeteAssetVolume), nameof(OHCL.PreviousOpen), nameof(OHCL.PreviousHigh), nameof(OHCL.PreviousLow)))
                    .Append(mlContext.Regression.Trainers.OnlineGradientDescent())
                    ;


                // STEP 3: Set the training algorithm, then create and config the modelBuilder - Selected Trainer (SDCA Regression algorithm)                            
                var trainer = mlContext.Regression.Trainers.Sdca(labelColumnName: "Label", featureColumnName: "Features");
                var trainingPipeline = pipeline.Append(trainer);

                // 3. Train model
                Console.WriteLine(dateBegin + " - Train...");
                var model = trainingPipeline.Fit(trainingData);
                Console.WriteLine("OK!");

                OHCL last = getOHCL(dateBegin, loadData(DateTime.Now));

                OHCL prediction = new OHCL();
                prediction.CurrentDate = dateBegin;
                prediction.PreviousClose = last.PreviousClose;
                prediction.PreviousVolume = last.PreviousVolume;
                prediction.PreviousTrades = last.PreviousTrades;
                prediction.PreviousQuoeteAssetVolume = last.PreviousQuoeteAssetVolume;
                prediction.PreviousOpen = last.PreviousOpen;
                prediction.PreviousHigh = last.PreviousHigh;
                prediction.PreviousLow = last.PreviousLow;
                prediction.NextClose = 0;
                var aux = mlContext.Model.CreatePredictionEngine<OHCL, OHCLPrediction>(model).Predict(prediction);
                //Console.WriteLine(aux.NextClose);



                if (last.PreviousClose < aux.NextClose)
                    longOrShort = "'L'";
                if (last.PreviousClose > aux.NextClose)
                    longOrShort = "'S'";

                lines += "['" +  dateBegin.ToString("yyyy-MM-dd") + "'," + last.NextClose.ToString().Replace(",",".") + "," + aux.NextClose.ToString().Replace(",", ".") + ","+longOrShort+"],";

            

                dateBegin = dateBegin.AddDays(1);
            }



        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + ex.StackTrace);
        }

        Console.WriteLine("press enter to exit...");
        Console.ReadLine();
        Environment.Exit(0);
    }

    public static DateTime UnixTimeStampToDateTime(float unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dtDateTime;
    }


}
