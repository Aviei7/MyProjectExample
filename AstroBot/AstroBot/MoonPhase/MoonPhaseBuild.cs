using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AstroBot.MoonPhase
{
    public class MoonPhaseBuild
    {
        readonly Dictionary<string, (string header, string body)> MonthForms;
        public MoonPhaseBuild()
        {
            MonthForms = new()
            {
                { "січень", ("січні", "січня") },
                { "лютий", ("лютому", "лютого") },
                { "березень", ("березні", "березня") },
                { "квітень", ("квітні", "квітня") },
                { "травень", ("травні", "травня") },
                { "червень", ("червні", "червня") },
                { "липень", ("липні", "липня") },
                { "серпень", ("серпні", "серпня") },
                { "вересень", ("вересні", "вересня") },
                { "жовтень", ("жовтні", "жовтня") },
                { "листопад", ("листопаді", "листопада") },
                { "грудень", ("грудні", "грудня") }
            };
        }

        private async Task<HtmlNodeCollection> DownloadHtmlMoonPhase(string monthTranslit, string monthName, int year)
        {
            using var client = new HttpClient();
            var moonPhaseUrl = $"https://ru.astro-seek.com/faza-luny-kalendar-lunnyh-faz-onlayn-{monthTranslit}-{year}";
            var docMoonPhase = new HtmlDocument();

            //Скачиваем ШТМЛ
            var html = await client.GetStringAsync(moonPhaseUrl);
            docMoonPhase.LoadHtml(html);

            var table = docMoonPhase.DocumentNode.SelectSingleNode("//table[contains(@class,'moonphases')]");

            var titleNode = docMoonPhase.DocumentNode.SelectSingleNode("//title");
            var title = titleNode?.InnerText.Trim();
            var moonPhaseRows = docMoonPhase.DocumentNode.SelectNodes("//div[@id='tab1']//tr[contains(@class, 'ruka')]");
            if (moonPhaseRows == null)
            {
                Console.WriteLine("Строки с событиями не найдены");
            }
            return moonPhaseRows;
        }



        private List<PhaseResult> ParceMoonPhase(HtmlNodeCollection moonPhaseRows)
        {
            List<PhaseResult> result = new List<PhaseResult>();
            int lastDay = 0;



            foreach (var row in moonPhaseRows)
            {
                var cells = row.SelectNodes("./td");
                if (cells == null || cells.Count < 3)
                    continue;

                var phaseCell = cells[3];
                var phaseTime = cells[1];
                var phaseDay = int.Parse(phaseTime.InnerText.Trim().Split(' ')[0]);
                lastDay = phaseDay;

                var strongNodes = phaseCell.SelectNodes(".//strong");
                if (strongNodes != null && strongNodes.Count > 0)
                {
                    foreach (var s in strongNodes)
                    {
                        var phaseText = s.InnerText.Trim();
                        if (IsEclipse(phaseText))
                        {
                            //затмение
                            result.Add(new PhaseResult
                            {
                                PhaseName = NormalizePhase(phaseText), // "mooneclipse"/"suneclipse"
                                DayStart = phaseDay,
                                DayEnd = phaseDay
                            });
                        }
                        else
                        {
                            //нормальная фаза
                            GroupList(phaseText, phaseDay, result);
                        }
                    }
                }
                else
                {
                    var phaseText = phaseCell.InnerText.Trim();
                    GroupList(phaseText, phaseDay, result);
                }
            }

            if (result.Count > 0)
            {
                var lastPhaseIndex = result.FindLastIndex(r => !IsEclipse(r.PhaseName));
                if (lastPhaseIndex != -1)
                    result[lastPhaseIndex].DayEnd = lastDay;
            }
            return result;
        }

        private string FormatRanges(List<(int Start, int End)> ranges, string monthName)
        {
            if (ranges.Count == 1)
            {
                var r = ranges[0];
                if (r.Start == r.End)
                    return $"{r.Start} {monthName}";
                else
                    return $"з {r.Start} по {r.End} {monthName}";
            }

            // несколько диапазонов
            var first = ranges[0];

            string result = $"з {first.Start} по {first.End} {monthName}";

            for (int i = 1; i < ranges.Count; i++)
            {
                var r = ranges[i];
                string part = r.Start == r.End
                    ? $"{r.Start}"
                    : $"{r.Start}–{r.End}";

                result += $" та {part} {monthName}";
            }

            return result;
        }

        private string CreateFinalMessage(StringBuilder sbFinal, string HeaderMonthName, string BodyMounthName, Dictionary<string, List<(int Start, int End)>> grouped)
        {
            bool eclipseBlockStarted = false;

            sbFinal.AppendLine("<u><b>Події в місяці:</b></u>");
            sbFinal.AppendLine();
            sbFinal.AppendLine($"<b>В {HeaderMonthName} нас чекає:</b>");
            sbFinal.AppendLine();

            var orderedKeys = grouped.Keys.OrderBy(k => GetPhaseOrder(k)).ToList();

            foreach (var name in orderedKeys)
            {
                var ranges = grouped[name];
                string emoji = GetEmoji(name);

                if (IsEclipse(name))
                {
                    if (!eclipseBlockStarted)
                    {
                        sbFinal.AppendLine();
                        eclipseBlockStarted = true;
                    }
                    sbFinal.AppendLine($"{emoji}<b><i>{name} - {FormatRanges(ranges, BodyMounthName)}</i></b>{emoji}");
                }
                else
                {
                    sbFinal.AppendLine($"{emoji}<i>{name} - {FormatRanges(ranges, BodyMounthName)}</i>");
                }
            }

            return sbFinal.ToString();
        }

        private void GroupList(string phaseText, int phaseDay, List<PhaseResult> result)
        {
            string phase = NormalizePhase(phaseText);
            if (phase == "")
                return;
            var lastIndex = result.FindLastIndex(r => !IsEclipse(r.PhaseName));
            if (lastIndex == -1)
            {
                result.Add(new PhaseResult
                {
                    PhaseName = phase,
                    DayStart = phaseDay
                });
                return;
            }

            var last = result[lastIndex];

            if (last.PhaseName == phase)
                return;

            if (phase.Contains("moon", StringComparison.OrdinalIgnoreCase))
            {
                last.DayEnd = phaseDay;
            }
            else
            {
                last.DayEnd = phaseDay - 1;
            }
            // добавляем новую фазу
            result.Add(new PhaseResult
            {
                PhaseName = phase,
                DayStart = phaseDay
            });
        }

        private bool IsEclipse(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            name = name.ToLower();

            if (name.Contains("затм"))
                return true;

            if (name.Contains("eclipse"))
                return true;

            if (name.Contains("зате"))
                return true;

            return false;
        }

        private int GetPhaseOrder(string name)
        {
            if (IsEclipse(name))
                return 5;

            return name switch
            {
                var n when n.Contains("Зростаюч", StringComparison.OrdinalIgnoreCase) => 1,
                var n when n.Contains("Повн", StringComparison.OrdinalIgnoreCase) => 2,
                var n when n.Contains("Спадаюч", StringComparison.OrdinalIgnoreCase) => 3,
                var n when n.Contains("Новолун", StringComparison.OrdinalIgnoreCase) => 4,
                _ => 10
            };
        }

        private string NormalizePhase(string text)
        {
            text = text.ToLower();

            if (text.Contains("растущ"))
                return "growing";

            if (text.Contains("убывающ"))
                return "waning";

            if (text.Contains("новолу"))
                return "newmoon";

            if (text.Contains("полнолу"))
                return "fullmoon";

            if (IsEclipse(text))
                return text.Contains("солн", StringComparison.OrdinalIgnoreCase)
                    ? "suneclipse"
                    : "moneclipse";

            return "";
        }

        private string GetEmoji(string name) => name switch
        {
            "Спадаючий місяць" => "🌘",
            "Зростаючий місяць" => "🌒",
            "Новолуння" => "🌑",
            "Повний місяць" => "🌕",
            "Місячне затемнення" => "🌚",
            "Сонячне затемнення" => "🌝",
            _ => ""
        };

        public async Task<string> CreateMessageForMoonPhase(DateTime DateNow)
        {
            int year = DateNow.Year;
            int month = DateNow.Month;
            var monthName = DateNow.ToString("MMMM", new System.Globalization.CultureInfo("uk-UA"));
            var rusMonthName = DateNow.ToString("MMMM", new System.Globalization.CultureInfo("ru-RU"));
            var monthTranslit = Transliterate(rusMonthName);


            var moonPhaseRows = await DownloadHtmlMoonPhase(monthTranslit, monthName, year);
            List<PhaseResult> result = ParceMoonPhase(moonPhaseRows);
            return BuildMoonPhaseMessage(result, monthName);
        }

        private string BuildMoonPhaseMessage(List<PhaseResult> result, string monthName)
        {

            var grouped = new Dictionary<string, List<(int Start, int End)>>();

            StringBuilder sbFinal = new StringBuilder();

            var (HeaderMonthName, BodyMonthName) = GetMonthForms(monthName);

            foreach (var p in result)
            {

                string translateName = p.PhaseName.ToLower() switch
                {
                    "waning" => "Спадаючий місяць",
                    "growing" => "Зростаючий місяць",
                    "newmoon" => "Новолуння",
                    "fullmoon" => "Повний місяць",
                    "suneclipse" => "Сонячне затемнення",
                    "moneclipse" => "Місячне затемнення",
                    _ => ""
                };

                if (string.IsNullOrWhiteSpace(translateName))
                    continue;

                if (!grouped.TryGetValue(translateName, out var list))
                {
                    list = new List<(int Start, int End)>();
                    grouped[translateName] = list;
                }

                list.Add((p.DayStart, p.DayEnd));
            }
            TrimRangeEndsByPointEvents(grouped);

            var finalMessage = CreateFinalMessage(sbFinal, HeaderMonthName, BodyMonthName, grouped);

            return finalMessage;
        }

        private void TrimRangeEndsByPointEvents(Dictionary<string, List<(int Start, int End)>> grouped)
        {
            var pointDays = new HashSet<int>(
                grouped.Values
                    .SelectMany(v => v)
                    .Where(r => r.Start == r.End)
                    .Select(r => r.Start)
            );

            if (pointDays.Count == 0)
                return;
            foreach (var key in grouped.Keys.ToList())
            {
                var ranges = grouped[key];
                for (int i = 0; i < ranges.Count; i++)
                {
                    var (s, e) = ranges[i];

                    if (s < e && pointDays.Contains(e))
                    {
                        e -= 1; // исключаем якорный день
                        ranges[i] = (s, e);
                    }
                }
                grouped[key] = ranges
                    .Where(r => r.Start <= r.End)
                    .OrderBy(r => r.Start)
                    .ToList();
            }
        }

        private (string header, string body) GetMonthForms(string monthName)
        {
            monthName = monthName.ToLower();

            if (MonthForms.TryGetValue(monthName, out var forms))
                return forms;

            return (monthName, monthName);
        }

        private string Transliterate(string text)
        {
            text = text.ToLower();

            var map = new Dictionary<string, string>
            {
                ["а"] = "a",["б"] = "b",["в"] = "v",["г"] = "g",["д"] = "d",["е"] = "e",["ё"] = "yo",["ж"] = "zh",["з"] = "z",["и"] = "i",["й"] = "y",
                ["к"] = "k",["л"] = "l",["м"] = "m",["н"] = "n",["о"] = "o",["п"] = "p",["р"] = "r",["с"] = "s",["т"] = "t",["у"] = "u",["ф"] = "f",
                ["х"] = "kh",["ц"] = "ts",["ч"] = "ch",["ш"] = "sh",["щ"] = "shch",["ъ"] = "",["ы"] = "y",["ь"] = "",["э"] = "e",["ю"] = "yu",["я"] = "ya"
            };

            var sb = new StringBuilder();

            foreach (var c in text)
                sb.Append(map.ContainsKey(c.ToString()) ? map[c.ToString()] : c);

            return sb.ToString();
        }

    }
}
