using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using CommandLine;

namespace SamFirm
{
    class Program
    {
        public class Options
        {
            [Option('m', "model", Required = true)]
            public string Model { get; set; }

            [Option('r', "region", Required = true)]
            public string Region { get; set; }

            [Option('i', "imei", Required = true)]
            public string imei { get; set; }
        }

        private static string GetLatestVersion(string region, string model)
        {
            using (WebClient client = new WebClient())
            {
                string XMLString = client.DownloadString("http://fota-cloud-dn.ospserver.net/firmware/" + region + "/" + model + "/version.xml");
                return XDocument.Parse(XMLString).XPathSelectElement("./versioninfo/firmware/version/latest").Value;
            }
        }

        static void Main(string[] args)
        {
            string model = "";
            string region = "";
            string imei = "";
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    model = o.Model;
                    region = o.Region;
                    imei = o.imei;
                });

            if (model.Length == 0 || region.Length == 0 || imei.Length == 0)
            {
                return;
            }

            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine($@"
  Model: {model}
  Region: {region}");

            string[] versions = GetLatestVersion(region, model).Split('/');
            string versionPDA = versions[0];
            string versionCSC = versions[1];
            string versionMODEM = versions[2];
            string version = $"{versionPDA}/{versionCSC}/{(versionMODEM.Length > 0 ? versionMODEM : versionPDA)}/{versionPDA}";

            Console.WriteLine($@"
  Latest version:
    PDA: {versionPDA}
    CSC: {versionCSC}
    MODEM: {(versionMODEM.Length > 0 ? versionMODEM : "N/A")}");

            int responseStatus;
            responseStatus = Utils.FUSClient.GenerateNonce();

            string binaryInfoXMLString;
            responseStatus = Utils.FUSClient.DownloadBinaryInform(
                Utils.Msg.GetBinaryInformMsg(version, region, model,imei, Utils.FUSClient.NonceDecrypted), out binaryInfoXMLString);

            XDocument binaryInfo = XDocument.Parse(binaryInfoXMLString);
            long binaryByteSize = long.Parse(binaryInfo.XPathSelectElement("./FUSMsg/FUSBody/Put/BINARY_BYTE_SIZE/Data").Value);
            string binaryDescription = binaryInfo.XPathSelectElement("./FUSMsg/FUSBody/Put/DESCRIPTION/Data").Value;
            string binaryFilename = binaryInfo.XPathSelectElement("./FUSMsg/FUSBody/Put/BINARY_NAME/Data").Value;
            string binaryLogicValue = binaryInfo.XPathSelectElement("./FUSMsg/FUSBody/Put/LOGIC_VALUE_FACTORY/Data").Value;
            string binaryModelPath = binaryInfo.XPathSelectElement("./FUSMsg/FUSBody/Put/MODEL_PATH/Data").Value;
            string binaryOSVersion = binaryInfo.XPathSelectElement("./FUSMsg/FUSBody/Put/CURRENT_OS_VERSION/Data").Value;
            string binaryVersion = binaryInfo.XPathSelectElement("./FUSMsg/FUSBody/Results/LATEST_FW_VERSION/Data").Value;

            Console.WriteLine($@"
  OS: {binaryOSVersion}
  Filename: {binaryFilename}
  Size: {binaryByteSize} bytes
  Logic Value: {binaryLogicValue}
  Description:
    {string.Join("\n    ", binaryDescription.TrimStart().Split('\n'))}");

            string binaryInitXMLString;
            responseStatus = Utils.FUSClient.DownloadBinaryInit(Utils.Msg.GetBinaryInitMsg(binaryFilename, Utils.FUSClient.NonceDecrypted), out binaryInitXMLString);

            Utils.File.FileSize = binaryByteSize;
            Utils.File.SetDecryptKey(binaryVersion, binaryLogicValue);

            string savePath = Path.GetFullPath($"./{model}_{region}");

            Console.WriteLine($@"
{savePath}");

            Utils.FUSClient.DownloadBinary(binaryModelPath, binaryFilename, savePath);
        }
    }
}
