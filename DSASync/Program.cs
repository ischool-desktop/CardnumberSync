using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;
using Aspose.Cells;

namespace DSASync
{
    class Program
    {
        enum ExitCode : int
        {
            Success = 0,
            InsufficientArguments = 1,
            UnableToObtainAccessToken = 2
        }

        static void Main(string[] args)
        {
            // 檢查參數
            // 參數內容 ClientID, ClientSecret, DSNS Name, RefreshToken, Output File
            if (args.Length < 5)
            {
                StdOut("Argument specification: ClientID, ClientSecret, DSNS Name, RefreshToken, Output File");
                Exit(ExitCode.InsufficientArguments);
            }

            string clientID = args[0];
            string clientSecret = args[1];
            string dsnsName = args[2];
            string refreshToken = args[3];
            string outputFileName = args[4];

            // Auth 認證，取得 Access Token
            string accessToken = "";

            try
            {
                accessToken = ObtainAccessToken(clientID, clientSecret, refreshToken);
            }
            catch (Exception e)
            {
                StdOut("Uable to obtain access token: " + e.ToString());
                Exit(ExitCode.UnableToObtainAccessToken);
            }

            Console.WriteLine(accessToken);

            // 呼叫 DSA，取得學生清冊
            // 清冊內容包括: StudentID, StudentNumber, ClassName, SeatNo, Name, CardNumber
            string cardNoXmlString;
            #region 呼叫DSA
            {
                string urlString = $"https://dsns.ischool.com.tw/test.h.hwsh.tc.edu.tw/CardnumberSync/GetCardNo?stt=PassportAccessToken&AccessToken={accessToken}";

                // 準備 Http request
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(urlString);
                req.Method = "GET";
                req.Accept = "*/*";
                req.ContentType = "application/xml";
                req.ContentLength = 0;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;

                // 呼叫並取得結果
                HttpWebResponse rsp;
                rsp = (HttpWebResponse)req.GetResponse();
                Stream dataStream = rsp.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                cardNoXmlString = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                rsp.Close();

                //Console.WriteLine(cardNoXmlString);
            }
            #endregion

            // 解析 XML 並輸出 CSV
            new Aspose.Cells.License().SetLicense(new MemoryStream(Properties.Resources._2015_Aspose_Total));
            Workbook wb = new Workbook();
            wb.Worksheets.Clear();
            wb.Worksheets.Add();
            #region ParseXML
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(cardNoXmlString);


                int rowIndex = 0;
                int cellIndex = 0;
                wb.Worksheets[0].Cells[rowIndex, cellIndex++].PutValue("StudentID");
                wb.Worksheets[0].Cells[rowIndex, cellIndex++].PutValue("StudentNumber");
                wb.Worksheets[0].Cells[rowIndex, cellIndex++].PutValue("ClassName");
                wb.Worksheets[0].Cells[rowIndex, cellIndex++].PutValue("SeatNo");
                wb.Worksheets[0].Cells[rowIndex, cellIndex++].PutValue("Name");
                wb.Worksheets[0].Cells[rowIndex, cellIndex++].PutValue("CardNumber");

                foreach (XmlElement studentEle in doc.SelectNodes("Body/Student"))
                {
                    rowIndex++;
                    cellIndex = 0;

                    wb.Worksheets[0].Cells[rowIndex, cellIndex++].PutValue(studentEle.SelectSingleNode("StudentID").InnerText);
                    wb.Worksheets[0].Cells[rowIndex, cellIndex++].PutValue(studentEle.SelectSingleNode("StudentNumber").InnerText);
                    wb.Worksheets[0].Cells[rowIndex, cellIndex++].PutValue(studentEle.SelectSingleNode("ClassName").InnerText);
                    wb.Worksheets[0].Cells[rowIndex, cellIndex++].PutValue(studentEle.SelectSingleNode("SeatNo").InnerText);
                    wb.Worksheets[0].Cells[rowIndex, cellIndex++].PutValue(studentEle.SelectSingleNode("Name").InnerText);
                    wb.Worksheets[0].Cells[rowIndex, cellIndex++].PutValue(studentEle.SelectSingleNode("CardNo").InnerText);
                }

            }
            wb.Settings.Encoding = Encoding.UTF8;
            wb.Save(outputFileName, SaveFormat.CSV);

            #endregion

            Exit(ExitCode.Success);
        }

        static void StdOut(string output)
        {
            Console.WriteLine(output);
        }

        static void Exit(ExitCode returnCode)
        {
            Environment.Exit((int)returnCode);
        }

        static string ObtainAccessToken(string clientID, string clientSecret, string refreshToken)
        {
            const string AuthRequestBase = "https://auth.ischool.com.tw/oauth/token.php";
            const string AuthGrantType = "grant_type=refresh_token";

            string urlString = $"{AuthRequestBase}?{AuthGrantType}&client_id={clientID}&client_secret={clientSecret}&refresh_token={refreshToken}";

            // 準備 Http request
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(urlString);
            req.Method = "GET";
            req.Accept = "*/*";
            req.ContentType = "application/json";
            req.ContentLength = 0;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;

            // 呼叫並取得結果
            HttpWebResponse rsp;
            rsp = (HttpWebResponse)req.GetResponse();
            Stream dataStream = rsp.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string result = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            rsp.Close();

            Console.WriteLine(result);

            // 解析 JSON
            byte[] byteArray = Encoding.UTF8.GetBytes(result);
            MemoryStream authStream = new MemoryStream(byteArray);
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(AuthResult));
            authStream.Position = 0;
            AuthResult authResult = (AuthResult)ser.ReadObject(authStream);

            return authResult.accessToken;
        }

        // JSon Object Model for Auth Access Token
        [DataContract]
        class AuthResult
        {
            [DataMember(Name = "access_token")]
            public string accessToken { get; set; }
            [DataMember(Name = "expires_in")]
            public int expiresIn { get; set; }
            [DataMember(Name = "token_type")]
            public string tokenType { get; set; }
            [DataMember(Name = "scope")]
            public string scope { get; set; }
            [DataMember(Name = "static")]
            public bool isStatic { get; set; }
        }
    }


}