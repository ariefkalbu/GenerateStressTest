using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Excel = Microsoft.Office.Interop.Excel;

namespace GenerateDataST
{

    public class FileDocumentXML
    {
        public string FileName { get; set; }
        public List<string> ClassRef { get; set; }
        public string TableName { get; set; }
        public FileInfo FileXml { get; set; }
        public bool isGenerateExcel { get; set; }
    }

    public class GenerateDataStressTest
    {

        public bool GenerateStuctureByXML()
        {
            FileInfo[] files = GetAllDocument();
            if (files == null || files.Count() <= 0)
            {
                //WriteErrorMessages("Files XML Not Found");
                return false;
            }
            List<FileDocumentXML> fileDocumentXMLs = ReadAndBindXML(files);
            WriteExcel(fileDocumentXMLs);
            return true;
        }

        public FileInfo[] GetAllDocument()
        {
            DirectoryInfo directory = new DirectoryInfo(@"./");
            FileInfo[] files = directory.GetFiles("*.xml");
            return files;
        }

        public List<FileDocumentXML> ReadAndBindXML(FileInfo[] files)
        {
            List<FileDocumentXML> fileDocumentXMLs = new List<FileDocumentXML>();

            foreach (FileInfo file in files)
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(file.FullName);

                FileDocumentXML fileDocumentXML = new FileDocumentXML();
                fileDocumentXML.FileName = file.Name;
                fileDocumentXML.FileXml = file;
                fileDocumentXML.isGenerateExcel = false;

                XmlNodeList nodes = xmlDocument.GetElementsByTagName("objectMaps");

                List<String> listClassForeignKeys = new List<String>();
                Dictionary<string, string> dictForeignKey = new Dictionary<string, string>();

                foreach (XmlNode node in nodes)
                {
                    fileDocumentXML.TableName = node.Attributes["table"].Value;
                    fileDocumentXML.isGenerateExcel = node.Attributes["generateData"] != null ? Convert.ToBoolean(node.Attributes["generateData"].Value) : false;
                    List<String> classReff = new List<string>();
                    foreach (XmlNode subnode in node)
                    {
                        if (subnode.Attributes["class"] != null && (subnode.Attributes["svalue"] == null || subnode.Attributes["svalue"].Value == "generated"))
                        {
                            classReff.Add(subnode.Attributes["class"].Value);
                        }
                    }
                    fileDocumentXML.ClassRef = classReff;
                }
                fileDocumentXMLs.Add(fileDocumentXML);
            }
            return fileDocumentXMLs;
        }

        public bool WriteExcel(List<FileDocumentXML> fileDocumentXMLs)
        {

            //var file = new FileInfo("sample.xlsx");

            //ExcelPackage package = new ExcelPackage(file);
            //package.Workbook.Worksheets.Add("sample");
            //ExcelWorksheet ws = package.Workbook.Worksheets["sample"];
            //ws.Cells["A1"].Value = "HELLO";
            ////package.Save();
            //package.Workbook.Worksheets.Add("sample2");
            //ws = package.Workbook.Worksheets["sample2"];
            //ws.Cells["A1"].Value = "HELLXO";
            //package.Save();

            foreach (FileDocumentXML fileDocument in fileDocumentXMLs)
            {
                if (!fileDocument.isGenerateExcel) continue;

                var file = new FileInfo(fileDocument.TableName + ".xlsx");

                ExcelPackage package = new ExcelPackage(file);

                foreach (String nameFileRef in fileDocument.ClassRef)
                {
                    DirectoryInfo directory = new DirectoryInfo(@"./");
                    FileInfo[] files = directory.GetFiles(nameFileRef + ".xml");
                    if (files != null)
                    {
                        foreach (FileInfo fileRef in files)
                        {
                            package = WriteWorksheet(package, nameFileRef, fileRef);
                        }
                    }
                }
                package = WriteWorksheet(package, fileDocument.TableName, fileDocument.FileXml);
                package.Save();
            }

            return true;
        }

        public ExcelPackage WriteWorksheet(ExcelPackage package, String WorksheetName, FileInfo fileXml)
        {
            package.Workbook.Worksheets.Add(WorksheetName);
            ExcelWorksheet ws = package.Workbook.Worksheets[WorksheetName];

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(fileXml.FullName);


            XmlNodeList nodes = xmlDocument.GetElementsByTagName("objectMaps");

            foreach (XmlNode node in nodes)
            {
                int row = 1;
                int col = 1;

                string TableName = node.Attributes["table"].Value;
                
                foreach (XmlNode subnode in node)
                {
                    ws.Cells[row, col].Value = subnode.Attributes["field"].Value;

                    for(int rowValue = 2; rowValue < 100; rowValue++)
                    {
                        string value = subnode.Attributes["svalue"].Value;
                        string dbtype = subnode.Attributes["dbtype"].Value;
                        string prefix = subnode.Attributes["prefix"] != null ? subnode.Attributes["prefix"].Value : "";
                        if (value.ToUpper() == "GENERATED") {
                            if (dbtype.Contains("date"))
                            {
                                value = Convert.ToString(DateTime.Now);
                            }
                            else if(dbtype == "int")
                            {
                                value = Convert.ToString(rowValue);
                            }
                            else if (!String.IsNullOrEmpty(prefix)) value = prefix + Convert.ToString(rowValue);
                            else
                            {
                                value = TableName + rowValue;
                            }
                        }
                        else if(value == "-")
                        {
                            if (!String.IsNullOrEmpty(prefix)) value = prefix + Convert.ToString(rowValue);
                            else value = subnode.Attributes["class"].Value + Convert.ToString(rowValue);
                        }
                        ws.Cells[rowValue, col].Value = value;
                    }
                    col += 1;
                }
            }
            return package;
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            GenerateDataStressTest indexKey = new GenerateDataStressTest();
            indexKey.GenerateStuctureByXML();
        }
    }
}
