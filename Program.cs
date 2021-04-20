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
        //init dict for ParceMode, whose we input at start program
        static Dictionary<string, string> ParceModeDict = new Dictionary<string, string>
        {
            {"1", "xml"},
            {"2", "json"},
            {"",  "json" }
        };
        //init dict for parceXML stream, this dict have main path to atributes all child
        static Dictionary<string, string> PathForXML = new Dictionary<string, string>
        {
            {"temp", "temperature"},
            {"humidity", "humidity"},
            {"sunrise", "city/sun"},
            {"sunset", "city/sun"},
        };
        //init answer dict for collect data to show and write in file
        static Dictionary<string, string> data = new Dictionary<string, string>
        {
            {"temp", ""},
            {"humidity", ""},
            {"sunrise", ""},
            {"sunset", ""},
        };
        //init out APIKEY
        const string API_KEY = "5718d8292e6a519de25c7c22b8a64939";
        const string HTTPLINK = @"https://api.openweathermap.org/data/2.5/weather?q=";

        static XmlDocument doc = null;
        static string ParceMode = null;
        static string inTownName;
        static string answer;

        //init Path to write files
        static string PathToWrite = @"E:\Test\"; //change path to save directory

        static void Main(string[] args)
        {   
            //main input Town Name
            Console.Write("Input here town name: ");
            inTownName = Console.ReadLine();
            //choose parse mode
            Console.Write("\nChoose selection mode \n\t1 - for XML parse\n\t2 - for JSON parse: ");
            ParceMode = Console.ReadLine();
            if (ParceMode==null)
            {
                Console.Write("Wrong input, set default parse mode: XML");
                ParceMode = "1";
            }
            try
            {
                //main logic func
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
            //api.openweathermap.org/data/2.5/weather?q={city name}&appid={API key} - example api request
            WebRequest request = WebRequest.Create(HTTPLINK + inTownName + "&APPID=" + API_KEY + "&mode=" + ParceModeDict[ParceMode]);
            request.Method = "POST"; //metod get data
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            //set parce mode
            switch (ParceModeDict[ParceMode])
            {
                case "xml":
                    {
                        doc = new XmlDocument();
                        doc.Load(response.GetResponseStream());

                        foreach (var current in PathForXML)
                        {
                            int LenOfStr = current.Value.Split('/').Length;
                            //func to select xml node tree child and his attributes values into data dict
                            SelectedNodeText(doc.DocumentElement, current.Value, LenOfStr, LenOfStr, current.Key);
                        }
                        //================= any data conversions ===============
                        data["temp"] = GetCelciusValue(data["temp"], temp.kel)+" C";
                        data["humidity"] = data["humidity"] + " %";
                        //console and file output
                        WriteToFile();
                        break;
                    }
                case "json":
                    {
                        answer = reader.ReadToEnd();
                        dynamic jsondata = JsonConvert.DeserializeObject(answer);

                        //================= any data conversions ===============
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
                        //console and file output
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
                //set filename as current datatime 
                DateTime now = DateTime.Now;
                fstream = new FileStream(PathToWrite + now.ToString("dd.MM.yyyy") +"_"+ now.ToString("HH.mm.ss") + ".txt", FileMode.OpenOrCreate);
                StreamWriter w = new StreamWriter(fstream);
                foreach (var current in data)
                {
                    //console and file output here
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
                //here we search for path needed child
                if (i.Name == xp.Split('/')[depth - num])
                {
                    if (num == 1)
                    {
                        //get child attribute into data dict
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

        //=================some convert and data conversions funct ===============
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
