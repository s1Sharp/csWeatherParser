using System;
using System.Collections.Generic;
//

//using Newtonsoft.Json;
using System.IO;
using System.Net;

using System.Xml;
using Newtonsoft.Json;

namespace TestSolve
{ 
    class Program
    {

        static Dictionary<string, string> ParceModeDict = new Dictionary<string, string>
        {
            {"1", "xml"},
            {"2", "json"},
            {"",  "json" }
        };

        static Dictionary<string, string> PathForXML = new Dictionary<string, string>
        {
            {"temp", "temperature"},
            {"humidity", "humidity"},
            {"sunrise", "city/sun"},
            {"sunset", "city/sun"},
        };

        static Dictionary<string, string> data = new Dictionary<string, string>
        {
            {"temp", ""},
            {"humidity", ""},
            {"sunrise", ""},
            {"sunset", ""},
        };
        static XmlDocument doc = null;
        static string ParceMode = null;
        const string API_KEY = "5718d8292e6a519de25c7c22b8a64939";
        static string inTownName;
        static string answer;
        static string PathToWrite = @"E:\Test\"; //change path to save directory

        static void Main(string[] args)
        {   

            Console.Write("Input here town name: ");
            inTownName = Console.ReadLine();
            Console.Write("\nChose selection mode \n\t1 - for XML parce\n\t2 - for JSON parce: ");
            ParceMode = Console.ReadLine();
            if (ParceMode==null)
            {
                Console.Write("Wrong input, set default parce mode: XML");
                ParceMode = "1";
            }
            try
            {
                Connect();
                Console.WriteLine("\nSuccess request!");
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message + "\n Try againg");
            }
        }

        public static void Connect()
        {
                //api.openweathermap.org/data/2.5/weather?q={city name}&appid={API key}
                //api.openweathermap.org/data/2.5/weather?q={ city name}&appid={ API key}
            WebRequest request = WebRequest.Create("https://api.openweathermap.org/data/2.5/weather?q=" + inTownName + "&APPID=" + API_KEY + "&mode=" + ParceModeDict[ParceMode]);
            request.Method = "POST";
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());

            
            switch (ParceModeDict[ParceMode])
            {
                case "xml":
                    {
                        doc = new XmlDocument();
                        doc.Load(response.GetResponseStream());

                        foreach (var current in PathForXML)
                        {
                            int LenOfStr = current.Value.Split('/').Length;
                            SelectedNodeText(doc.DocumentElement, current.Value, LenOfStr, LenOfStr, current.Key);
                        }
                        data["temp"] = GetCelciusValue(data["temp"], temp.kel)+" C";
                        data["humidity"] = data["humidity"] + " %";
                        WriteToFile();
                        break;
                    }
                case "json":
                    {
                        answer = reader.ReadToEnd();
                        dynamic jsondata = JsonConvert.DeserializeObject(answer);
                        data["temp"]= jsondata.main.temp;
                        data["temp"] = GetCelciusValue(data["temp"],temp.kel) + " C";
                        data["humidity"] = jsondata.main.humidity; 
                        data["humidity"] = data["humidity"] + " %";
                        data["sunrise"] = jsondata.sys.sunrise;
                        DateTime ss = ParseUnixTimestamp(data["sunrise"]);
                        data["sunrise"] = ss.ToString("dd.MM.yyyy") + " : " + ss.ToString("HH.mm.ss");

                        data["sunset"] = jsondata.sys.sunset;
                        DateTime st = ParseUnixTimestamp(data["sunset"]);
                        data["sunset"] = st.ToString("dd.MM.yyyy") + " : " + st.ToString("HH.mm.ss"); 

                        WriteToFile();
                        break;
                    }
                default:
                    break;
            }

            response.Close();
        }



        public static void WriteToFile()
        {
            foreach (var c in data)
            {
                if (c.Value == null) throw new Exception("error write data, data is empty");
            }
            FileStream fstream = null;
            try
            {
                DateTime now = DateTime.Now;
                fstream = new FileStream(PathToWrite + now.ToString("dd.MM.yyyy") +"_"+ now.ToString("HH.mm.ss") + ".txt", FileMode.OpenOrCreate);
                StreamWriter w = new StreamWriter(fstream);
                foreach (var current in data)
                {
                    Console.Write(current.Key + " : " + current.Value + "\n");
                    w.Write(current.Key + " : " + current.Value + "\n");
                }
                w.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message + "\n Try againg");
            }
            finally
            { 
                if (fstream != null)
                    fstream.Close();
            }

           
        }

    
    public static void SelectedNodeText(XmlNode x, string xp, int depth, int num, string key)
        {
            //func to search node value 
            foreach (XmlNode i in x)
            {
                string v = String.Empty, s = String.Empty, r = String.Empty;
                foreach (XmlAttribute attr in i.Attributes) 
                { 
                    if (i.Attributes.GetNamedItem("value") != null)
                        v = i.Attributes["value"].Value;
                    if (i.Attributes.GetNamedItem("set") != null)
                        s = i.Attributes["set"].Value;
                    if (i.Attributes.GetNamedItem("rise") != null)
                        r = i.Attributes["rise"].Value;
                }   

                if (i.Name == xp.Split('/')[depth - num])
                {
                    if (num == 1)
                    {
                        switch (key)
                        {
                            case "temp":
                                {
                                    data[key] = Convert.ToString(v);
                                    break;
                                }
                            case "humidity":
                                {
                                    data[key] = Convert.ToString(v);
                                    break;
                                }
                            case "sunset":
                                {
                                    data[key] = Convert.ToString(s).Replace("T"," : ");
                                    break;
                                }
                            case "sunrise":
                                {
                                    data[key] = Convert.ToString(r).Replace("T", " : ");
                                    break;
                                }
                            default:
                                throw new Exception("Invalid key");
                        }
                        return;
                    }
                    else
                    {
                        num = num - 1;
                        SelectedNodeText(i, xp, depth, num, key);
                    }
                }
            }
            return;
        }


        enum temp
        {
            kel,far
        }
        static private string GetCelciusValue(double fahrenheitValue)
        {
            return Convert.ToString((fahrenheitValue - 32) * 5 / 9);
        }
        static private string GetCelciusValue(string fahrenheitValue,temp key)
        {
            switch (key)
            {
                case temp.kel:
                    {
                        double l = Convert.ToDouble(fahrenheitValue.Replace(".", ","));
                        return Convert.ToString(Math.Round((l - 273), 2, MidpointRounding.AwayFromZero));
                    }
                case temp.far:
                    {
                        double l = Convert.ToDouble(fahrenheitValue.Replace(".", ","));
                        return Convert.ToString(Math.Round(((l-32)*5/9),2,MidpointRounding.AwayFromZero));
                    }
                default:
                    throw new Exception("Invalid key");
            }
        }
        public static DateTime ParseUnixTimestamp(long timestamp)
        {
            return (new DateTime(1970, 1, 1)).AddSeconds(timestamp).ToLocalTime();
        }
        public static DateTime ParseUnixTimestamp(string timestamp)
        {
            return (new DateTime(1970, 1, 1)).AddSeconds(Convert.ToDouble(timestamp)).ToLocalTime();
        }
    }
    

}
