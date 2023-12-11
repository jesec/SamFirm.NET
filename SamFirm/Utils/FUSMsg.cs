using System.Xml.Linq;

namespace SamFirm.Utils
{
    internal static class Msg
    {
        public static string GetBinaryInformMsg(string version, string region, string model, string nonce)
        {
            XDocument document = new XDocument(
                new XElement("FUSMsg",
                    new XElement("FUSHdr",
                        new XElement("ProtoVer", "1.0")),
                    new XElement("FUSBody",
                        new XElement("Put",
                            new XElement("ACCESS_MODE",
                                new XElement("Data", "2")),
                            new XElement("BINARY_NATURE",
                                new XElement("Data", "1")),
                            new XElement("CLIENT_PRODUCT",
                                new XElement("Data", "Smart Switch")),
                            new XElement("DEVICE_FW_VERSION",
                                new XElement("Data", version)),
                            new XElement("DEVICE_LOCAL_CODE",
                                new XElement("Data", region)),
                            new XElement("DEVICE_MODEL_NAME",
                                new XElement("Data", model)),
                            new XElement("LOGIC_CHECK",
                                new XElement("Data", Auth.GetLogicCheck(version, nonce)))
                            )
                        )
                    )
                );

            // Add additional fields for EUX
            if (region == "EUX")
            {
                document.Root.Element("FUSBody").Element("Put").Add(
                    new XElement("DEVICE_AID_CODE",
                        new XElement("Data", region)),
                    new XElement("DEVICE_CC_CODE",
                        new XElement("Data", "DE")),
                    new XElement("MCC_NUM",
                        new XElement("Data", "262")),
                    new XElement("MNC_NUM",
                        new XElement("Data", "01"))
                );
            }
            // Add additional fields for EUY
            else if (region == "EUY")
            {
                document.Root.Element("FUSBody").Element("Put").Add(
                    new XElement("DEVICE_AID_CODE",
                        new XElement("Data", region)),
                    new XElement("DEVICE_CC_CODE",
                        new XElement("Data", "RS")),
                    new XElement("MCC_NUM",
                        new XElement("Data", "220")),
                    new XElement("MNC_NUM",
                        new XElement("Data", "01"))
                );
            }

            return document.ToString();
        }

        public static string GetBinaryInitMsg(string filename, string nonce)
        {
            XDocument document = new XDocument(
                new XElement("FUSMsg",
                    new XElement("FUSHdr",
                        new XElement("ProtoVer", "1.0")),
                    new XElement("FUSBody",
                        new XElement("Put",
                            new XElement("BINARY_FILE_NAME",
                                new XElement("Data", filename)),
                            new XElement("LOGIC_CHECK",
                                new XElement("Data", Auth.GetLogicCheck(filename.Split(".")[0][^16..], nonce)))
                            )
                        )
                    )
                );

            return document.ToString();
        }
    }
}
