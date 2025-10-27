using iTextSharp.text.pdf;
using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;

namespace EDGE.Application.Processing.Infra.Pdf
{
    public static class FormsManager2
    {
        public static byte[] CreateApplicationPdf<T>(T app, string PdfTemplate = "Service-1-Admission-Slip.pdf") where T : class
        {
            var pdfPath = Path.Combine(Directory.GetCurrentDirectory(), "PDFTemplate", PdfTemplate);
            var template = Path.GetFullPath(pdfPath);

            using var pdfReader = new PdfReader(template);
            using var stream = new MemoryStream();
            using var stamper = new PdfStamper(pdfReader, stream);

            var formFields = stamper.AcroFields;
            var table = BuildDataTable(app);

            foreach (DataRow drow in table.Rows)
            {
                for (var i = 0; i < drow.ItemArray.Length; i++)
                {
                    var originalColName = table.Columns[i].ColumnName;
                    var colName = SanitizeFieldName(originalColName);
                    if (string.IsNullOrEmpty(colName))
                        continue;

                    var currentItem = drow.ItemArray[i];
                    var value = currentItem?.ToString();
                    var colType = table.Columns[i].DataType;

                    if (!string.IsNullOrEmpty(value) && IsBase64Image(value))
                    {
                        InsertImageAtFieldPosition(stamper, formFields, colName, value);
                        continue; // Skip text field for images
                    }

                    SetFieldType(stamper, colName, colType, currentItem);
                }
            }

            // Add system date (optional utility field)
            SetFieldType(stamper, "System.Date", typeof(DateTime), DateTimeOffset.UtcNow.Date.ToShortDateString());

            stamper.FormFlattening = true;
            stamper.Close();
            pdfReader.Close();

            return stream.ToArray();
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
    }
}
