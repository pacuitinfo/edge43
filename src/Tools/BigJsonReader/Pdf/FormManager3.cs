using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace EDGE.Application.Processing.Infra.Pdf
{
    public static class PdfBindingService
    {
        public static byte[] MergePdfs(byte[][] pdfBytesArray)
        {
            using (var msOutput = new MemoryStream())
            {
                var document = new Document();
                var copy = new PdfCopy(document, msOutput);
                document.Open();

                foreach (var pdfBytes in pdfBytesArray)
                {
                    using (var reader = new PdfReader(pdfBytes))
                    {
                        var n = reader.NumberOfPages;
                        for (var page = 1; page <= n; page++)
                        {
                            copy.AddPage(copy.GetImportedPage(reader, page));
                        }
                    }
                }

                document.Close();
                return msOutput.ToArray();
            }
        }

        public static byte[] CreateApplicationPdf<T>(T app, byte[] PdfTemplate,
            Dictionary<string, List<List<string>>> tables = null) where T : class
        {
            using (var templateStream = new MemoryStream(PdfTemplate))
            using (var outputStream = new MemoryStream())
            {
                var pdfReader = new PdfReader(templateStream);
                var stamper = new PdfStamper(pdfReader, outputStream);
                var form = stamper.AcroFields;

                var propInfos = app.GetType().GetProperties();
                foreach (var prop in propInfos)
                {
                    var pName = prop.Name;
                    var pValue = prop.GetValue(app);
                    var value = pValue?.ToString();
                    var type = prop.PropertyType;

                    if (!string.IsNullOrEmpty(value) && IsBase64Image(value))
                    {
                        InsertImageAtFieldPosition(stamper, form, pName, value);
                        continue;
                    }

                    SetFieldType(stamper, pName, type, pValue);
                }

                if (tables != null)
                {
                    foreach (var tableEntry in tables)
                    {
                        var tableName = tableEntry.Key;
                        var tableData = tableEntry.Value;
                        SetTable(form, tableName, tableData);
                    }
                }

                // Optional: Add system date (if needed)
                SetFieldType(stamper, "System.Date", typeof(DateTime), DateTimeOffset.UtcNow.Date.ToShortDateString());

                stamper.FormFlattening = true;
                stamper.Close();
                pdfReader.Close();

                return outputStream.ToArray();
            }
        }
 #region Helpers

        private static DataTable BuildDataTable<T>(T app) where T : class
        {
            var table = new DataTable();
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
            return table;
        }

        private static bool IsBase64Image(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return false;

            try
            {
                var imageBytes = Convert.FromBase64String(base64);
                return imageBytes.Length > 4 &&
                       ((imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && imageBytes[2] == 0x4E && imageBytes[3] == 0x47) || // PNG
                        (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)); // JPG
            }
            catch
            {
                return false;
            }
        }

        private static void InsertImageAtFieldPosition(PdfStamper stamper, AcroFields formFields, string fieldName, string base64)
        {
            try
            {
                if (!formFields.Fields.ContainsKey(fieldName))
                    return;

                var positions = formFields.GetFieldPositions(fieldName);
                if (positions == null || positions.Count == 0)
                    return;

                var position = positions[0]; // Use the first position
                var imageBytes = Convert.FromBase64String(base64);
                var image = iTextSharp.text.Image.GetInstance(imageBytes);

                image.ScaleToFit(position.position.Width, position.position.Height);
                image.SetAbsolutePosition(position.position.Left, position.position.Bottom);

                stamper.GetOverContent(position.page).AddImage(image);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error placing image at field '{fieldName}': {ex.Message}");
            }
        }

        private static void SetField(PdfStamper stamper, string name, string value)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(name))
                return;

            var fields = stamper.AcroFields;
            fields.SetField(name, value);
        }

        private static void SetField(PdfStamper stamper, string name, decimal? value)
        {
            if (!value.HasValue || string.IsNullOrEmpty(name))
                return;

            var fields = stamper.AcroFields;
            fields.SetField(name, value.Value.ToString("###,###,###.00#"));
        }

        private static void SetField(PdfStamper stamper, string name, DateTime? value)
        {
            if (!value.HasValue || string.IsNullOrEmpty(name))
                return;

            var fields = stamper.AcroFields;
            fields.SetField(name, value.Value.ToShortDateString());
        }

        private static void SetFieldType(PdfStamper stamper, string name, Type type, object field)
        {
            if (type == null || string.IsNullOrEmpty(name))
            {
                SetField(stamper, name, field?.ToString());
            }
            else
            {
                switch (type.Name)
                {
                    case "String":
                        SetField(stamper, name, field?.ToString());
                        break;
                    case "Decimal":
                        SetField(stamper, name, field as decimal?);
                        break;
                    case "DateTime":
                        SetField(stamper, name, field as DateTime?);
                        break;
                }
            }
        }

        private static string SanitizeFieldName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var sanitized = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '.' || c == '-').ToArray());
            return sanitized.Length > 50 ? sanitized.Substring(0, 50) : sanitized;
        }

        #endregion
        private static void SetTable(AcroFields form, string tableName, List<List<string>> tableData)
        {
            if (form == null || tableName == null || tableData == null)
            {
                throw new ArgumentNullException();
            }

            if (form.Fields.ContainsKey("Header"))
            {
                form.SetField("Header", tableName);
            }

            for (var col = 0; col < tableData[0].Count; col++)
            {
                var columnHeaderField = $"Table_ColumnHeader_{col + 1}";
                if (form.Fields.ContainsKey(columnHeaderField))
                {
                    form.SetField(columnHeaderField, tableData[0][col]);
                }
            }

            for (var row = 1; row < tableData.Count; row++)
            {
                for (var col = 0; col < tableData[0].Count; col++)
                {
                    var cellValue = tableData[row][col];
                    var cellField = $"Table_Row{row}_Col{col + 1}";
                    if (form.Fields.ContainsKey(cellField))
                    {
                        form.SetField(cellField, cellValue);
                    }
                }
            }
        }

        private static void SetField(AcroFields form, string name, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            if (form.Fields.ContainsKey(name))
            {
                form.SetField(name, value);
            }
        }

        private static void SetField(AcroFields form, string name, decimal? value)
        {
            if (!value.HasValue) return;
            if (form.Fields.ContainsKey(name))
            {
                form.SetField(name, value.Value.ToString("###,###,###.00#"));
            }
        }

        private static void SetField(AcroFields form, string name, DateTime? value)
        {
            if (!value.HasValue) return;
            if (form.Fields.ContainsKey(name))
            {
                form.SetField(name, value.Value.ToShortDateString());
            }
        }

        

        
        private static void SetFieldType(AcroFields form, string name, Type type, object field)
        {
            if (type == null)
            {
                SetField(form, name, field?.ToString() ?? "");
            }
            else
            {
                switch (type.Name)
                {
                    case "String":
                        SetField(form, name, field?.ToString() ?? "");
                        break;
                    case "Decimal":
                        SetField(form, name, field as decimal?);
                        break;
                    case "DateTime":
                        SetField(form, name, field as DateTime?);
                        break;
                }
            }
        }
    }
}
