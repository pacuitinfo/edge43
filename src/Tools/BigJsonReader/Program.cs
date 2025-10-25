using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ClosedXML.Excel;
using ClosedXML.Report;
// ---------- CLI args ----------
string Arg(string name, string def = "")
{
    var a = Environment.GetCommandLineArgs();
    var i = Array.IndexOf(a, $"--{name}");
    return (i >= 0 && i + 1 < a.Length) ? a[i + 1] : def;
}

string owner     = Arg("owner");
string repo      = Arg("repo");
string path      = Arg("path");            // e.g., cache/report:::VII
string @ref      = Arg("ref", "main");
string dateStart = Arg("dateStart", "");
string dateEnd   = Arg("dateEnd", "");
string regionKey   = Arg("regionKey", "");
string outPath   = Arg("out", "");
string region    = Arg("region", "");      // OPTIONAL: filter by GitHub label and annotate chart labels
string? token    = Environment.GetEnvironmentVariable("GH_PAT")
                ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");

if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo) || string.IsNullOrWhiteSpace(path))
{
    Console.Error.WriteLine("Missing --owner/--repo/--path");
    Environment.Exit(1);
}

// ---------- helpers ----------
static DateTime? TryParseDate(string? s)
{
    if (string.IsNullOrWhiteSpace(s)) return null;
    if (DateTimeOffset.TryParse(s, out var dto)) return dto.UtcDateTime.Date;
    if (DateTime.TryParse(s, out var dt)) return dt.Date;
    return null;
}
static string EscapeSegments(string p) =>
    string.Join("/", p.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
static string Lower(string? s) => string.IsNullOrWhiteSpace(s) ? "" : s.ToLowerInvariant();
static byte[] GenerateCashReceiptsRecordExcel(Reports report, UserModel? user)
    {
        var fileContent = new byte[] { };



        try
        {
            List<RecordModel> records = new();
            foreach (var applicationService in report.Docs)
            {
               var basicFirstName = applicationService?.Service?["basic"]?["firstName"]?.ToString() ?? "";
                var basicLastName = applicationService?.Service?["basic"]?["lastName"]?.ToString() ?? "";
                var basicCompanyName = applicationService?.Service?["basic"]?["companyName"]?.ToString() ?? "";
                var basicClubName = applicationService?.Service?["basic"]?["clubName"]?.ToString() ?? "";

              var companyName = applicationService?.Applicant?.CompanyName?.ToString() ?? "";
                var clubName = applicationService?.Service?["applicationDetails"]?["clubName"]?.ToString() ?? "";
                var firstName = applicationService?.Applicant?.FirstName?.ToString() ?? "";
                var lastName = applicationService?.Applicant?.LastName?.ToString() ?? "";
                var applicantName = applicationService?.Applicant?.ApplicantName?.ToString() ?? "";

                var result = !string.IsNullOrEmpty(firstName) ? firstName
                    : !string.IsNullOrEmpty(lastName) ? lastName
                    : applicantName;
                RecordModel record = new();
                record.Date = applicationService.CreatedAt.ToString();
               record.OrNo = applicationService.OfficialReceipt?.ORNumber ?? "";

                record.PermitToPurchase = applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name == "Purchase Permit Fee").Value.ToString() ?? "0";
                record.FilingFeeLicense = applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name ==  "Filling Fee").Value.ToString() ?? "0";
                record.PermitToPossessStorage = applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name ==  "Possess Permit Fee").Value.ToString() ?? "0"; 
                record.RadioStationLicense = "0";
                record.SeminarFee = "0";
                record.RadioStationModificationFee = applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name ==  "Modification Fee").Value.ToString() ?? "0"; 
                record.RadioOperatiorCertif = applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name ==  "Certificate Fee").Value.ToString() ?? "0";  
                record.RadioStationLicenseArocRoc = "0" ;   
                record.PermitFee = "0" ;
                record.Mpo = "0";
                record.ConstructionPermitFee =  applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name == "Construction Permit Fee").Value.ToString() ?? "0";
                record.RepeaterStationInspectionFee =  applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name == "Repeater Station Inspection Fee").Value.ToString() ?? "0";
                record.RegistrationFees = applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name == "Registration Fee").Value.ToString() ?? "0"; 
                record.ExaminationFees =   applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name == "Examination Fee").Value.ToString() ?? "0";
                record.InspectionFeesLicense =    applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name == "Inspection Fee").Value.ToString() ?? "0";
                record.InspectionFeesPermit =    applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name == "Inspection Fee (Per Year)").Value.ToString() ?? "0";
                record.FilingFeePermit = applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name == "Filing Fee").Value.ToString() ?? "0";  
                record.ApplicationFeesFilingFee = applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name == "Seminar Fee / Application Fee").Value.ToString() ?? "0"; 
                
                record.SurchargesLicense = applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name == "Surcharge").Value.ToString() ?? "0";  
                record.Other =  applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name == "Other").Value.ToString() ?? "0"; 
                record.DocumentaryStampTax =  applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name == "Documentary Stamp Tax").Value.ToString() ?? "0"; 
                record.Sum = (applicationService?.ServicesReports?.Fees
                    .Find(c => c.Name == "Purchase Permit Fee").Value +  
                    applicationService?.ServicesReports?.Fees
                         .Find(c => c.Name == "Repeater Station Inspection Fee").Value +  
                    applicationService?.ServicesReports?.Fees
                        .Find(c => c.Name == "Repeater Station License Fee").Value + 
                     applicationService?.ServicesReports?.Fees
                         .Find(c => c.Name ==  "Filling Fee").Value +  
                     applicationService?.ServicesReports?.Fees
                         .Find(c => c.Name ==  "Possess Permit Fee").Value + 
                     applicationService?.ServicesReports?.Fees
                         .Find(c => c.Name ==  "Modification Fee").Value + 
                     applicationService?.ServicesReports?.Fees
                         .Find(c => c.Name ==  "Certificate Fee").Value
                     +  applicationService?.ServicesReports?.Fees
                         .Find(c => c.Name == "Construction Permit Fee").Value +  applicationService?.ServicesReports?.Fees
                         .Find(c => c.Name == "Registration Fee").Value +  applicationService?.ServicesReports?.Fees
                         .Find(c => c.Name == "Other").Value +  applicationService?.ServicesReports?.Fees
                         .Find(c => c.Name == "Examination Fee").Value + applicationService?.ServicesReports?.Fees
                         .Find(c => c.Name == "Inspection Fee").Value + applicationService?.ServicesReports?.Fees
                         .Find(c => c.Name == "Inspection Fee (Per Year)").Value + applicationService?.ServicesReports?.Fees
                         .Find(c => c.Name == "Filing Fee").Value + applicationService?.ServicesReports?.Fees
                         .Find(c => c.Name == "Seminar Fee / Application Fee").Value +  applicationService?.ServicesReports?.Fees
                         .Find(c => c.Name == "Surcharge").Value + applicationService?.ServicesReports?.Fees
                         .Find(c => c.Name == "Documentary Stamp Tax").Value).ToString() ?? "0";
                record.SurchargesPermits =  "0";  
                
                record.SurchargesArocRoc =  "0";
                record.MiscellaneousIncome =  "0";
                    record.VerficationAuthFees ="0";
                 
                record.SupervisionRegulationFees ="0";
                record.ClearanceCertificateFee = "0";
                record.UsageFees = "0";;
                string resultString;

                if (applicationService?.Applicant?.UserId == "2b2957c9-c604-4d0e-ad31-14466f172c06" ||
                    applicationService?.Applicant?.UserId == "41b17694-119a-4d3c-b996-7aa4ab6e9b91" ||
                    (basicFirstName?.ToString() != null && basicFirstName?.ToString() != "")|| (basicLastName?.ToString() != null  && basicLastName?.ToString() != ""))
                {
                    if ((basicFirstName?.ToString() != null && basicFirstName?.ToString() != "")  || (basicLastName?.ToString() != null  && basicLastName?.ToString() != ""))
                    {
                        resultString = $"{basicFirstName?.ToString() ?? ""} {basicLastName?.ToString() ?? ""}";
                    }
                    else if (basicCompanyName?.ToString() != null && basicCompanyName?.ToString() != "")
                    {
                        resultString = basicCompanyName;
                    }
                    else
                    {
                        resultString = basicClubName?.ToString() ?? companyName?.ToString();
                    }
                }
                else if (applicationService?.Applicant.UserType == "Individual")
                {
                    if (result != null)
                    {
                        resultString = string.IsNullOrWhiteSpace($"{firstName ?? ""} {lastName ?? ""}".Trim())
                            ? applicantName
                            : $"{firstName ?? ""} {lastName ?? ""}".Trim();
                    }
                    else
                    {
                        resultString = basicFirstName?.GetType() == typeof(JValue) || !string.IsNullOrEmpty(basicFirstName?.ToString())
                            ? $"{basicFirstName?.ToString() ?? ""} {basicLastName?.ToString() ?? ""}"
                            : basicCompanyName ?? "";
                    }
                }
                else
                {
                    resultString = !string.IsNullOrEmpty(companyName) || !string.IsNullOrEmpty(basicCompanyName)
                        ? companyName ?? basicCompanyName
                        : !string.IsNullOrEmpty(clubName)
                            ? clubName
                            : result != null && !string.IsNullOrEmpty(result)
                                ? string.IsNullOrWhiteSpace($"{firstName ?? ""} {lastName ?? ""}".Trim())
                                    ? applicantName
                                    : $"{firstName ?? ""} {lastName ?? ""}".Trim()
                                : basicFirstName?.GetType() == typeof(JValue) || !string.IsNullOrEmpty(basicFirstName?.ToString())
                                    ? $"{basicFirstName?.ToString() ?? ""} {basicLastName?.ToString() ?? ""}"
                                    : basicCompanyName ?? "";
                }
                
                record.Payor = resultString;
                records.Add(record);
            }
            var path = Path.Combine(AppContext.BaseDirectory, "ExcelTemplate");
            var applicationsReport = new ApplicationReportsExcel()
            {
                Year = DateTime.Now.ToString("yyyy"),
                RegionValue = $"Region {user?.EmployeeDetails?.Region}",
                Reports = records,
                Name = $"{user?.FirstName} {user?.MiddleName} {user?.LastName }" ,
                Designation = $"{user?.EmployeeDetails?.Designation}"
            };
            using (var streamTo = new MemoryStream())
            {
                var template = new XLTemplate(path + "/CASH-RECEIPTS-RECORD.xlsx");
                
                template.AddVariable(applicationsReport);
                template.Generate();

                template.SaveAs(streamTo);
                fileContent = streamTo.ToArray();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }


        return fileContent;
    }
static (ApplicationModel? app, JObject? raw) ParseApplicationFromBody(string? body)
{
    if (string.IsNullOrWhiteSpace(body)) return (null, null);

    // Body is JSON text – try direct parse
    try
    {
        var raw = JObject.Parse(body);
        return (raw.ToObject<ApplicationModel>(), raw);
    }
    catch { /* maybe the body has noise; try slicing braces */ }

    int first = body.IndexOf('{');
    int last  = body.LastIndexOf('}');
    if (first >= 0 && last > first)
    {
        var json = body.Substring(first, last - first + 1);
        try
        {
            var raw = JObject.Parse(json);
            return (raw.ToObject<ApplicationModel>(), raw);
        }
        catch { }
    }
    return (null, null);
}

Console.Error.WriteLine(path);
DateTime? ds = TryParseDate(dateStart);
DateTime? de = TryParseDate(dateEnd);

// counts: (Month,Year) -> status -> count
var monthlyStatusCounts = new Dictionary<(int Month, int Year), Dictionary<string,int>>();

// NEW: per-day status counts for last 30 days
var dailyStatusCounts = new Dictionary<DateTime, Dictionary<string, int>>();
Reports soareports = null;
// a tiny in-memory report accumulator (auto-adds service rows)
var servicesReports = new ServicesReports { Region = region };

// ---------- GitHub HTTP ----------
using var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
using var http = new HttpClient(handler);
if (!string.IsNullOrWhiteSpace(token))
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token);
http.DefaultRequestHeaders.UserAgent.ParseAdd("big-json-reader/1.0");
http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
Console.Error.WriteLine(owner);
Console.Error.WriteLine(repo);
Console.Error.WriteLine(path);
Console.Error.WriteLine(regionKey);
// 1) metadata → download_url
var metaUrl = $"https://api.github.com/repos/{repo}/contents/{EscapeSegments(regionKey)}?ref={Uri.EscapeDataString(@ref)}";
Console.Error.WriteLine(metaUrl);
using var metaResp = await http.GetAsync(metaUrl);
metaResp.EnsureSuccessStatusCode();
var metaJson = await metaResp.Content.ReadAsStringAsync();
var metaObj  = JObject.Parse(metaJson);
var rawUrl   = metaObj.Value<string>("download_url");
if (string.IsNullOrWhiteSpace(rawUrl))
{
    Console.Error.WriteLine("No download_url found.");
    Environment.Exit(1);
}

// 2) stream the raw JSON array (array of GitHub issues)
using var resp = await http.GetAsync(rawUrl, HttpCompletionOption.ResponseHeadersRead);
resp.EnsureSuccessStatusCode();
await using var netStream = await resp.Content.ReadAsStreamAsync();
using var sr = new StreamReader(netStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 128 * 1024);
using var reader = new JsonTextReader(sr) { SupportMultipleContent = false };

int processed = 0;
Console.Error.WriteLine(metaUrl);

// Expect array root
if (!await reader.ReadAsync() || reader.TokenType != JsonToken.StartArray)
{
    Console.Error.WriteLine("Expected a JSON array at root.");
    Environment.Exit(1);
}
var applications = new List<ApplicationModel>();
 decimal totalSum = 0;
while (await reader.ReadAsync())
{
    if (reader.TokenType == JsonToken.EndArray) break;
    if (reader.TokenType != JsonToken.StartObject) { await reader.SkipAsync(); continue; }

    // One issue object
    var joIssue = await JObject.LoadAsync(reader);
    var issue   = joIssue.ToObject<RepoInfo>();
    if (issue is null) continue;

    // Region filter: only keep issues that contain the region label (if provided)
    if (!string.IsNullOrWhiteSpace(region))
    {
        var hasRegion = issue.Labels != null &&
                        issue.Labels.Any(l => string.Equals(l, region, StringComparison.OrdinalIgnoreCase));
        if (!hasRegion) continue;
    }

    // Parse Body (JSON string) into ApplicationModel
    var (app, innerJo) = ParseApplicationFromBody(issue.Body);
    if (app is null) continue;

    applications.Add(app);
    
        



    // -------- monthly + daily status tally --------
    var updatedAt = app.UpdatedAt;
    if (updatedAt != null)
    {
        bool yearOk  = (ds == null) || (updatedAt!.Value.Year == ds.Value.Year);
        bool startOk = (ds == null) || (updatedAt!.Value.Month >= ds.Value.Month);
        bool endOk   = (de == null) || (updatedAt!.Value.Month <= de.Value.Month);
        if (yearOk && startOk && endOk)
        {
            var key = (updatedAt!.Value.Month, updatedAt!.Value.Year);
            if (!monthlyStatusCounts.TryGetValue(key, out var dict))
                monthlyStatusCounts[key] = dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var s = string.IsNullOrEmpty(app.Status) ? "(unknown)" : app.Status!;
            dict[s] = dict.TryGetValue(s, out var c) ? c + 1 : 1;
        }

        // last-30-days proportions
        var dayUtc = updatedAt.Value.ToUniversalTime().Date;
        var today  = DateTime.UtcNow.Date;
        var cutoff = today.AddDays(-29); // inclusive window

        if (dayUtc >= cutoff && dayUtc <= today)
        {
            if (!dailyStatusCounts.TryGetValue(dayUtc, out var dct))
                dailyStatusCounts[dayUtc] = dct = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var status = string.IsNullOrWhiteSpace(app.Status) ? "(unknown)" : app.Status!;
            dct[status] = dct.TryGetValue(status, out var c) ? c + 1 : 1;
        }
    }

    // -------- service-level fields ----------
    var applicationReceive   = Lower(app.Service?["applicationType"]?["label"]?.ToString());
    var natureOfServiceType  = app.Service?["natureOfService"]?["type"]?.ToString() ?? "";
    servicesReports.PermitNumber = app.PermitNumber ?? "";

    if (!string.IsNullOrEmpty(applicationReceive))
    {
        var m = Regex.Match(applicationReceive, @"\([^\)]*\)");
        if (m.Success)
        {
            servicesReports.ApplicationType = m.Value switch
            {
                "(new)"          => "NEW",
                "(renewal)"      => "REN",
                "(modification)" => "MOD",
                _                => servicesReports.ApplicationType
            };
        }
    }

    // years
    int noOfYear = 1;
    if (int.TryParse(app.Service?["applicationDetails"]?["noOfYears"]?.ToString(), out var parsedYears) && parsedYears > 0)
        noOfYear = parsedYears;
    servicesReports.NoOfYear = noOfYear;

    // period covered (best-effort)
    var validityStart = app.Service?["validityStart"]?.ToString() ?? innerJo?["validityStart"]?.ToString();
    var validityEnd   = app.Service?["validityEnd"]?.ToString()   ?? innerJo?["validityEnd"]?.ToString();
    servicesReports.PeriodCovered       = !string.IsNullOrWhiteSpace(validityEnd) ? $"{validityStart} to {validityEnd}" : "";
    servicesReports.NatureOfServiceType = natureOfServiceType;

    // ensure row keyed by label so we can mark ApplicationReceive
    var idx = servicesReports.EnsureRow(applicationReceive);
    if (!string.IsNullOrEmpty(applicationReceive))
    {
        if (applicationReceive.Contains("renewal") || applicationReceive.Contains("modification"))
            servicesReports.Services[idx].ApplicationReceive = "renewal";
        else if (applicationReceive.Contains("new"))
            servicesReports.Services[idx].ApplicationReceive = "new";
    }

    // equipments + per-particular routing
    int equipments = 0;
    if (app.Service?["particulars"] is JArray parts)
    {
        foreach (var p in parts.OfType<JObject>())
        {
            if (p["equipments"] is JArray eqs) equipments += Math.Max(0, eqs.Count);

            if (p["stationClass"] != null)
            {
                var subName = applicationReceive.Contains("radio station license");
                var subNotIncludeMicrowave = applicationReceive.Contains("radio station license - microwave");
                var subNotIncludeVSAT      = applicationReceive.Contains("radio station license - vsat");
                var subNotIncludeBWA       = applicationReceive.Contains("radio station license - bwa");
                var subNotIncludeWDN       = applicationReceive.Contains("radio station license - wdn");
                var subNotIncludeBTS       = applicationReceive.Contains("radio station license - bts");

                try
                {
                    if (subNotIncludeMicrowave)
                    {
                        Report.COGovernmentMicrowave(natureOfServiceType, p, servicesReports, idx, equipments, noOfYear);
                        Report.CVPrivateMicrowave  (natureOfServiceType, p, servicesReports, idx, equipments, noOfYear);
                        Report.CPPublicCorrespondenceMicrowave(natureOfServiceType, p, servicesReports, idx, equipments, noOfYear);
                    }
                    else if (subNotIncludeVSAT)
                    {
                        Report.COGovernmentVSAT(natureOfServiceType, p, servicesReports, idx, equipments, noOfYear);
                        Report.CVPrivateVSAT  (natureOfServiceType, p, servicesReports, idx, equipments, noOfYear);
                        Report.CPPublicCorrespondenceVSAT(natureOfServiceType, p, servicesReports, idx, equipments, noOfYear);
                    }
                    else if (subNotIncludeWDN)
                    {
                        Report.COGovernmentWDN(natureOfServiceType, p, servicesReports, idx, equipments, noOfYear);
                        Report.CVPrivateWDN  (natureOfServiceType, p, servicesReports, idx, equipments, noOfYear);
                        Report.CPPublicCorrespondenceWDN(natureOfServiceType, p, servicesReports, idx, equipments, noOfYear);
                    }
                    else if (subName && !(subNotIncludeMicrowave && subNotIncludeVSAT && subNotIncludeBWA && subNotIncludeWDN && subNotIncludeBTS))
                    {
                        Report.COGovernment(natureOfServiceType, p, servicesReports, idx, equipments, noOfYear);
                        Report.CVPrivate   (natureOfServiceType, p, servicesReports, idx, equipments, noOfYear);
                        Report.CPPublicCorrespondence(natureOfServiceType, p, servicesReports, idx, equipments, noOfYear);
                    }
                }
                catch (Exception e) { Console.WriteLine(e); }
            }
        }
    }
    if (equipments <= 0) equipments = 1;

    // bump the base row too
    servicesReports.Services[idx].Value += (1 * equipments * noOfYear);

    // type
    servicesReports.Services[idx].Type ??= app.Type;

    // SOA surcharges & totals (from inner JSON)
    if (innerJo?["soa"] is JArray soa && soa.Count > 0)
    {
        decimal Amt(JToken? x) => decimal.TryParse(x?.ToString(), out var d) ? d : 0m;

        var surcharge          = soa.OfType<JObject>().FirstOrDefault(x => (x["Item"]?.ToString() ?? "") == "Surcharge");
        var surLicenseFee      = soa.OfType<JObject>().FirstOrDefault(x => (x["Item"]?.ToString() ?? "") == "SUR - License Fee");
        var surSpectrumUserFee = soa.OfType<JObject>().FirstOrDefault(x => (x["Item"]?.ToString() ?? "") == "SUR - Spectrum User Fee");

        servicesReports.Services[idx].Surcharge += Amt(surcharge?["Amount"]);
        servicesReports.Services[idx].Surcharge += Amt(surLicenseFee?["Amount"]);
        servicesReports.Services[idx].Surcharge += Amt(surSpectrumUserFee?["Amount"]);
    }

    // total fee
    servicesReports.Services[idx].TotalFee +=  app.TotalFee;
    servicesReports.TotalFee               +=  app.TotalFee;

    // Elements bump (optional elementKey)
    var elementKey = app.Service?["applicationType"]?["element"]?.ToString();
    if (!string.IsNullOrWhiteSpace(elementKey))
    {
        servicesReports.Services[idx].Elements ??= new List<Element>();
        var e = servicesReports.Services[idx].Elements
            .FirstOrDefault(x => string.Equals(x.Name, elementKey, StringComparison.OrdinalIgnoreCase));

        if (e == null)
            servicesReports.Services[idx].Elements.Add(new Element { Name = elementKey, Value = 1 });
        else
            e.Value++;
    }

    // Fees rollup from app.Soa (line items)
    if (app.Soa != null)
    {
        foreach (var line in app.Soa)
        {
            var name = line?.Item;
            if (string.IsNullOrWhiteSpace(name)) continue;

            var idxFee = servicesReports.Fees.FindIndex(f =>
                f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (idxFee >= 0)
            {
                var add = line?.Amount ?? 0m;
                servicesReports.Fees[idxFee].Value =
                    (servicesReports.Fees[idxFee].Value ?? 0m) + add;
            }
        }
    }

    // Evaluator full name (safe)
    try
    {
        if (app.Evaluator != null)
        {
            servicesReports.Evaluator = PersonExtensions.GetFullName(
                app.Evaluator.FirstName,
                app.Evaluator.MiddleName,
                app.Evaluator.LastName,
                app.Evaluator.Suffix
            );
        }
    }
    catch (Exception e)
    {
        Console.Write(e);
    }

    processed++;
}
totalSum = 0;
var totals =  applications.Count();
var rows = applications
    .Where(c => c?.OfficialReceipt?.ORNumber != null)
    .Select(c => new AppRow
    {
        _id = c._id,
        Type = c.Type,
        Applicant = c.Applicant,
        Service = c.Service,
        Region = c.Region,
        Status = c.Status,
        PaymentStatus = c.PaymentStatus,
        PaymentMethod = c.PaymentMethod,
        Amnesty = c.Amnesty,
        TotalFee = c.TotalFee,
        AmnestyTotalFee = c.AmnestyTotalFee,
        AssignedPersonnel = c.AssignedPersonnel,
        IsPinned = c.IsPinned,
        ApprovalHistory = c.ApprovalHistory,
        PaymentHistory = c.PaymentHistory,
        Soa = c.Soa,
        SoaHistory = c.SoaHistory,
        Exam = c.Exam,
        OfficialReceipt = c.OfficialReceipt,
        OrderOfPayment = c.OrderOfPayment,
        Make = c.Make,
        Schedule = c.Schedule,
        ProofOfPayment = c.ProofOfPayment,
        Evaluator = c.Evaluator,
        Cashier = c.Cashier,
        Director = c.Director,
        Commissioner = c.Commissioner,
        Document = c.Document,
        TempDocument = c.TempDocument,
        DocumentNumber = c.DocumentNumber,
        QRCode = c.QRCode,
        Note = c.Note,
        DateOfExpiry = c.DateOfExpiry,
        ValidUntil = c.ValidUntil,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt ?? DateTime.MinValue,
        DateOfBirth = c.DateOfBirth,
        Validity = c.Validity,
        Renew = c.Renew,
        IsModified = c.IsModified,
        ReferenceNumber = c.ReferenceNumber,
        PermitNumber = c.PermitNumber,
        ServicesReports = new ServicesReports()
    });
try{
foreach (var application in rows)
{
     if (application.ServicesReports == null){
application.ServicesReports = new ServicesReports();
     }
        
   
    var applicationReceive = application.Service?["applicationType"]?["label"]
        ?.ToString()?.ToLowerInvariant();

    var natureOfServiceType = "";
    var natureOfService = application.Service?["natureOfService"];
    if (natureOfService?.Type == JTokenType.Object)
    {
        var typeToken = natureOfService["type"];
        if (typeToken?.Type is JTokenType.String or JTokenType.Integer)
            natureOfServiceType = typeToken.ToString();
    }

    // If you added EnsureRow helper on ServicesReports, prefer it:
    // var rowIndex = application.ServicesReports.EnsureRow(applicationReceive);
    // If not using EnsureRow, find row case-insensitively:
    var rowIndex = application.ServicesReports.Services.FindIndex(s =>
        s.Service.Equals(applicationReceive ?? "unknown", StringComparison.OrdinalIgnoreCase));

    if (rowIndex < 0) continue; // optionally: create a new row instead of continue

    // ---- Mark receive type -----------------------------------------------------
    if (!string.IsNullOrEmpty(applicationReceive))
    {
        if (applicationReceive.Contains("renewal") || applicationReceive.Contains("modification"))
            application.ServicesReports.Services[rowIndex].ApplicationReceive = "renewal";
        else if (applicationReceive.Contains("new"))
            application.ServicesReports.Services[rowIndex].ApplicationReceive = "new";
    }

    // ---- Years & equipments ----------------------------------------------------
    int noOfYear = 1;
    int equipments = 1;

    var applicationDetails = application.Service?["applicationDetails"];
    if (applicationDetails?.Type == JTokenType.Object)
    {
        var noOfYearsToken = applicationDetails["noOfYears"];
        if (noOfYearsToken?.Type is JTokenType.Integer or JTokenType.String)
            int.TryParse(noOfYearsToken.ToString(), out noOfYear);
        if (noOfYear <= 0) noOfYear = 1;
    }

    var particulars = application.Service?["particulars"] as JArray;
    if (particulars != null && particulars.Count > 0)
    {
        int count = 0;
        foreach (var p in particulars)
            if (p?["equipments"] is JArray eq) count += eq.Count;
        equipments = Math.Max(count, 1);
    }

    // ---- Update service row ----------------------------------------------------
    try
    {
        var service = application.ServicesReports.Services[rowIndex];

        service.Value = (service.Value + 1) * equipments * noOfYear;

        if (string.IsNullOrEmpty(service.Type))
            service.Type = application.Type;

        // SOA surcharges (all decimal-safe)
        if (application.Soa != null)
        {
            decimal surcharge =
                (application.Soa.Find(x => x.Item == "Surcharge")?.Amount ?? 0m) +
                (application.Soa.Find(x => x.Item == "SUR - License Fee")?.Amount ?? 0m) +
                (application.Soa.Find(x => x.Item == "SUR - Spectrum User Fee")?.Amount ?? 0m);

            service.Surcharge += surcharge;
        }

        // Total fee
        service.TotalFee += application.TotalFee;

        // Elements increment
        var elementName = application.Service?["applicationType"]?["element"]?.ToString();
        if (service.Elements != null && !string.IsNullOrWhiteSpace(elementName))
        {
            var idx = service.Elements.FindIndex(e =>
                e.Name.Equals(elementName, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) service.Elements[idx].Value++;
        }

        // SOA fees into Fees bucket (case-insensitive; fall back to "Other")
        if (application.Soa != null)
        {
            foreach (var line in application.Soa)
            {
                var feeName = line?.Item;
                var feeIdx = application.ServicesReports.Fees.FindIndex(f =>
                    f.Name.Equals(feeName ?? "", StringComparison.OrdinalIgnoreCase));

                if (feeIdx >= 0)
                    application.ServicesReports.Fees[feeIdx].Value += (line?.Amount ?? 0m);
                else
                {
                    var otherIdx = application.ServicesReports.Fees.FindIndex(f =>
                        f.Name.Equals("Other", StringComparison.OrdinalIgnoreCase));
                    if (otherIdx >= 0)
                        application.ServicesReports.Fees[otherIdx].Value += (line?.Amount ?? 0m);
                }
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error processing application {application._id}: {e.Message}");
    }

    // ---- Grand total -----------------------------------------------------------
    if ((application.TotalFee ) > 0m)
        totalSum += (application.TotalFee);
}
} catch (Exception e)
    {
        Console.Write(e);
    }

                 Console.WriteLine( JsonConvert.SerializeObject(applications.OrderByDescending(i => i.CreatedAt).FirstOrDefault()));
           soareports = new Reports()
            {
                Docs = applications.OrderByDescending(i => i.CreatedAt).ToList(),
                Total = totals,
             TotalSum = totalSum
            };

            var fileContext = GenerateCashReceiptsRecordExcel(soareports, null);

            if (fileContext != null)
            {
                var uploadResult = await GitHubHelper.UploadStream(
                    name: $"soa-reports/cache/{regionKey}.xlsx",
                    file: fileContext, // ✅ use fileContext here
                    githubToken: Environment.GetEnvironmentVariable("GH_PAT"),
                    repoOwner: "pacuitinfo",
                    repoName: "edge43",
                    folder: "reports",
                    branch: "main"
                );
                if (uploadResult.Success)
                    Console.WriteLine($"✅ Upload succeeded: {uploadResult.Url}");
                else
                    Console.WriteLine($"❌ Upload failed: {uploadResult.Message}");
            }









     
// ---------- OUTPUT ----------
Console.WriteLine($"Processed {processed} items.\n");
Console.WriteLine("Monthly status counts:");
foreach (var kv in monthlyStatusCounts.OrderBy(k => k.Key.Year).ThenBy(k => k.Key.Month))
{
    var ym = $"{new DateTime(2000, kv.Key.Month, 1):MMM} {kv.Key.Year}";
    var pairs = string.Join(", ", kv.Value.Select(p => $"{p.Key}:{p.Value}"));
    Console.WriteLine($"  {ym}: {pairs}");
}

var statuses = new[] { "Declined", "For Approval", "Approved", "For Evaluation" };
var days = Enumerable.Range(0, 30)
    .Select(i => DateTime.UtcNow.Date.AddDays(-29 + i))
    .ToList();

// init series
var seriesMap = statuses.ToDictionary(
    s => s,
    s => new EchartsSeries { name = s });

// fill data: proportion each day
foreach (var d in days)
{
    dailyStatusCounts.TryGetValue(d, out var dict);
    var total = dict?.Values.Sum() ?? 0;

    foreach (var s in statuses)
    {
        var count = (dict != null && dict.TryGetValue(s, out var c)) ? c : 0;
        seriesMap[s].data.Add(total > 0 ? (double)count  : 0.0);
    }
}

// attach to report
servicesReports.ChartStackedSeries.Clear();
servicesReports.ChartStackedSeries.AddRange(statuses.Select(s => seriesMap[s]));

servicesReports.ChartDataList.Clear();

// simple color rotation (feel free to change)
string[] palette = {
    "#3B82F6", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6",
    "#06B6D4", "#22C55E", "#A855F7", "#F97316", "#E11D48",
    "#14B8A6", "#0EA5E9"
};

int colorIdx = 0;

foreach (var kv in monthlyStatusCounts
            .OrderBy(k => k.Key.Year)
            .ThenBy(k => k.Key.Month))
{
    int totalForMonth = kv.Value.Values.Sum(); // sum all statuses in the month
    var label = new DateTime(kv.Key.Year, kv.Key.Month, 1).ToString("MMM yyyy");
    if (!string.IsNullOrWhiteSpace(region))
        label = $"{label} · {region}";

    var color = palette[colorIdx % palette.Length];
    colorIdx++;

    servicesReports.ChartDataList.Add(new ChartData
    {
        Label = label,
        Value = totalForMonth,
        FrontColor = color,
        // use a semi-opaque gradient variant if your UI expects it; otherwise reuse
        GradientColor = color // or $"{color}80" if your renderer supports #RRGGBBAA
    });
}


// --- Create/Update a GitHub issue with the summary payload (no '+') ---
string issueKey = "cache/" + $"{regionKey}";
string newPath = Regex.Replace(issueKey, @"T[\d:.]+Z", string.Empty);
var issueBody = JsonConvert.SerializeObject(servicesReports);
Console.Error.WriteLine(issueKey);
var result = await GitHubHelper.CreateOrUpdateIssue(newPath, issueBody);
Console.WriteLine(JsonConvert.SerializeObject(result));


 
// ===================== types (must come AFTER all top-level statements) =====================




    public class SerialNumberReason
    {
        public string? SerialNumber { get; set; }
        public string? Reason { get; set; }
    }


public class RecordModel 
{
   public string Date { get; set; }
   public string OrNo { get; set; }
   public string? Payor { get; set; }
   public string PermitToPurchase { get; set; }
   public string? FilingFeeLicense { get; set; }
   public string? PermitToPossessStorage { get; set; }
   public string? RadioStationLicense { get; set; }
   public string? RadioStationModificationFee { get; set; }
   public string? RadioOperatiorCertif { get; set; }
   public string? RadioStationLicenseArocRoc { get; set; }
   public string? PermitFee { get; set; }
   public string? ConstructionPermitFee { get; set; }
   public string? RegistrationFees { get; set; }
   public string? ClearanceCertificateFee { get; set; }
   public string? ExaminationFees { get; set; }
   public string? InspectionFeesLicense { get; set; }
   public string? InspectionFeesPermit { get; set; }
   public string? SupervisionRegulationFees { get; set; }
   public string? FilingFeePermit { get; set; }
   public string? UsageFees { get; set; }
   public string? ApplicationFeesFilingFee { get; set; }
   public string? SeminarFee { get; set; }
   public string? VerficationAuthFees { get; set; }
   public string? SurchargesLicense { get; set; }
   public string? SurchargesPermits { get; set; }
   public string? SurchargesArocRoc { get; set; }
   public string? MiscellaneousIncome { get; set; }
   public string? Other { get; set; }
   public string? DocumentaryStampTax { get; set; }
   public string? Sum { get; set; }
   public string Mpo { get; set; }
   public string RepeaterStationInspectionFee { get; set; }
}
public sealed class RepoInfo
{
    [JsonProperty("Number")] public int Number { get; set; }
    [JsonProperty("Title")]  public string? Title { get; set; }
    [JsonProperty("Body")]   public string? Body { get; set; } // JSON-as-text
    [JsonProperty("State")]  public string? State { get; set; }
    [JsonProperty("Url")]    public string? Url { get; set; }
    [JsonProperty("Labels")] public List<string>? Labels { get; set; }
}
public class Reports
{
    public List<ApplicationModel> Docs { get; set; }
    public int Total { get; set; }
    public decimal TotalSum { get; set; } 
}

public class UserModel
    {
        
        public string _id { get; set; } 
       
        [JsonProperty("firstName")]
        public string FirstName { get; set; }
        [JsonProperty("lastName")]
        public string LastName { set; get; }
        [JsonProperty("middleName")]
        public string MiddleName { set; get; }
        [JsonProperty("suffix")]
        public string Suffix { get; set; }
        
        [JsonProperty("canViewReport")]
        public bool CanViewReports { get; set; }
        [JsonProperty("canViewFees")]
        public bool CanViewFees { get; set; }
        [JsonProperty("canViewServices")]
        public bool CanViewServices { get; set; }
        
        [JsonProperty("email")]
        public string Email { set; get; }
        [JsonProperty("password")]
        public string Password { set; get; }
        [JsonProperty("contactNumber")]
        public string ContactNumber { set; get; }
        [JsonProperty("dateOfBirth")]
        public DateTime? DateOfBirth { set; get; }
        [JsonProperty("sex")]
        public string Sex { set; get; }
        [JsonProperty("nationality")]
        public string Nationality { set; get; }
        [JsonProperty("userType")]
        public string UserType { set; get; }

        [JsonProperty("address")]
        public virtual AddressModel Address { set; get; } = new AddressModel()
        {
            Barangay = null,
            City = null,
            Province = null,
            Region = null,
            Unit = null,
            ZipCode = null
        };
        
        [JsonProperty("passwordResetCode")]
        public string PasswordResetCode { set; get; }
        
        [JsonProperty("resetSendDate")]
        public DateTime ResetSendDate { set; get; }
        
        
        [JsonProperty("profilePicture")]
        public virtual ImagesModel ProfilePicture { set; get; }
        [JsonProperty("employeeDetails")]
        public virtual EmployeeModel EmployeeDetails { set; get; }
       
        [JsonProperty("isOnline")]
        public bool IsOnline { set; get; }
        [JsonProperty("lastOnline")]
        public DateTime? LastOnline { set; get; }
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { set; get; }
        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { set; get; }
        [JsonProperty("signature")]
        public virtual ImagesModel Signature { set; get; }

        public bool IsDefault { get; set; }
    }
public class PersonnelDTO
{
    public string FirstName { get; set; }
    public string LastName { set; get; }
    public string MiddleName { set; get; }
    public string Suffix { set; get; }
    public string Email { set; get; }
    public string Role { set; get; }
    public string Signature { set; get; }
}
public class ApplicationReportsExcel
{
    public List<RecordModel> Reports { get; set; }
    public string Name { get; set; }
    public string Designation { get; set; }
    public string Year { get; set; }
    public string RegionValue { get; set; }
}

public class ApplicantDTO
    {
        public string _id { get; set; }= string.Empty;
        public  string Type { set; get; }= string.Empty;
        public string UserId { get; set; }= string.Empty;
        public string UserType { get; set; }= string.Empty;
        public string CompanyName { get; set; }= string.Empty;
        public string ApplicantName { get; set; }= string.Empty;
        public string FirstName { get; set; }= string.Empty;
        public string LastName { get; set; }= string.Empty;
        public string MiddleName { get; set; }= string.Empty;
        public string Suffix { get; set; }= string.Empty;
        public string Nationality { get; set; }= string.Empty;
        public string Sex { get; set; }= string.Empty;
        public string Signature { get; set; }= string.Empty;

        public decimal Height { get; set; }= 0;
        public decimal Weight { get; set; }= 0;

       public  AddressModel Address { get; set; }
        public  ContactModel Contact { get; set; }
        public string? DateOfBirth { get; set; }= string.Empty;
        public string Email { get; set; }= string.Empty;
        public  EducationModel Education { get; set; }
        public  ImagesModel ProfilePicture { set; get; }
       
    }
public class AddressModel
    {
        public string Street { get; set; } 
        public string Unit { get; set; }
        public string Barangay { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string Region { get; set; }
        public string ZipCode { get; set; }
    }

public class ImagesModel
    {
        public string Original { get; set; }
        public string Thumb { get; set; }
        public string Small { get; set; }
        public string Medium { get; set; }
        public string Large { get; set; }
        public string Xlarge { get; set; }
    }


public class EducationModel
    {
        public string SchoolAttended { get; set; }
        public string CourseTaken { get; set; }
        public string YearGraduated { get; set; }
    }

     public class ContactModel
    {
        
        public string ContactNumber { get; set; }
        public string Email { get; set; }
    }
public class RegionDTO
{
    public string Id { get; set; }
    public string Address { get; set; }
    public string SupportEmail { get; set; }
    public string Label { get; set; }
    public string Value { get; set; }
    public string Code { get; set; }
    public ConfigurationDTO Configuration { get; set; }
}
public class ConfigurationDTO
    {
       
        public PersonnelDTO Commissioner { get; set; }
        public PersonnelDTO Director { get; set; }
        
      
    }

public class PaymentHistoryModel
    {
        public DateTime Time { set; get; }
        public string Action { set; get; }
        public string UserId { set; get; }
        public PersonnelModel Personnel { set; get; }
        public string Status { set; get; }
        public string Remarks { get; set; }
    }
    public class ApprovalHistoryModel
    {
        public DateTime Time { set; get; }
       public string Action { set; get; }
        public string UserId { set; get; }
        public PersonnelModel EndorsedTo { set; get; }
        public PersonnelModel Personnel { set; get; }
       public string Status { set; get; }
        public string Remarks { set; get; }
    }

    public class SoaModel
        {
            public string Id { set; get; } 
             [JsonProperty("Item")]   public string Item { get; set; } = "";
    [JsonProperty("Amount")] public decimal  Amount { get; set; } = 0;
        
            public string Type { set; get; }
            public string Description { set; get; }

            public string Section { get; set; }
        }
        public class SoaHistoryModel
    {
        public List<SoaModel> Soa { get; set; }
        public decimal TotalFee { set; get; }
        public string UserId { set; get; }
        public DateTime CreatedAt { set; get; }
    }

      public class ExamModel
    {
        public string Venue { set; get; }
        public DateTime Time { set; get; }
    }
    public class ORModel
    {
        public string ORNumber { get; set; }
        public string Pdf { set; get; }
        public string BankName { set; get; }
        public string CheckNumber { set; get; }
        public PersonnelModel ORBy { get; set; }
        public DateTime CreatedAt { set; get; }
    }
    public class OrderOfPaymentModel
    {
        public string? Pdf { set; get; }
        public PersonnelModel? OrderOfPaymentBy { get; set; }
        public DateTime? CreatedAt { set; get; }

        public string Number { get; set; }
    }
    public class RadioTypeModel
    {
       public string Make { get; set; }
        public string Type { get; set; }
        public string Model { get; set; }
    }
    public class ScheduleDTO
    {
        public string Id { set; get; }
        public string Venue { set; get; }
        public string Region { set; get; }
        public int Slots { set; get; }
        public string SeatNumber { set; get; }
        public DateTime? DateStart { set; get; }
        public DateTime? DateEnd { set; get; }
        public DateTime? ApplicationStartDate { set; get; }
        public DateTime? ApplicationEndDate { set; get; }
    }
    public class PaymentImagesModel
    {
        public string Original { get; set; }
        public string Thumb { get; set; }
        public string Small { get; set; }
        public string Medium { get; set; }
        public string Large { get; set; }
        public string Xlarge { get; set; }
    }
    public class PersonnelModel
    {
        public string _id { get; set; }

        public string FirstName { get; set; }

        public string LastName { set; get; }

        public string MiddleName { set; get; }

         public string Suffix { set; get; }

        public string Email { set; get; }

        public string Role { set; get; }

        public string Signature { set; get; }

        public virtual EmployeeModel EmployeeDetails { set; get; }
    }

    public class AppRow
{
    public string _id { get; set; }
    public string Type { get; set; }
    public ApplicantDTO Applicant { get; set; }
    public JToken Service { get; set; }
    public RegionDTO Region { get; set; }
    public string Status { get; set; }
    public string PaymentStatus { get; set; }
    public string PaymentMethod { get; set; }
    public string Amnesty { get; set; }
    public decimal TotalFee { get; set; }
    public string AmnestyTotalFee { get; set; }
    public PersonnelModel AssignedPersonnel { get; set; }
    public bool IsPinned { get; set; }
    public List<ApprovalHistoryModel> ApprovalHistory { get; set; }
    public List<PaymentHistoryModel> PaymentHistory { get; set; }
    public List<SoaModel> Soa { get; set; }
    public List<SoaHistoryModel> SoaHistory { get; set; }
    public ExamModel Exam { get; set; }
    public ORModel OfficialReceipt { get; set; }
    public OrderOfPaymentModel OrderOfPayment { get; set; }
    public RadioTypeModel Make { get; set; }
    public ScheduleDTO Schedule { get; set; }
    public List<PaymentImagesModel> ProofOfPayment { get; set; }
    public PersonnelModel Evaluator { get; set; }
    public PersonnelModel Cashier { get; set; }
    public PersonnelDTO Director { get; set; }
    public PersonnelDTO Commissioner { get; set; }
    public string Document { get; set; }
    public string TempDocument { get; set; }
    public string DocumentNumber { get; set; }
    public string QRCode { get; set; }
    public string Note { get; set; }
    public DateTime? DateOfExpiry { get; set; }
    public DateTime? ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string DateOfBirth { get; set; }
    public DateTime Validity { get; set; }
    public ApplicationRenewModel Renew { get; set; }
    public bool IsModified { get; set; }
    public string ReferenceNumber { get; set; }
    public string PermitNumber { get; set; }
    public ServicesReports ServicesReports { get; set; } = new();
}
public class ApplicationServicesModel 
{
    public ServicesReports ServicesReports  { get; set; }
        public string _id { get; set; }
        public string Type { set; get; }
        public virtual ApplicantDTO Applicant { set; get; }
        public dynamic Service { set; get; }
        public  RegionDTO Region { set; get; }
        public string Status { set; get; }
        public string PaymentStatus { set; get; }
        public string PaymentMethod { set; get; }
        public string Amnesty { set; get; }
        public decimal TotalFee { set; get; }
        
        public string AmnestyTotalFee { set; get; }
        public  PersonnelModel AssignedPersonnel { set; get; }
        public bool IsPinned { set; get; }
        public List<ApprovalHistoryModel> ApprovalHistory { set; get; }
        public List<PaymentHistoryModel> PaymentHistory { set; get; }
        public List<SoaModel> Soa { set; get; }
        public List<SoaHistoryModel> SoaHistory { set; get; }
        public  ExamModel Exam { set; get; }
        public ORModel OfficialReceipt { set; get; }
        public OrderOfPaymentModel? OrderOfPayment { set; get; }
        public RadioTypeModel Make { set; get; }
        public  ScheduleDTO Schedule { set; get; }
        public List<PaymentImagesModel> ProofOfPayment { set; get; }
        public  PersonnelModel Evaluator { set; get; }
        public  PersonnelModel Cashier { set; get; }
        public  PersonnelDTO Director { set; get; }
        public  PersonnelDTO Commissioner { set; get; }
        public string Document { set; get; }
        public string TempDocument { set; get; }
        public string DocumentNumber { set; get; }
        public string QRCode { set; get; }
        public string Note { set; get; }
        public DateTime? DateOfExpiry { set; get; }
        public DateTime? ValidUntil { set; get; }
        public DateTime CreatedAt { set; get; }
        public DateTime UpdatedAt { set; get; }
        public string DateOfBirth { set; get; }
        public DateTime Validity { get; set; }
        public ApplicationRenewModel Renew { get; set; }
        public bool IsModified { get; set; }
        public string ReferenceNumber { get; set; }
        public string PermitNumber { get; set; }
   
}


public class ApplicationRenewModel {
    public bool ForRenewal { get; set; }

    public bool Renewed { get; set; }

    public string RenewedFrom { get; set; }

    public virtual ApplicationTypeModel ApplicationType { set; get; }
  }


  public class ApplicationTypeModel
    {
        public string ServiceCode { get; set; }

       public string Label { get; set; }

        public string Element { get; set; }

        public List<string> Elements { get; set; }

        public string FormCode { get; set; }

        public List<RequirementModel> Requirements { get; set; }

        public string SequenceCode { get; set; }

        public List<ModificationDueToModel> ModificationDueTos { get; set; }
    }
    public class ModificationDueToModel
    {
        public string Label { get; set; }

        public string Value { get; set; }

        public List<RequirementModel> Requirements { get; set; }
    }
    public class RequirementModel
    {
        public string Key { get; set; }

        public string Title { get; set; }

        public List<RequirementImageModel> Links { get; set; }

        public string Description { get; set; }

        public bool Required { get; set; }
    }
    public class RequirementImageModel
    {
        public string Original { get; set; }
        public string Thumb { get; set; }
        public string Small { get; set; }
        public string Medium { get; set; }
        public string Large { get; set; }
        public string Xlarge { get; set; }
    }
 public class EmployeeModel
    {
        public string Region { get; set; }
       public string Level { get; set; }
        public string Title { get; set; }
        public string Division { get; set; }
        public string Position { get; set; }
        public string Designation { get; set; }
        public string Signature { set; get; }
    }
public class ChartData
{
    public int Value { get; set; }
    public string FrontColor { get; set; } = "";
    public string GradientColor { get; set; } = "";
    public string Label { get; set; } = "";
}
public class Services
{
    public int Value { get; set; }
    public string Service { get; set; } = "";     // default to avoid CS8618
    public decimal TotalFee { get; set; }         // was decimal
    public decimal Surcharge { get; set; }        // was decimal
    public List<Element>? Elements { get; set; }
    public string? Type { get; set; }
    public string? ApplicationReceive { get; set; }
}
public sealed class EchartsSeries
{
    public string name { get; set; } = "";
    public string type { get; set; } = "line";
    public string stack { get; set; } = "total";
    public string barWidth { get; set; } = "60%";
    public object label { get; set; } = new { show = false };
    public List<double> data { get; set; } = new();
}
public class ServicesReports
{
    public List<EchartsSeries> ChartStackedSeries { get; set; } = new();
    public int EnsureRow(string? name)
    {
        name ??= "unknown";
        var idx = Services.FindIndex(s => s.Service.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) { Services.Add(new Services { Service = name }); idx = Services.Count - 1; }
        return idx;
    }
     public List<ChartData> ChartDataList { get; set; } = new List<ChartData>();
    public decimal OtherDSTFee;
    public decimal AmateurAnsROCFineFee;
    public string ORNumber{ get; set; } = "";
    public string ORBy{ get; set; } = "";
    public string ORAmount{ get; set; } = "";
    public string Type { get; set; } = "";
    public string ORDate{ get; set; } = "";
    public string RCNo { get; set; } = "";
    public string Applicant { get; set; } = "";
    public string ApprovedBy { get; set; } = "";
    public string Evaluator { get; set; } = "";
    public string PermitNumber { get; set; } = "";
    public string PeriodCovered { get; set; } = "";    
    public List<Services> Services { get; set; } = new()
    {
        
        new Services()
        {
            Service = "Radio Station License",
            Value = 0,
            TotalFee = 0,
        } , new Services()
        {
            Service = "Radio Station License Aroc Roc",
            Value = 0,
            TotalFee = 0,
        }, new Services()
        {
            Service = "Radio Station License - VSAT Portable CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - VSAT Portable CP RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - VSAT LandMobile CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - VSAT LandMobile CP (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - VSAT Fixed CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - VSAT Fixed CP (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - VSAT TC CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - VSAT TC CP (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - VSAT LandBase CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - VSAT LandBase CP (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - VSAT Fixed And LandBase CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - VSAT Fixed And LandBase CP (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - VSAT Repeater CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - VSAT Repeater CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        
        new Services()
        {
            Service = "Radio Station License - Microwave Portable CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - Microwave Portable CP RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - Microwave LandMobile CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - Microwave LandMobile CP (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - Microwave Fixed CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - Microwave Fixed CP (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - Microwave LandBase CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - Microwave LandBase CP (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - Microwave Fixed And LandBase CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - Microwave Fixed And LandBase CP (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - Microwave Repeater CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - Microwave Repeater CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        
        
        
        new Services()
        {
            Service = "Private Radio Station License - Portable WDN (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Private Radio Station License - Portable WDN (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Private Radio Station License - LandMobile WDN (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Private Radio Station License - LandMobile WDN (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Private Radio Station License - Fixed WDN (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Private Radio Station License - Fixed WDN (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Private Radio Station License - LandBase WDN (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Private Radio Station License - LandBase WDN (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Private Radio Station License - Fixed and LandBase WDN (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Private Radio Station License - Fixed and LandBase WDN (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Private Radio Station License - Repeater WDN (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Private Radio Station License - Repeater WDN (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - LandBase CP WDN (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - LandBase CP WDN (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        
        
        new Services()
        {
            Service = "Private Radio Station License - LandBase PRS (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Radio Station License - LandBase PRS (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Radio Station License - LandMobile PRS (NEW)",
            Value = 0,
            TotalFee = 0,
        }, new Services()
        {
            Service = "Private Radio Station License - LandMobile PRS (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Private Radio Station License - Portable PRS (NEW)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Private Radio Station License - Portable PRS (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "NEW",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "RENEWAL",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "MODIFICATION",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Commercial Radio Operator Certificate (NEW)",
            Value = 0,
            TotalFee = 0,
            Elements =new List<Element>()
            {
                new Element()
                {
                    Name ="1RTG",
                    Value = 0
                },
                new Element()
                {
                    Name = "2RTG",
                    Value = 0
                },
                new Element()
                {
                    Name = "3RTG",
                    Value = 0
                },
                new Element()
                {
                    Name =  "1PHN",
                    Value = 0
                },
                new Element()
                {
                    Name =   "2PHN",
                    Value = 0
                },
                new Element()
                {
                    Name =    "3PHN",
                    Value = 0
                },
            }
        },
        new Services()
        {
            Service = "Commercial Radio Operator Certificate (RENEWAL)",
            Value = 0,
            TotalFee = 0,
            Elements =new List<Element>()
            {
                new Element()
                {
                    Name ="1RTG",
                    Value = 0
                },
                new Element()
                {
                    Name = "2RTG",
                    Value = 0
                },
                new Element()
                {
                    Name = "3RTG",
                    Value = 0
                },
                new Element()
                {
                    Name =  "1PHN",
                    Value = 0
                },
                new Element()
                {
                    Name =   "2PHN",
                    Value = 0
                },
                new Element()
                {
                    Name =    "3PHN",
                    Value = 0
                },
            }
        },
        new Services()
        {
            Service = "Commercial Radio Operator Certificate (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
            Elements =new List<Element>()
            {
                new Element()
                {
                    Name ="1RTG",
                    Value = 0
                },
                new Element()
                {
                    Name = "2RTG",
                    Value = 0
                },
                new Element()
                {
                    Name = "3RTG",
                    Value = 0
                },
                new Element()
                {
                    Name =  "1PHN",
                    Value = 0
                },
                new Element()
                {
                    Name =   "2PHN",
                    Value = 0
                },
                new Element()
                {
                    Name =    "3PHN",
                    Value = 0
                },
            }
        },
        new Services()
        {
            Service = "Restricted Radiotelephone Operator's Certificate - Aircraft (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Restricted Radiotelephone Operator's Certificate - Aircraft (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Restricted Radiotelephone Operator's Certificate - Aircraft (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Temporary Radio Operator Certificate for Foreign Pilot (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Temporary Radio Operator Certificate for Foreign Pilot (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Government Radio Operator Certificate (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Government Radio Operator Certificate (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Government Radio Operator Certificate (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Restricted Radiotelephone Operator's Certificate for Land Mobile Station (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Restricted Radiotelephone Operator's Certificate for Land Mobile Station (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Restricted Radiotelephone Operator's Certificate for Land Mobile Station (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Special Radio Operator Certificate (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Special Radio Operator Certificate (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Special Radio Operator Certificate (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Amateur Radio Operator Certificate (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Amateur Radio Operator Certificate (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Amateur Radio Operator Certificate (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Lifetime Amateur Radio Station Supplementary Certificate (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "For Dealers",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "For Non-Dealers",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "TVRO Registration Certificate (Commercial)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "TVRO Registration Certificate (Non-Commercial)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "RENEWAL",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Certificate of Exemption for Non-Customer Premises Equipment",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Certified True Copy",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Duplicate Copy",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radiotelegraphy",
            Value = 0,
            TotalFee = 0,
            Elements =new List<Element>()
            {
                new Element()
                {
                    Name =  "1RTG - Elements 1, 2, 5, 6 & Code (25/20 wpm)",
                    Value = 0
                },
                new Element()
                {
                    Name = "1RTG - For removal , Code (25/20 wpm)",
                    Value = 0
                },
                new Element()
                {
                    Name = "1RTG - For upgrade (2RTG Holder) & Code (25/20 wpm)",
                    Value = 0
                }, new Element()
                {
                    Name = "2RTG - Elements 1, 2, 5, 6 & Code (16 wpm)",
                    Value = 0
                },new Element()
                {
                    Name =  "2RTG - For removal, Code (16 wpm)",
                    Value = 0
                },new Element()
                {
                    Name =  "2RTG - For upgrade (3RTG Holder) , Element 6",
                    Value = 0
                },
            }
        },
        new Services()
        {
            Service = "Radiotelephony",
            Value = 0,
            TotalFee = 0,
            Elements =new List<Element>()
                {
                    new Element()
                    {
                        Name = "1PHN - Elements 1, 2, 3 & 4",
                        Value = 0
                    },
                    new Element()
                    {
                        Name = "1PHN - For upgrade (2PHN Holder) , Element 4",
                        Value = 0
                    },
                    new Element()
                    {
                        Name = "1PHN - For upgrade (3PHN Holder), Element 3 & 4",
                        Value = 0
                    },
                }
        },
        new Services()
        {
            Service = "Amateur",
            Value = 0,
            TotalFee = 0,
            Elements =new List<Element>()
            {
                new Element()
                {
                    Name = "Class A - Elements 8, 9, 10 & Code (5 wpm)",
                    Value = 0
                },
                new Element()
                {
                    Name = "Class A - For removal, Code (5 wmp)",
                    Value = 0
                },
                new Element()
                {
                    Name =  "Class B - Elements 5, 6 & 7",
                    Value = 0
                },
                new Element()
                {
                    Name = "Class B - For Registered ECE, 1PHN, 1RTG & 2RTG , Element 2",
                    Value = 0
                },
                new Element()
                {
                    Name =  "Class C - Elements 2, 3 & 4",
                    Value = 0
                },
                new Element()
                {
                    Name =   "Class C - For Class D Holder, Elements 3 & 4",
                    Value = 0
                },
                new Element()
                {
                    Name =  "Class D - Element 2",
                    Value = 0
                },
            }
        },
        new Services()
        {
            Service = "Restricted Radio Operator Certificate - Aircraft",
            Value = 0,
            TotalFee = 0,
            Elements =new List<Element>()
            {
                new Element()
                {
                    Name = "RROC - Aircraft - Element 1",
                    Value = 0
                }
            }
        },
        new Services()
        {
            Service = "Amateur Radio Station License (NEW)",
            Value = 0,
            TotalFee = 0,
            Elements =new List<Element>()
            {
                new Element()
                {
                    Name = "Class A",
                    Value = 0
                },
                new Element()
                {
                    Name = "Class B",
                    Value = 0
                },
                new Element()
                {
                    Name = "Class C",
                    Value = 0
                },
                new Element()
                {
                    Name = "Class D",
                    Value = 0
                },
            }
        },
        new Services()
        {
            Service = "Amateur Radio Station License (RENEWAL)",
            Value = 0,
            TotalFee = 0,
            Elements =new List<Element>()
            {
                new Element()
                {
                    Name = "Class A",
                    Value = 0
                },
                new Element()
                {
                    Name = "Class B",
                    Value = 0
                },
                new Element()
                {
                    Name = "Class C",
                    Value = 0
                },
                new Element()
                {
                    Name = "Class D",
                    Value = 0
                },
            }
        },
        new Services()
        {
            Service = "Amateur Radio Station License (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Lifetime Amateur Radio Station License for Class A",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Amateur Club Radio Station License (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Amateur Club Radio Station License (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Amateur Club Radio Station License (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Fixed Aeronautical Station License (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Fixed Aeronautical Station License (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Fixed Aeronautical Station License (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Aircraft Station License (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Aircraft Station License (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Aircraft Station License (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Station License DOMESTIC Trade (NEW) (WITHOUT originally-installed equipment)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Station License DOMESTIC Trade (NEW) (WITH originally-installed equipment)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Station License DOMESTIC Trade (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Station License DOMESTIC Trade (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Station License DOMESTIC Trade (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Station License INTERNATIONAL Trade (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Station License INTERNATIONAL Trade (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Earth Station License INTERNATIONAL Trade (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Earth Station License INTERNATIONAL Trade (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Station License INTERNATIONAL Trade (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Earth Station License INTERNATIONAL Trade (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Coastal Station License (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Coastal Station License (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Coastal Station License (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Public Coastal Station License (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Public Coastal Station License (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Public Coastal Station License (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Station License (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Station License (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Radio Station License - LandBase (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Radio Station License - LandBase (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Radio Station License - LandMobile (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Radio Station License - LandMobile (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Radio Station License - Portable (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Radio Station License - Portable (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        }, new Services()
        {
            Service = "Private Radio Station License - Fixed (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Radio Station License - Fixed (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Private Radio Station License - Fixed and LandBase (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Radio Station License - Fixed and LandBase (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Private Radio Station License - Repeater (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Radio Station License - Repeater (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Civic Action (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Civic Action (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Very Small Apperture Terminal (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Very Small Apperture Terminal (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Earth Station License (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Earth Station License (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Telemetry (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Telemetry (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Studio To Transmitter Link (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Studio To Transmitter Link (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Public Earth Station - Terrestrial Communication (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Public Earth Station - Terrestrial Communication (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Telemetry (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Telemetry (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Portable (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Portable (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - LandMobile (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - LandMobile (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Fixed (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Fixed (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - LandBase (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - LandBase (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Fixed And LandBase (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Fixed And LandBase (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Repeater (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Repeater (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        
        
        new Services()
        {
            Service = "Radio Station License - Portable CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Portable CP (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - LandMobile CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - LandMobile CP (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Fixed CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Fixed CP (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - LandBase CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - LandBase CP (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Fixed And LandBase CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Fixed And LandBase CP (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Repeater CP (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Repeater CP (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        
        
        
        new Services()
        {
            Service = "Private Radio Station License - Repeater (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Radio Station License - Repeater (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Repeater (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Repeater (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Certificate Of Registration - WDN (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Certificate Of Registration - WDN (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Certificate Of Registration - TVRO (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Certificate Of Registration - TVRO (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Certificate Of Registration - RFID (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Certificate Of Registration - RFID (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Certificate Of Registration - Radio (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Certificate Of Registration - Radio (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Certificate Of Exemption",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Certificate Of Exemption",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Release Clearance",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Release Clearance",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Demo/Propagate",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Demo/Propagate (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Permit To Duplicate",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Permit To Duplicate (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Permit For Modification",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Permit For Modification (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Value Added Service",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Value Added Service (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Microwave (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Microwave (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Microwave (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        }, new Services()
        {
            Service = "Radio Station License - Microwave (RENEWAL) Fixed and LandBase",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Microwave (NEW) Fixed and LandBase",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Microwave (MODIFICATION) Fixed and LandBase",
            Value = 0,
            TotalFee = 0,
        },new Services()
        {
            Service = "Radio Station License - Microwave (NEW) Fixed",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Microwave (MODIFICATION) Fixed",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - VSAT (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - VSAT (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - VSAT (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Public Trunked (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Public Trunked (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - Public Trunked (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - BWA (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - BWA (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - BWA (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - WDN (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - WDN (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - WDN (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - BTS (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - BTS (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Radio Station License - BTS (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "TVRO Station License (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "TVRO Station License (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "CATV Station License (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "CATV Station License (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Certified True Copy",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Duplicate Copy",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Amateur Radio Station Permit to PURCHASE",
            Value = 0,
            TotalFee = 0,
        }, new Services()
        {
            Service = "Amateur Radio Station Permit to PURCHASE (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Amateur Radio Station Permit to POSSESS",
            Value = 0,
            TotalFee = 0,
        }, new Services()
        {
            Service = "Amateur Radio Station Permit to POSSESS (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Amateur Radio Station Permit to PURCHASE/POSSESS",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Amateur Radio Station Permit to SELL/TRANSFER",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Temporary Permit to Operate an Amateur Radio Station - Foreign Visitor",
            Value = 0,
            TotalFee = 0,
            Elements =new List<Element>()
            {
                new Element()
                {
                    Name = "Class A",
                    Value = 0
                },
                new Element()
                {
                    Name = "Class B",
                    Value = 0
                },
                new Element()
                {
                    Name = "Class C",
                    Value = 0
                },
            }
        },
        new Services()
        {
            Service = "Special Permit for the Use of Vanity Call Sign (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Special Permit for the Use of Vanity Call Sign (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Special Permit for the Use of Special Event Call Sign",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Aeronautical Station Permit to PURCHASE",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Aeronautical Station Permit to POSSESS",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Aeronautical Station Permit to PURCHASE/POSSESS",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Station Permit to PURCHASE (DOMESTIC Trade)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Station Permit to POSSESS (DOMESTIC Trade)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Ship Station Permit to PURCHASE/POSSESS (DOMESTIC Trade)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Coastal Station Permit to PURCHASE",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Coastal Station Permit to POSSESS",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Private Coastal Station Permit to PURCHASE/POSSESS",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Public Coastal Station Permit to PURCHASE",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Public Coastal Station Permit to POSSESS",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Public Coastal Station Permit to PURCHASE/POSSESS",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Permit to PURCHASE",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Permit to POSSESS",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Permit to PURCHASE/POSSESS",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Permit to POSSESS for Storage",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Construction Permit (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Construction Permit (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Temporary Permit to Demonstrate and Propagate",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Permit to Possess for Storage (PTEs)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Permit to Transport",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Dealer Permit (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Dealer Permit (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },  
        
        new Services()
        {
            Service = "Radio Operator Certificate",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Dealer Permit (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Manufacturer Permit (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Manufacturer Permit (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Manufacturer Permit (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Service Center Permit (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Service Center Permit (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Service Center Permit (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Dealer Permit (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Dealer Permit (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Dealer Permit (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Retailer/Reseller Permit (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Retailer/Reseller Permit (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Retailer/Reseller Permit (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Service Center Permit (NEW)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Service Center Permit (RENEWAL)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Service Center Permit (MODIFICATION)",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Permit to Import for Customer Premises Equipment",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Certified True Copy",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Duplicate Copy",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Request for Blocking of IMEI and SIM of Lost/Stolen Mobile Phone",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Request for Unblocking of IMEI and SIM of Lost/Stolen Mobile Phone",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Complaint on Text Spam, Text Scam, or Illegal/Obscene/Threat/Other Similar Text Messages",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
        {
            Service = "Complaint on Services offered by Telecommunications or Broadcast Service Providers",
            Value = 0,
            TotalFee = 0,
        },
        new Services()
          {
            Service = "Request for Mandatory Tape Preservation",
            Value = 0,
            TotalFee = 0,
        },
    };

    public List<Element> Fees { get; set; } = new()
    {
        new Element()
        {
            Value = 0,
            Name = "Fixed Station Inspection Fee"
        }, 
        new Element()
        {
            Value = 0,
            Name = "LandBase Station Inspection Fee"
        }, 
        new Element()
        {
            Value = 0,
            Name = "PublicTrunked Station Inspection Fee"
        },  
        new Element()
        {
            Value = 0,
            Name = "Terrestrial Communication Station Inspection Fee"
        },  
        new Element()
        {
            Value = 0,
            Name = "Terrestrial Communication Station Inspection Fee"
        },  
        new Element()
        {
            Value = 0,
            Name = "Examination Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Certificate Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Documentary Stamp Tax"
        },
        new Element()
        {
            Value = 0,
            Name = "Filing Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Seminar Fee / Application Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Surcharge"
        },
        new Element()
        {
            Value = 0,
            Name = "Modification Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "License Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Filling Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Sell/Transfer Permit Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Purchase Permit Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Possess Permit Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Construction Permit Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Special Permit Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Inspection Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Fixed Station License Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "LandBase Station License Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "PublicTrunked Station License Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Terrestrial Communication Station License Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Repeater Station Filing Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Fixed Station Filing Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "LandBase Station Filing Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "LandMobile Station Filing Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Portable Station Filing Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Portable Station Inspection Fee"
        },
        new Element()
        {
            Value = 0,
                Name = "Repeater Station License Fee"
        },
        
        new Element()
        {
            Value = 0,
            Name = "Repeater Station Inspection Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "LandMobile Station Inspection Fee"
        }, 
        new Element()
        {
            Value = 0,
            Name = "Portable Station Inspection Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "LandMobile Station License Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Portable Station License Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Demo/PropagateFee"
        },
        new Element()
        {
            Value = 0,
            Name = "Permit To Transport Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Permit/Accreditation Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Inspection Fee (Per Year)"
        },
        new Element()
        {
            Value = 0,
            Name = "Registration Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Annual Registration Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Permit To Import Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Certificate Of Exemption"
        },
        new Element()
        {
            Value = 0,
            Name = "Spectrum User Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "First Copy"
        },
        new Element()
        {
            Value = 0,
            Name = "SUR - License Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "SUR - Spectrum User Fee"
        },
        new Element()
        {
            Value = 0,
            Name = "Surcharge"
        },
        new Element()
        {
            Value = 0,
            Name = "Other"
        }
    };

    public string ApplicationType { get; set; }
    public int NoOfYear { get; set; }
    public decimal? TotalFee { get; set; }
    public DateTime? Today { get; set; }
    public string NatureOfServiceType { get; set; }
    public string ApprovedByPosition { get; set; }
    public string EvaluatorPosition { get; set; }
    public string EvaluatorSignature { get; set; }
    public string ApprovedBySignature { get; set; }
    public string? Region { get; set; }
    public decimal OtherOtherFee { get; set; }
    public decimal OtherMiscFee { get; set; }
    public decimal OtherModiFee { get; set; }
    public decimal OtherClearanceFee { get; set; }
    public decimal OtherExaminationFee { get; set; }
    public decimal OtherVerificationFee { get; set; }
    public decimal OtherRegFee { get; set; }
    public decimal OtherSupervision { get; set; }
    public decimal AmateurFines { get; set; }
    public decimal AmateurSeminarFee { get; set; }
    public decimal AmateurApplicationFee { get; set; }
    public decimal AmateurRadioOperationLicense { get; set; }
    public decimal PermitFineFee { get; set; }
    public decimal PermitFillingFee { get; set; }
    public decimal PermitInspectionFee { get; set; }
    public decimal PermitPermitFee { get; set; }
    public decimal LicensesFinePenaltiesSurchangesFee { get; set; }
    public decimal LicensesSpectrumUserFee { get; set; }
    public decimal LicensesInspectionFee { get; set; }
    public decimal LicensesRadioStationLicense { get; set; }
    public decimal LicensesContructionPermitFee { get; set; }
    public decimal PermittoPurchase { get; set; }
    public decimal FillingFee { get; set; }
    public decimal LicensesPermittoPossessStorageFee { get; set; }
    public decimal AmateurPtp { get; set; }
    public string OtherText { get; set; }
}

public class Element
{
    private decimal? _value = 0;
    public decimal? Value { get => _value ?? 0; set => _value = value; }
    public string Name { get; set; } = "";   // <- fixes CS8618
}




public sealed class ApplicationModel
{
    [JsonProperty("type")]         public string?   Type { get; set; }
    [JsonProperty("status")]       public string?   Status { get; set; }
    [JsonProperty("updatedAt")]    public DateTime? UpdatedAt { get; set; }
    [JsonProperty("service")]      public JToken?   Service { get; set; }
    [JsonProperty("permitNumber")] public string?   PermitNumber { get; set; }
    [JsonProperty("totalFee")]     public decimal     TotalFee { get; set; }

    // NEW:
    [JsonProperty("soa")]          public List<SoaModel>? Soa { get; set; }




public string SOANumber;

        
       public string _id { get; set; }
        public   ApplicantDTO Applicant { set; get; }


        public string? ServiceName { set; get; } = "";
       public string? ApplicationTypeLabel { set; get; } = "";

       public  RegionDTO Region { set; get; }

       public string PaymentStatus { set; get; }

        public string PaymentMethod { set; get; }

        public string Amnesty { set; get; }

        public string AmnestyTotalFee { set; get; }

       public  PersonnelModel AssignedPersonnel { set; get; }
        public bool IsPinned { set; get; }

        public  List<ApprovalHistoryModel> ApprovalHistory { set; get; }

       public List<PaymentHistoryModel>? PaymentHistory { set; get; }

       public List<SoaHistoryModel> SoaHistory { set; get; }

         public  ExamModel Exam { set; get; }

         public ORModel OfficialReceipt { set; get; }
         public ServicesReports ServicesReports  { get; set; } = new();

        public OrderOfPaymentModel? OrderOfPayment { set; get; }

         public RadioTypeModel Make { set; get; }

        public  ScheduleDTO Schedule { set; get; }

         public List<PaymentImagesModel> ProofOfPayment { set; get; }

         public  PersonnelModel Evaluator { set; get; }

         public  PersonnelModel Eod { set; get; }

         public   PersonnelModel Cashier { set; get; }

         public   List<string> PersonnelIds { set; get; }

        public   List<string> PersonnelNames { set; get; }

        public   PersonnelDTO Director { set; get; }

         public   PersonnelDTO Commissioner { set; get; }

        public string Document { set; get; }

        public string TempDocument { set; get; }

          public string DocumentNumber { set; get; }

          public string QRCode { set; get; }

         public string Note { set; get; }

         public DateTime? DateOfExpiry { set; get; }

         public DateTime? ValidUntil { set; get; }

          public DateTime? DueDate { set; get; }

         public DateTime CreatedAt { set; get; }

        public string? SoaDocument { set; get; }

        public string DateOfBirth { set; get; }

         public DateTime Validity { get; set; }

          public DateTime? NotifyExpiry { get; set; }

         public ApplicationRenewModel Renew { get; set; }

        public bool IsModified { get; set; }

         public bool? IsEndorsed { get; set; } = false;

         public string ReferenceNumber { get; set; }

        public string SoaReport { get; set; }

        public string SoaReportPdf { get; set; }

         public string FormDocument { get; set; }

         public List<SerialNumberReason> Reason { get; set; } = new List<SerialNumberReason>();

         public string AccountableForm { get; set; }


}
public static class PersonExtensions
{
    public static string GetFullName(string? first, string? middle, string? last, string? suffix)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(first))  parts.Add(first.Trim());
        if (!string.IsNullOrWhiteSpace(middle)) parts.Add(middle.Trim());
        if (!string.IsNullOrWhiteSpace(last))   parts.Add(last.Trim());
        var name = string.Join(" ", parts);
        if (!string.IsNullOrWhiteSpace(suffix)) name = $"{name} {suffix.Trim()}";
        return name;
    }
}
public static class Report
{
    // ---------- MICROWAVE ----------
    public static void CPPublicCorrespondenceMicrowave(string natureOfServiceType, dynamic particular,
        ServicesReports report, int findIndex, int equipments, int years)
    {
        if (natureOfServiceType != "CP (Public Correspondence)") return;
        string key = particular?.stationClass?.ToString() switch
        {
            "P"     => Pick(report, findIndex, "Radio Station License - Microwave Portable CP (NEW)", "Radio Station License - Microwave Portable CP (RENEWAL)"),
            "ML"    => Pick(report, findIndex, "Radio Station License - Microwave LandMobile CP (NEW)", "Radio Station License - Microwave LandMobile CP (RENEWAL)"),
            "FX"    => Pick(report, findIndex, "Radio Station License - Microwave Fixed CP  (NEW)", "Radio Station License - Microwave Fixed CP (RENEWAL)"),
            "FB"    => Pick(report, findIndex, "Radio Station License - Microwave LandBase CP (NEW)", "Radio Station License - Microwave LandBase CP (RENEWAL)"),
            "FX-FB" => Pick(report, findIndex, "Radio Station License - Microwave Fixed And LandBase CP (NEW)", "Radio Station License - Microwave Fixed And LandBase CP (RENEWAL)"),
            "RT"    => Pick(report, findIndex, "Radio Station License - Microwave Repeater CP (NEW)", "Radio Station License - Microwave Repeater CP (RENEWAL)"),
            _       => ""
        };
        Bump(report, key, equipments, years);
    }

    public static void CVPrivateMicrowave(string natureOfServiceType, dynamic particular,
        ServicesReports report, int findIndex, int equipments, int years)
    {
        if (natureOfServiceType != "CV (Private)") return;
        string key = particular?.stationClass?.ToString() switch
        {
            "P"     => Pick(report, findIndex, "Radio Station License - Microwave Portable (NEW)", "Radio Station License - Microwave Fixed (RENEWAL)"),
            "ML"    => Pick(report, findIndex, "Radio Station License - Microwave LandMobile (NEW)", "Radio Station License - Microwave LandMobile (RENEWAL)"),
            "FX"    => Pick(report, findIndex, "Radio Station License - Microwave Fixed (NEW)", "Radio Station License - Microwave Fixed (RENEWAL)"),
            "FB"    => Pick(report, findIndex, "Radio Station License - Microwave CP (NEW)", "Radio Station License - Microwave CP (RENEWAL)"),
            "FX-FB" => Pick(report, findIndex, "Radio Station License - Microwave Fixed AND LandBase (NEW)", "Radio Station License - Microwave Fixed AND LandBase (RENEWAL)"),
            "RT"    => Pick(report, findIndex, "Radio Station License - Microwave Repeater (NEW)", "Radio Station License - Microwave Repeater (RENEWAL)"),
            _       => ""
        };
        Bump(report, key, equipments, years);
    }

    public static void COGovernmentMicrowave(string natureOfServiceType, dynamic particular,
        ServicesReports report, int findIndex, int equipments, int years)
    {
        if (natureOfServiceType != "CO (Government)") return;
        string key = particular?.stationClass?.ToString() switch
        {
            "P"     => Pick(report, findIndex, "Radio Station License - VSAT Portable CO (NEW)", "Radio Station License - VSAT Portable CO (RENEWAL)"),
            "ML"    => Pick(report, findIndex, "Radio Station License - VSAT CO (NEW)", "Radio Station License - VSAT LandMobile CO (RENEWAL)"),
            "FX"    => Pick(report, findIndex, "Radio Station License - VSAT Fixed CO (NEW)", "Radio Station License - VSAT Fixed CO (RENEWAL)"),
            "FB"    => Pick(report, findIndex, "Radio Station License - VSAT LandBase CO (NEW)", "Radio Station License - VSAT LandBase CO (RENEWAL)"),
            "FX-FB" => Pick(report, findIndex, "Radio Station License - VSAT LandBase and LandMobile CO (NEW)", "Radio Station License - VSAT LandBase and LandMobile CO (RENEWAL)"),
            "RT"    => Pick(report, findIndex, "Radio Station License - VSAT Repeater CO (NEW)", "Radio Station License - VSAT Repeater CO (RENEWAL)"),
            _       => ""
        };
        Bump(report, key, equipments, years);
    }

    // ---------- VSAT ----------
    public static void CPPublicCorrespondenceVSAT(string natureOfServiceType, dynamic particular,
        ServicesReports report, int findIndex, int equipments, int years)
    {
        if (natureOfServiceType != "CP (Public Correspondence)") return;
        string key = particular?.stationClass?.ToString() switch
        {
            "P"     => Pick(report, findIndex, "Radio Station License - VSAT Portable CP (NEW)", "Radio Station License - VSAT Portable CP (RENEWAL)"),
            "ML"    => Pick(report, findIndex, "Radio Station License - VSAT LandMobile CP (NEW)", "Radio Station License - VSAT LandMobile CP (RENEWAL)"),
            "FX"    => Pick(report, findIndex, "Radio Station License - VSAT Fixed CP  (NEW)", "Radio Station License - VSAT Fixed CP (RENEWAL)"),
            "FB"    => Pick(report, findIndex, "Radio Station License - VSAT LandBase CP (NEW)", "Radio Station License - VSAT LandBase CP (RENEWAL)"),
            "FX-FB" => Pick(report, findIndex, "Radio Station License - VSAT Fixed And LandBase CP (NEW)", "Radio Station License - VSAT Fixed And LandBase CP (RENEWAL)"),
            "RT"    => Pick(report, findIndex, "Radio Station License - VSAT Repeater CP (NEW)", "Radio Station License - VSAT Repeater CP (RENEWAL)"),
            _       => ""
        };
        Bump(report, key, equipments, years);
    }

    public static void CVPrivateVSAT(string natureOfServiceType, dynamic particular,
        ServicesReports report, int findIndex, int equipments, int years)
    {
        if (natureOfServiceType != "CV (Private)") return;
        string key = particular?.stationClass?.ToString() switch
        {
            "P"     => Pick(report, findIndex, "Radio Station License - Microwave Portable (NEW)", "Radio Station License - Microwave Fixed (RENEWAL)"),
            "ML"    => Pick(report, findIndex, "Radio Station License - Microwave LandMobile (NEW)", "Radio Station License - Microwave LandMobile (RENEWAL)"),
            "FX"    => Pick(report, findIndex, "Radio Station License - Microwave Fixed (NEW)", "Radio Station License - Microwave Fixed (RENEWAL)"),
            "FB"    => Pick(report, findIndex, "Radio Station License - Microwave CP (NEW)", "Radio Station License - Microwave CP (RENEWAL)"),
            "FX-FB" => Pick(report, findIndex, "Radio Station License - Microwave Fixed AND LandBase (NEW)", "Radio Station License - Microwave Fixed AND LandBase (RENEWAL)"),
            "RT"    => Pick(report, findIndex, "Radio Station License - Microwave Repeater (NEW)", "Radio Station License - Microwave Repeater (RENEWAL)"),
            _       => ""
        };
        Bump(report, key, equipments, years);
    }

    public static void COGovernmentVSAT(string natureOfServiceType, dynamic particular,
        ServicesReports report, int findIndex, int equipments, int years)
    {
        if (natureOfServiceType != "CO (Government)") return;
        string key = particular?.stationClass?.ToString() switch
        {
            "P"     => Pick(report, findIndex, "Radio Station License - Microwave Portable CO (NEW)", "Radio Station License - Microwave Portable CO (RENEWAL)"),
            "ML"    => Pick(report, findIndex, "Radio Station License - Microwave LandMobile CO (NEW)", "Radio Station License - Microwave LandMobile CO (RENEWAL)"),
            "FX"    => Pick(report, findIndex, "Radio Station License - Microwave Fixed CO (NEW)", "Radio Station License - Microwave Fixed CO (RENEWAL)"),
            "FB"    => Pick(report, findIndex, "Radio Station License - Microwave LandBase CO (NEW)", "Radio Station License - Microwave LandBase CO (RENEWAL)"),
            "FX-FB" => Pick(report, findIndex, "Radio Station License - Microwave LandBase CO (NEW)", "Radio Station License - Microwave LandBase CO (RENEWAL)"),
            "RT"    => Pick(report, findIndex, "Radio Station License - Microwave Repeater CO (NEW)", "Radio Station License - Microwave Repeater CO (RENEWAL)"),
            _       => ""
        };
        Bump(report, key, equipments, years);
    }

    // ---------- WDN ----------
    public static void CPPublicCorrespondenceWDN(string natureOfServiceType, dynamic particular,
        ServicesReports report, int findIndex, int equipments, int years)
    {
        if (natureOfServiceType != "CP (Public Correspondence)") return;
        string key = particular?.stationClass?.ToString() switch
        {
            "P"     => Pick(report, findIndex, "Radio Station License - Portable CP WDN (NEW)", "Radio Station License - Portable CP WDN (RENEWAL)"),
            "ML"    => Pick(report, findIndex, "Radio Station License - LandMobile CP WDN (NEW)", "Radio Station License - LandMobile CP WDN (RENEWAL)"),
            "FX"    => Pick(report, findIndex, "Radio Station License - Fixed CP WDN (NEW)", "Radio Station License - Fixed CP WDN (RENEWAL)"),
            "FB"    => Pick(report, findIndex, "Radio Station License - LandBase CP WDN (NEW)", "Radio Station License - LandBase CP WDN (RENEWAL)"),
            "FX-FB" => Pick(report, findIndex, "Radio Station License - Fixed And LandBase CP WDN (NEW)", "Radio Station License - Fixed And LandBase CP WDN (RENEWAL)"),
            "RT"    => Pick(report, findIndex, "Radio Station License - Repeater CP WDN (NEW)", "Radio Station License - Repeater CP WDN (RENEWAL)"),
            _       => ""
        };
        Bump(report, key, equipments, years);
    }

    public static void CVPrivateWDN(string natureOfServiceType, dynamic particular,
        ServicesReports report, int findIndex, int equipments, int years)
    {
        if (natureOfServiceType != "CV (Private)") return;
        string key = particular?.stationClass?.ToString() switch
        {
            "P"     => Pick(report, findIndex, "Private Radio Station License - Portable WDN (NEW)", "Private Radio Station License - Portable WDN (RENEWAL)"),
            "ML"    => Pick(report, findIndex, "Private Radio Station License - LandMobile WDN (NEW)", "Private Radio Station License - LandMobile WDN (RENEWAL)"),
            "FX"    => Pick(report, findIndex, "Private Radio Station License - Fixed WDN (NEW)", "Private Radio Station License - Fixed WDN (RENEWAL)"),
            "FB"    => Pick(report, findIndex, "Private Radio Station License - LandBase WDN (NEW)", "Private Radio Station License - LandBase WDN (RENEWAL)"),
            "FX-FB" => Pick(report, findIndex, "Private Radio Station License - Fixed and LandBase WDN (NEW)", "Private Radio Station License - Fixed and LandBase WDN (RENEWAL)"),
            "RT"    => Pick(report, findIndex, "Private Radio Station License - Repeater WDN (NEW)", "Private Radio Station License - Repeater WDN (RENEWAL)"),
            _       => ""
        };
        Bump(report, key, equipments, years);
    }

    public static void COGovernmentWDN(string natureOfServiceType, dynamic particular,
        ServicesReports report, int findIndex, int equipments, int years)
    {
        if (natureOfServiceType != "CO (Government)") return;
        string key = particular?.stationClass?.ToString() switch
        {
            "P"     => Pick(report, findIndex, "Radio Station License - Portable WDN (NEW)", "Radio Station License - Portable WDN (RENEWAL)"),
            "ML"    => Pick(report, findIndex, "Radio Station License - LandMobile WDN (NEW)", "Radio Station License - LandMobile WDN (RENEWAL)"),
            "FX"    => Pick(report, findIndex, "Radio Station License - Fixed WDN (NEW)", "Radio Station License - Fixed WDN (RENEWAL)"),
            "FB"    => Pick(report, findIndex, "Radio Station License - LandBase WDN (NEW)", "Radio Station License - LandBase WDN (RENEWAL)"),
            "FX-FB" => Pick(report, findIndex, "Radio Station License - Fixed And LandBase WDN (NEW)", "Radio Station License - Fixed And LandBase WDN (RENEWAL)"),
            "RT"    => Pick(report, findIndex, "Radio Station License - Repeater WDN (NEW)", "Radio Station License - Repeater WDN (RENEWAL)"),
            _       => ""
        };
        Bump(report, key, equipments, years);
    }

    // ---------- generic CP / Private / Government ----------
    public static void CPPublicCorrespondence(string natureOfServiceType, dynamic particular,
        ServicesReports report, int findIndex, int equipments, int years)
    {
        if (natureOfServiceType != "CP (Public Correspondence)") return;
        string key = particular?.stationClass?.ToString() switch
        {
            "P"     => Pick(report, findIndex, "Radio Station License - Portable CP (NEW)", "Radio Station License - Portable CP (RENEWAL)"),
            "ML"    => Pick(report, findIndex, "Radio Station License - LandMobile CP (NEW)", "Radio Station License - LandMobile CP (RENEWAL)"),
            "FX"    => Pick(report, findIndex, "Radio Station License - Fixed CP (NEW)", "Radio Station License - Fixed CP (RENEWAL)"),
            "FB"    => Pick(report, findIndex, "Radio Station License - LandBase CP (NEW)", "Radio Station License - LandBase CP (RENEWAL)"),
            "FX-FB" => Pick(report, findIndex, "Radio Station License - Fixed And LandBase CP (NEW)", "Radio Station License - Fixed And LandBase CP (RENEWAL)"),
            "RT"    => Pick(report, findIndex, "Radio Station License - Repeater CP (NEW)", "Radio Station License - Repeater CP (RENEWAL)"),
            _       => ""
        };
        Bump(report, key, equipments, years);
    }

    public static void CVPrivate(string natureOfServiceType, dynamic particular,
        ServicesReports report, int findIndex, int equipments, int years)
    {
        if (natureOfServiceType != "CV (Private)") return;
        string key = particular?.stationClass?.ToString() switch
        {
            "P"     => Pick(report, findIndex, "Private Radio Station License - Portable (NEW)", "Private Radio Station License - Portable (RENEWAL)"),
            "ML"    => Pick(report, findIndex, "Private Radio Station License - LandMobile (NEW)", "Private Radio Station License - LandMobile (RENEWAL)"),
            "FX"    => Pick(report, findIndex, "Private Radio Station License - Fixed (NEW)", "Private Radio Station License - Fixed (RENEWAL)"),
            "FB"    => Pick(report, findIndex, "Private Radio Station License - LandBase (NEW)", "Private Radio Station License - LandBase (RENEWAL)"),
            "FX-FB" => Pick(report, findIndex, "Private Radio Station License - Fixed and LandBase (NEW)", "Private Radio Station License - Fixed and LandBase (RENEWAL)"),
            "RT"    => Pick(report, findIndex, "Private Radio Station License - Repeater (NEW)", "Private Radio Station License - Repeater (RENEWAL)"),
            _       => ""
        };
        Bump(report, key, equipments, years);
    }

    public static void COGovernment(string natureOfServiceType, dynamic particular,
        ServicesReports report, int findIndex, int equipments, int years)
    {
        if (natureOfServiceType != "CO (Government)") return;
        string key = particular?.stationClass?.ToString() switch
        {
            "P"     => Pick(report, findIndex, "Radio Station License - Portable (NEW)", "Radio Station License - Portable (RENEWAL)"),
            "ML"    => Pick(report, findIndex, "Radio Station License - LandMobile (NEW)", "Radio Station License - LandMobile (RENEWAL)"),
            "FX"    => Pick(report, findIndex, "Radio Station License - Fixed (NEW)", "Radio Station License - Fixed (RENEWAL)"),
            "FB"    => Pick(report, findIndex, "Radio Station License - LandBase (NEW)", "Radio Station License - LandBase (RENEWAL)"),
            "FX-FB" => Pick(report, findIndex, "Radio Station License - Fixed And LandBase (NEW)", "Radio Station License - Fixed And LandBase (RENEWAL)"),
            "RT"    => Pick(report, findIndex, "Radio Station License - Repeater (NEW)", "Radio Station License - Repeater (RENEWAL)"),
            _       => ""
        };
        Bump(report, key, equipments, years);
    }

    private static string Pick(ServicesReports r, int idx, string whenNew, string whenRenewal)
        => (r.Services[idx].ApplicationReceive == "new") ? whenNew : whenRenewal;

    private static void Bump(ServicesReports r, string key, int equipments, int years)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        var i = r.EnsureRow(key);
        r.Services[i].Value += (1 + equipments + years);
    }
}

// ===================== GitHub helper (standalone) =====================
public sealed class GitHubIssueResult
{
    public bool Success { get; set; }
    public bool Created { get; set; }
    public bool Updated { get; set; }
    public int? IssueNumber { get; set; }
    public string? Message { get; set; }
    public string? Url { get; set; }
}

public static class GitHubHelper
{
     private static readonly HttpClient _http = new HttpClient();
    public static async Task<(bool Success, string Url, string Sha, string Path, string Message)>
        UploadStream(
            string name,
            byte[] file,
            string? githubToken = null,
            string? repoOwner = "https-multiculturaltoolbox-com",
            string? repoName = "prod",
            string folder = "files",
            string branch = "main",
            string commitMessagePrefix = "chore(upload): "
        )
    {
        githubToken ??= Environment.GetEnvironmentVariable("GH_PAT");
        repoOwner ??= Environment.GetEnvironmentVariable("RepoOwner");
        repoName ??= Environment.GetEnvironmentVariable("RepoNameRefresh");

        if (string.IsNullOrWhiteSpace(githubToken) ||
            string.IsNullOrWhiteSpace(repoOwner) ||
            string.IsNullOrWhiteSpace(repoName))
        {
            return (false, null, null, null, "Missing GitHub credentials or repo coordinates.");
        }

        if (file is null || file.Length == 0)
            return (false, null, null, null, "Empty file.");

        // Sanitize filename
        string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = $"upload_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmssfff}";

        string pathInRepo = $"{folder}/{DateTimeOffset.UtcNow:yyyyMMdd_HHmmssfff}-{safeName}";

        static string EscapeSegments(string p)
            => string.Join("/", p.Split('/').Select(Uri.EscapeDataString));

        try
        {
            var payload = new
            {
                message = $"{commitMessagePrefix}{safeName}",
                content = Convert.ToBase64String(file),
                branch
            };

            var putUrl = $"https://api.github.com/repos/{repoOwner}/{repoName}/contents/{EscapeSegments(pathInRepo)}";

            using var req = new HttpRequestMessage(HttpMethod.Put, putUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            };

            AddGhHeaders(req, githubToken, acceptJson: true);

            using var response = await _http.SendAsync(req);
            var respBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, null, null, pathInRepo, $"Upload failed: {response.StatusCode} - {respBody}");

            using var json = JsonDocument.Parse(respBody);
            var contentEl = json.RootElement.TryGetProperty("content", out var c) ? c : json.RootElement;

            string url =
                (contentEl.TryGetProperty("download_url", out var dl) ? dl.GetString()
                : contentEl.TryGetProperty("html_url", out var hu) ? hu.GetString()
                : null);

            string sha = contentEl.TryGetProperty("sha", out var shaEl) ? shaEl.GetString() : null;
            string savedPath = contentEl.TryGetProperty("path", out var pEl) ? pEl.GetString() : pathInRepo;

            return (true, url, sha, savedPath, "Upload succeeded");
        }
        catch (Exception ex)
        {
            return (false, null, null, pathInRepo, $"Exception: {ex.Message}");
        }
    }

    // ========================================
    //  Helper: Add GitHub headers
    // ========================================
    private static void AddGhHeaders(HttpRequestMessage req, string token, bool acceptJson = false)
    {
        req.Headers.Authorization = new AuthenticationHeaderValue("token", token);
        req.Headers.UserAgent.ParseAdd("BigJsonReaderUploader/1.0");
        if (acceptJson)
            req.Headers.Accept.ParseAdd("application/vnd.github+json");
    }
    public static async Task<GitHubIssueResult> CreateOrUpdateIssue(
        string title,
        string body,
        string[]? labels = null,
        string? repoName = "edge-refresh-token",
        string? githubToken = null,
        string? repoOwner = "edward1986")
    {
        githubToken ??= Environment.GetEnvironmentVariable("GH_REFRESH_PAT")
                    ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        repoOwner  ??= Environment.GetEnvironmentVariable("REPOOWNER");
        repoName   ??= Environment.GetEnvironmentVariable("REPONAMEREFRESH")
                    ?? Environment.GetEnvironmentVariable("REPONAME");

        if (string.IsNullOrWhiteSpace(githubToken)
           )
        {
            return new GitHubIssueResult { Success = false, Message = "Missing GH token/owner/repo." };
        }
         if (string.IsNullOrWhiteSpace(repoOwner)
           )
        {
             repoOwner = "edward1986";
        }
        if (string.IsNullOrWhiteSpace(repoName)
           )
        {
           repoName = "edge-refresh-token";
        }

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", githubToken);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("big-json-reader/1.0");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            // 1) Search for an existing issue by exact title
            var searchQuery = Uri.EscapeDataString($"in:title {title} repo:{repoOwner}/{repoName}");
           
            var searchUrl = $"https://api.github.com/search/issues?q={searchQuery}";
            
            var searchResp = await client.GetAsync(searchUrl);
             Console.WriteLine(JsonConvert.SerializeObject(searchResp));
            
            using var searchJson = JsonDocument.Parse(await searchResp.Content.ReadAsStringAsync());
            int? existingNumber = null;
            if (searchJson.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("title", out var t) &&
                        string.Equals(t.GetString(), title, StringComparison.Ordinal))
                    {
                        existingNumber = item.GetProperty("number").GetInt32();
                        break;
                    }
                }
            }

            if (existingNumber.HasValue)
            {
                // 2a) Update existing issue (PATCH)
                var updateUrl = $"https://api.github.com/repos/{repoOwner}/{repoName}/issues/{existingNumber.Value}";
                var updatePayload = new { body };
                var req = new HttpRequestMessage(new HttpMethod("PATCH"), updateUrl)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(updatePayload), Encoding.UTF8, "application/json")
                };
                var updResp = await client.SendAsync(req);

                return new GitHubIssueResult
                {
                    Success = updResp.IsSuccessStatusCode,
                    Updated = updResp.IsSuccessStatusCode,
                    IssueNumber = existingNumber,
                    Message = updResp.IsSuccessStatusCode ? "Issue updated" : $"Issue update failed: {(int)updResp.StatusCode} {updResp.StatusCode}",
                    Url = $"https://github.com/{repoOwner}/{repoName}/issues/{existingNumber.Value}"
                };
            }
            else
            {
               
                // 2b) Create new issue
                var createUrl = $"https://api.github.com/repos/{repoOwner}/{repoName}/issues";
                var createPayload = new { title, body, labels = labels ?? new[] { "github-cache" } };
                var createResp = await client.PostAsync(
                    createUrl,
                    new StringContent(JsonConvert.SerializeObject(createPayload), Encoding.UTF8, "application/json"));
                 Console.WriteLine(JsonConvert.SerializeObject(createResp));
                if (!createResp.IsSuccessStatusCode)
                    return new GitHubIssueResult { Success = false, Message = $"Issue create failed: {(int)createResp.StatusCode} {createResp.StatusCode}" };

                using var created = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
                var number = created.RootElement.GetProperty("number").GetInt32();
                var htmlUrl = created.RootElement.GetProperty("html_url").GetString();

                return new GitHubIssueResult
                {
                    Success = true,
                    Created = true,
                    IssueNumber = number,
                    Message = "Issue created",
                    Url = htmlUrl
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Processed {ex} items.\n");
            return new GitHubIssueResult { Success = false, Message = $"Exception: {ex.Message}" };
        }
    }
}
