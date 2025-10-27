using iTextSharp.text.pdf;
using System;
using System.ComponentModel;
using System.Data;
using System.IO;

namespace EDGE.Application.Processing.Infra.Pdf
{
    public static class FormsManager
    {
        public static byte[] CreateApplicationPdf<T>(T app, string PdfTemplate = "Service-1-Admission-Slip.pdf") where T : class
        {
            var pdfPath = $@"{ Directory.GetCurrentDirectory() }/PDFTemplate/{PdfTemplate}";
            var template = Path.GetFullPath(pdfPath);

            PdfReader pdfReader = new(@template);

            MemoryStream stream = new();

            PdfStamper stamper = new(pdfReader, stream);

            DataTable table = new();

            var props = TypeDescriptor.GetProperties(typeof(T));

            foreach (PropertyDescriptor property in props)
            {
                table.Columns.Add(property.Name.Replace("_", "."), Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
            }

            var row = table.NewRow();

            foreach (PropertyDescriptor property in props)
            {
                row[property.Name.Replace("_", ".")] = property.GetValue(app) ?? DBNull.Value;
            }

            table.Rows.Add(row);

            var columnNames = new string[table.Columns.Count];
            var columnTypes = new Type[table.Columns.Count];

            for (int i = 0; i < table.Columns.Count; i++)
            {
                columnNames[i] = table.Columns[i].ColumnName;
                columnTypes[i] = table.Columns[i].DataType;
            }
            foreach (DataRow dataRow in table.Rows)
            {
                for (int i = 0; i < columnNames.Length; i++)
                {
                    var colName = columnNames[i];
                    var colType = columnTypes[i];
                    var currentItem = dataRow[i] != DBNull.Value ? dataRow[i] : null;

                    if (colType != typeof(bool))
                    {
                        SetFieldType(
                            stamper, 
                            colName, 
                            typeof(string), 
                            currentItem?.ToString() ?? ""
                        );
                    }
                    else
                    {
                        SetFieldType(stamper, colName, colType, currentItem);
                    }
                }
            }

            SetFieldType(stamper, "System.Date", Type.GetType("DateTime"), DateTimeOffset.UtcNow.Date.ToShortDateString());

            stamper.FormFlattening = true;

            stamper.Close();

            stream.Close();

            pdfReader.Close();

            return stream.ToArray();
        }

        private static void SetField(PdfStamper stamper, string name, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            var fields = stamper.AcroFields;


            fields.SetField(name, value);

        }

        private static void SetField(PdfStamper stamper, string name, decimal? value)
        {
            if (!value.HasValue) return;

            var fields = stamper.AcroFields;

            fields.SetField(name, value.Value.ToString("###,###,###.00#"));
        }

        private static void SetField(PdfStamper stamper, string name, DateTime? value)
        {
            if (!value.HasValue) return;

            var fields = stamper.AcroFields;

            fields.SetField(name, value.Value.ToShortDateString());
        }

        private static void SetFieldType(PdfStamper pdfStamper, string name, Type type, object field)
        {
            System.Diagnostics.Debug.WriteLine(name + " " + field.ToString());
            if (type == null)
            {
                SetField(pdfStamper, name, field.ToString());
            }
            else
            {
                switch (type.Name)
                {
                    case "String":
                        SetField(pdfStamper, name, field.ToString());
                        break;
                    case "Decimal":
                        SetField(pdfStamper, name, (decimal?)field);
                        break;
                    case "DateTime":
                        SetField(pdfStamper, name, (DateTime?)field);
                        break;

                }
            }
        }
    }
}

