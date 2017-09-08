using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace pdf2xml
{
    class RegexItem<TProperty>
    {
        private readonly Regex regex;
        private readonly PropertyInfo propertyMain;
        private readonly PropertyInfo[] properties;

        public RegexItem(string regex, params Expression<Func<TProperty, string>>[] properties)
        {
            this.regex = new Regex(regex);
            this.properties = properties.Select(p => GetPropertyInfo(p)).ToArray();
            this.propertyMain = typeof(ReportData).GetProperties().Single(p => p.PropertyType == typeof(TProperty));
        }

        public void Fill(ReportData data, string text)
        {
            var match = regex.Match(text);
            if (match.Success)
            {
                var mainValue = propertyMain.GetValue(data);
                for (int i = 0; i < properties.Length; i++)
                {
                    properties[i].SetValue(mainValue, match.Groups[i + 1].Value.Trim());
                }
            };
        }

        PropertyInfo GetPropertyInfo(Expression<Func<TProperty, string>> property)
        {
            Type type = typeof(ReportData);

            MemberExpression member = property.Body as MemberExpression;
            PropertyInfo propInfo = member.Member as PropertyInfo;

            return propInfo;
        }
    }

    class PdfParser
    {
        static RegexItem<PatientDetails> patientDetails = new RegexItem<PatientDetails>(@"Name\s*(.*)\sRec\.\sstart\s*(.*)\s*ID\s*(.*)\sLength\s*(.*)\s*Age\s*(.*)\sRecorder\s*(.*)\s*Gender\s*(.*)\s*Analysed\sby\s*(.*)\s*Referring\sDr.\s*(.*)\sReported\sby\s*(.*)",
            p => p.Name, p => p.RecStart, p => p.ID, p => p.Length, p => p.Age, p => p.Recorder, p => p.Gender, p => p.AnalysedBy, p => p.ReferringDr, p => p.ReportedBy);

        public static ReportData Parse(FileStream stream)
        {
            var reader = new PdfReader(stream);
            var text = PdfTextExtractor.GetTextFromPage(reader, 1);

            var data = new ReportData();
            patientDetails.Fill(data, text);

            return data;
        }
    }
}
