using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AstroBot.RetroPlanet
{
    public class RetroPlanetBuild
    {
        private readonly Dictionary<string, string> MonthMap = new()
        {
            ["янв"] = "Jan",["фев"] = "Feb",["мар"] = "Mar",["апр"] = "Apr",["май"] = "May",
            ["мая"] = "May",["июн"] = "Jun",["июля"] = "Jul",["июл"] = "Jul",["авг"] = "Aug",
            ["сен"] = "Sep",["сент"] = "Sep",["окт"] = "Oct",["ноя"] = "Nov",["дек"] = "Dec"
        };

        private readonly Dictionary<string, string> PlanetName = new()
        {
            ["merkur"] = "Меркурій",
            ["venuse"] = "Венера",
            ["mars"] = "Марс",
            ["jupiter"] = "Юпітер",
            ["saturn"] = "Сатурн",
            ["uran"] = "Уран",
            ["neptun"] = "Нептун",
            ["pluto"] = "Плутон"

        };

        private readonly Dictionary<string, (string header, string body)> MonthForms = new() {
            { "січень",     ("січень", "січня") },
            { "лютий",      ("лютий", "лютого") },
            { "березень",   ("березень", "березня") },
            { "квітень",    ("квітень", "квітня") },
            { "травень",    ("травень", "травня") },
            { "червень",    ("червень", "червня") },
            { "липень",     ("липень", "липня") },
            { "серпень",    ("серпень", "серпня") },
            { "вересень",   ("вересень", "вересня") },
            { "жовтень",    ("жовтень", "жовтня") },
            { "листопад",   ("листопад", "листопада") },
            { "грудень",    ("грудень", "грудня") }
        };

        private readonly HashSet<string> planetKeys = new HashSet<string> { "merkur", "venuse", "mars", "jupiter", "saturn", "uran", "neptun", "pluto", "uzel", "lilith", "chiron" };

        //public async Task<string> CreateMessageForRetroPlanet(DateTime Nowdate, List<RetroPlanetResult> retroPlanetResult)

        private string GetMonthForm(string monthName)
        {
            monthName = monthName.ToLower();

            if (MonthForms.TryGetValue(monthName, out var forms))
                return forms.body;   

            return monthName;        
        }


        public async Task<string> CreateMessageForRetroPlanet(DateTime now)
        {
            var years = new List<int> { now.Year, now.Year - 1 };
            if (now.Month == 12) years.Add(now.Year + 1);

            var all = new List<RetroPlanetResult>();

            foreach (var y in years)
            {
                var doc = await DownloadHTMLDoc(y);
                all.AddRange(ParseRetroFromDoc(doc, now)); 
            }

            var merged = MergePeriods(all);

            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var forMonth = merged
                .Where(p => p.DateStart <= monthEnd && p.DateEnd >= monthStart);

            return BuildRetroPlanetMessage(forMonth.ToList());
        }

        //public async Task<string> CreateMessageForRetroPlanet(DateTime Nowdate)
        //{
        //    List<RetroPlanetResult> retroPlanetResult = new List<RetroPlanetResult>();
        //    var retroDoc = await DownloadHTMLDoc();

        //    var container = retroDoc.DocumentNode.SelectSingleNode("//div[@id='div_items_id' and contains(@class,'detail-rozbor-items')]");

        //    //CurrentPlanet
        //    string currentPlanet = null;

        //    //IgnoreList
        //    string[] ignoredPlanet = { "uzel", "lilith", "chiron" };

        //    foreach (var node in container.ChildNodes)
        //    {

        //        if (node.NodeType != HtmlNodeType.Element || node.Name != "div")
        //            continue;

        //        string planet = GetPlanetName(node);
        //        if (planet != null)
        //        {
        //            currentPlanet = planet;
        //        }


        //        //пропускаем если это не планета
        //        if (currentPlanet == null || ignoredPlanet.Any(x => currentPlanet.Contains(x)))
        //            continue;


        //        // Ретро?
        //        if (!IsRetroBlock(node))
        //            continue;


        //        var period = ExtractRetroPeriod(node);
        //        if (period == null)
        //            continue;

        //        AddRetroPeriod(currentPlanet, Nowdate, period.StartRaw, period.EndRaw, retroPlanetResult);
        //    }

        //    var finalMessage = BuildRetroPlanetMessage(retroPlanetResult);

        //    return finalMessage;
        //}




        private List<RetroPlanetResult> ParseRetroFromDoc(HtmlDocument retroDoc, DateTime nowDate)
        {
            var retroPlanetResult = new List<RetroPlanetResult>();

            var container = retroDoc.DocumentNode.SelectSingleNode("//div[@id='div_items_id' and contains(@class,'detail-rozbor-items')]");

            //CurrentPlanet
            string currentPlanet = null;

            //IgnoreList
            string[] ignoredPlanet = { "uzel", "lilith", "chiron" };

            foreach (var node in container.ChildNodes)
            {

                if (node.NodeType != HtmlNodeType.Element || node.Name != "div")
                    continue;

                string planet = GetPlanetName(node);
                if (planet != null)
                {
                    currentPlanet = planet;
                }


                //пропускаем если это не планета
                if (currentPlanet == null || ignoredPlanet.Any(x => currentPlanet.Contains(x)))
                    continue;


                // Ретро?
                if (!IsRetroBlock(node))
                    continue;


                var period = ExtractRetroPeriod(node);
                if (period == null)
                    continue;

                AddRetroPeriod(currentPlanet, nowDate, period.StartRaw, period.EndRaw, retroPlanetResult);
            }

            return retroPlanetResult;

        }

        private string BuildRetroPlanetMessage(List<RetroPlanetResult> RetroPlanetList)
        {
            StringBuilder sbFinal = new StringBuilder();

            var filteredRetroPlanet = RetroPlanetList.Where(e => e.IsCurrentMounth == true);

            foreach (var rp in filteredRetroPlanet)
            {
                var monthStart = GetMonthForm(rp.DateStart.ToString("MMMM", new CultureInfo("uk-UA")));
                var monthEnd = GetMonthForm(rp.DateEnd.ToString("MMMM", new CultureInfo("uk-UA")));
                var nomalizePlanetName = NormalizePlanetName(rp.PlanetName);

                sbFinal.AppendLine($"<blockquote> <b>Ретроградний {nomalizePlanetName} - з {rp.DateStart.Day} {monthStart} по {rp.DateEnd.Day} {monthEnd} {rp.DateEnd.Year} року</b> </blockquote>");
            }

            return sbFinal.ToString();
        }

        private string NormalizePlanetName(string planetName)
        {
            if (PlanetName.TryGetValue(planetName, out var normalName))
                return normalName;

            return planetName;
        }

        private string GetPlanetName(HtmlNode node)
        {
            var anchor = node.SelectSingleNode(".//a[@name]");
            if (anchor == null)
                return null;

            var name = anchor.GetAttributeValue("name", "").Trim();

            return planetKeys.Contains(name) ? name : null;
        }

        private bool IsRetroBlock(HtmlNode node) => node.InnerText.Trim().Equals("Ретро", StringComparison.OrdinalIgnoreCase);

        private RetroPeriod ExtractRetroPeriod(HtmlNode retroNode)
        {
            var start = FindNextDateNode(retroNode);
            var end = start != null ? FindNextDateNode(start) : null;
            var mid = end != null ? FindNextDateNode(end) : null;

            if (start == null || end == null)
                return null;

            return new RetroPeriod
            {
                StartRaw = HtmlEntity.DeEntitize(start.InnerText.Trim()),
                EndRaw = HtmlEntity.DeEntitize(end.InnerText.Trim())
            };
        }

        private async Task<HtmlDocument> DownloadHTMLDoc(int year)
        {
            using var client = new HttpClient();
            var retroPlanetUrl = $"https://ru.astro-seek.com/retrogradnye-planety-astro-kalendar-{year}";
            var docRetroOkanet = new HtmlDocument();
            var html = await client.GetStringAsync(retroPlanetUrl);
            docRetroOkanet.LoadHtml(html);

            return docRetroOkanet;
        }


        private List<RetroPlanetResult> MergePeriods(List<RetroPlanetResult> items)
        {
            return items
                .GroupBy(x => x.PlanetName)
                .SelectMany(g =>
                {
                    var sorted = g.OrderBy(x => x.DateStart).ToList();
                    var result = new List<RetroPlanetResult>();

                    foreach (var p in sorted)
                    {
                        if (result.Count == 0)
                        {
                            result.Add(p);
                            continue;
                        }

                        var last = result[^1];

                        if (p.DateStart <= last.DateEnd.AddDays(1))
                        {
                            last.DateEnd = (p.DateEnd > last.DateEnd) ? p.DateEnd : last.DateEnd;
                        }
                        else
                        {
                            result.Add(p);
                        }
                    }

                    return result;
                })
                .ToList();
        }

        private HtmlNode FindNextDateNode(HtmlNode node)
        {
            var cur = node.NextSibling;
            while (cur != null)
            {
                if (cur.NodeType == HtmlNodeType.Element && cur.Name == "div" && cur.GetAttributeValue("class", "").Contains("datum_odkaz_horoskop"))
                {
                    return cur;
                }
                cur = cur.NextSibling;
            }
            return null;
        }

        private string ExtractDate(string raw)
        {
            if (raw == null) return null;

            var parts = raw.Split(',');

            return $"{parts[0].Trim()} {parts[1].Trim()}";
        }

        private void AddRetroPeriod(string currentPlanet, DateTime now, string startRaw, string endRaw, List<RetroPlanetResult> retroPlanetResult)
        {

            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            string startClean = ExtractDate(startRaw);   
            string endClean = ExtractDate(endRaw);

            startClean = NormalizeMonth(startClean);   
            endClean = NormalizeMonth(endClean);

            DateTime start = DateTime.ParseExact(startClean, "d MMM yyyy", CultureInfo.InvariantCulture);
            DateTime end = DateTime.ParseExact(endClean, "d MMM yyyy", CultureInfo.InvariantCulture);

            retroPlanetResult.Add(new RetroPlanetResult()
            {
                PlanetName = currentPlanet,
                DateStart = start,
                DateEnd = end,
                IsCurrentMounth = start <= monthEnd && end >= monthStart
            });
        }

        private string NormalizeMonth(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return raw;

            var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                return raw;

            string day = parts[0];
            string monthRaw = parts[1].ToLower();
            string year = parts[2];

            // если месяц есть в словаре — меняем
            if (MonthMap.TryGetValue(monthRaw[..3], out var eng))
                return $"{day} {eng} {year}";

            return raw; 
        }
    }
    }
