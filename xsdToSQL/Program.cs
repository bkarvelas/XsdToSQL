using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace xsdToSQL
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: MyApp.exe <path_to_xsd> <output_file_path>");
                    return;
                }

                string xsdFilePath = args[0];
                string outputFilePath = args[1];

                // Extract the filename without extension to use as a prefix
                string xsdPrefix = Path.GetFileNameWithoutExtension(xsdFilePath);

                XmlDocument xsdDocument = new XmlDocument();
                xsdDocument.Load(xsdFilePath);

                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xsdDocument.NameTable);
                namespaceManager.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");

                // Retrieve the type mappings
                Dictionary<string, string> typeMappings = GetMappings();

                XmlNodeList dataSetNodes = xsdDocument.SelectNodes("//xs:element[contains(@*[local-name() = 'IsDataSet'], 'true')]", namespaceManager);


                using (StreamWriter sw = new StreamWriter(outputFilePath))
                {
                    foreach (XmlNode dataSetNode in dataSetNodes)
                    {
                        XmlNodeList tableNodes = dataSetNode.SelectNodes("xs:complexType/xs:choice/xs:element", namespaceManager);

                        foreach (XmlNode tableNode in tableNodes)
                        {
                            string tableName = tableNode.Attributes["name"]?.Value;

                            if (!string.IsNullOrEmpty(tableName))
                            {
                                // Use the filename as a prefix for the table name
                                sw.WriteLine($"CREATE TABLE {xsdPrefix}_{tableName} (");

                                XmlNodeList columns = tableNode.SelectNodes("xs:complexType/xs:sequence/xs:element", namespaceManager);

                                foreach (XmlNode column in columns)
                                {
                                    string name = column.Attributes["name"]?.Value;
                                    string type = column.Attributes["type"]?.Value ?? GetSimpleType(column, namespaceManager);

                                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(type) && typeMappings.ContainsKey(type))
                                    {
                                        sw.WriteLine($"    {name} {typeMappings[type]}{(column.Attributes["minOccurs"]?.Value == "0" ? " NULL," : " NOT NULL,")}");
                                    }
                                }

                                sw.WriteLine(");");
                                sw.WriteLine(); // Add a newline for readability
                            }
                        }
                    }
                }

                Console.WriteLine($"T-SQL script has been written to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }


        private static string GetSimpleType(XmlNode columnNode, XmlNamespaceManager namespaceManager)
        {
            var simpleTypeNode = columnNode.SelectSingleNode("xs:simpleType/xs:restriction", namespaceManager);
            if (simpleTypeNode != null)
            {
                return simpleTypeNode.Attributes["base"]?.Value;
            }
            return null;
        }

        private static Dictionary<string, string> GetMappings()
        {
            return new Dictionary<string, string>
                {
                    { "xs:string", "NVARCHAR(MAX)" },
                    { "xs:normalizedString", "NVARCHAR(MAX)" },
                    { "xs:token", "NVARCHAR(MAX)" },
                    { "xs:base64Binary", "VARBINARY(MAX)" },
                    { "xs:hexBinary", "VARBINARY(MAX)" },
                    { "xs:integer", "BIGINT" },
                    { "xs:positiveInteger", "BIGINT" },
                    { "xs:negativeInteger", "BIGINT" },
                    { "xs:nonNegativeInteger", "BIGINT" },
                    { "xs:nonPositiveInteger", "BIGINT" },
                    { "xs:long", "BIGINT" },
                    { "xs:int", "INT" },
                    { "xs:short", "SMALLINT" },
                    { "xs:byte", "TINYINT" },
                    { "xs:unsignedLong", "DECIMAL(20, 0)" },
                    { "xs:unsignedInt", "BIGINT" },
                    { "xs:unsignedShort", "INT" },
                    { "xs:unsignedByte", "SMALLINT" },
                    { "xs:decimal", "DECIMAL(18, 2)" },
                    { "xs:float", "REAL" },
                    { "xs:double", "FLOAT" },
                    { "xs:boolean", "BIT" },
                    { "xs:dateTime", "DATETIME" },
                    { "xs:date", "DATE" },
                    { "xs:time", "TIME" },
                    { "xs:duration", "NVARCHAR(50)" }, // Duration has a specific format which may be represented as a string in SQL.
                    { "xs:gYearMonth", "NVARCHAR(7)" }, // Format: YYYY-MM
                    { "xs:gYear", "SMALLINT" },
                    { "xs:gMonthDay", "NVARCHAR(5)" }, // Format: --MM-DD
                    { "xs:gDay", "NVARCHAR(5)" }, // Format: ---DD
                    { "xs:gMonth", "NVARCHAR(5)" }, // Format: --MM
                    { "xs:anyURI", "NVARCHAR(MAX)" },
                    { "xs:QName", "NVARCHAR(255)" },
                    { "xs:NOTATION", "NVARCHAR(255)" },
                    { "xs:ID", "NVARCHAR(255)" },
                    { "xs:IDREF", "NVARCHAR(255)" },
                    { "xs:IDREFS", "NVARCHAR(MAX)" }, // A space-separated list of IDREF values.
                    { "xs:ENTITY", "NVARCHAR(255)" },
                    { "xs:ENTITIES", "NVARCHAR(MAX)" }, // A space-separated list of ENTITY values.
                    { "xs:NMTOKEN", "NVARCHAR(255)" },
                    { "xs:guid", "UNIQUEIDENTIFIER" },
                };
        }
    }
}
