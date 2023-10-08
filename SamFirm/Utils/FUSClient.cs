using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SamFirm.Utils
{
    internal class FUSRequest : WebRequest
    {
        public static new HttpWebRequest Create(string requestUriString)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUriString);
            request.Headers["Cache-Control"] = "no-cache";
            request.UserAgent = "Kies2.0_FUS";
            request.Headers.Add("Authorization",
                $"FUS nonce=\"{FUSClient.Nonce}\", signature=\"{(FUSClient.NonceDecrypted.Length > 0 ? Auth.GetAuthorization(FUSClient.NonceDecrypted) : "")}\", nc=\"\", type=\"\", realm=\"\", newauth=\"1\"");
            request.CookieContainer = FUSClient.Cookies;
            return request;
        }
    }

    internal static class FUSClient
    {
        public static CookieContainer Cookies = new CookieContainer();
        public static string Nonce { get; set; } = string.Empty;
        public static string NonceDecrypted { get; set; } = string.Empty;

        public static int DownloadBinary(string path, string file, string saveTo)
        {
            long num = 0L;
            HttpWebRequest wr = FUSRequest.Create("http://cloud-neofussvr.samsungmobile.com/NF_DownloadBinaryForMass.do?file=" + path + file);
            wr.Method = "GET";
            wr.Timeout = 0x61a8;
            wr.ReadWriteTimeout = 0x61a8;
            using (HttpWebResponse response = (HttpWebResponse)wr.GetFUSResponse())
            {
                if (response == null)
                {
                    Console.WriteLine("Error DownloadBinary(): response is null.");
                    return 0x385;
                }
                if ((response.StatusCode != HttpStatusCode.OK) && (response.StatusCode != HttpStatusCode.PartialContent))
                {
                    Console.WriteLine("Error DownloadBinary(): " + ((int)response.StatusCode));
                }
                else
                {
                    long total = long.Parse(response.GetResponseHeader("content-length")) + num;
                    byte[] buffer = new byte[0x2000];
                    try
                    {
                        File.HandleEncryptedFile(response.GetResponseStream(), saveTo);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("Error DownloadBinary(): ");
                        Console.WriteLine(exception.ToString());
                        return -1;
                    }
                }
                return 0;
            }
        }

        public static int DownloadBinaryInform(string xml, out string xmlresponse) =>
            XMLFUSRequest("https://neofussvr.sslcs.cdngc.net/NF_DownloadBinaryInform.do", xml, out xmlresponse);

        public static int DownloadBinaryInit(string xml, out string xmlresponse) =>
            XMLFUSRequest("https://neofussvr.sslcs.cdngc.net/NF_DownloadBinaryInitForMass.do", xml, out xmlresponse);

        public static int GenerateNonce()
        {
            HttpWebRequest wr = FUSRequest.Create("https://neofussvr.sslcs.cdngc.net/NF_DownloadGenerateNonce.do");
            wr.Method = "POST";
            wr.ContentLength = 0L;
            using (HttpWebResponse response = (HttpWebResponse)wr.GetFUSResponse())
            {
                if (response == null)
                {
                    return 0x385;
                }
                return (int)response.StatusCode;
            }
        }

        public static void SetReconnect()
        {
            // TODO: Not implemented.
        }

        private static int XMLFUSRequest(string URL, string xml, out string xmlresponse)
        {
            xmlresponse = null;
            HttpWebRequest wr = FUSRequest.Create(URL);
            wr.CookieContainer = Cookies;
            wr.Method = "POST";
            byte[] bytes = Encoding.ASCII.GetBytes(Regex.Replace(xml, @"\r\n?|\n|\t", string.Empty));
            wr.ContentLength = bytes.Length;
            using (Stream stream = wr.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }
            using (HttpWebResponse response = (HttpWebResponse)wr.GetFUSResponse())
            {
                if (response == null)
                {
                    return 0x385;
                }
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    try
                    {
                        xmlresponse = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    }
                    catch (Exception)
                    {
                        return 900;
                    }
                }
                return (int)response.StatusCode;
            }
        }

        public static WebResponse GetFUSResponse(this WebRequest wr)
        {
            try
            {
                WebResponse response = wr.GetResponse();
                if (response.Headers.AllKeys.Contains("NONCE"))
                {
                    Nonce = response.Headers["NONCE"];
                    NonceDecrypted = Auth.DecryptNonce(Nonce);
                }
                return response;
            }
            catch (WebException exception)
            {
                Console.WriteLine("Error GetResponseFUS() -> " + exception.ToString());
                if (exception.Status == WebExceptionStatus.NameResolutionFailure)
                {
                    SetReconnect();
                }
                return exception.Response;
            }
        }
    }
}
