using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CSN_Lab_Shell.Models;
using Microsoft.Data.Sqlite;
using System.Xml.Linq;

namespace CSN_Lab_Shell.Controllers
{
    public class CSNController : Controller
    {
        SqliteConnection sqlite;

        public CSNController()
        {
            sqlite = new SqliteConnection("Data Source=csn.db");
        }

        async Task<XElement> SQLResult(string query, string root, string nodeName)
        {
            var xml = new XElement(root);

            try
            {
                await sqlite.OpenAsync();

                using (var command = new SqliteCommand(query, sqlite))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var element = new XElement(nodeName);
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var value = await reader.GetFieldValueAsync<object>(i) ?? "";
                            element.Add(new XElement(reader.GetName(i), value));
                        }
                        xml.Add(element);
                    }
                }
            }
            finally
            {
                await sqlite.CloseAsync();
            }

            return xml;
        }

        //
        // GET: /Csn/Test
        // 
        // Testmetod som visar på hur ni kan arbeta från SQL -> XML ->
        // presentationsxml -> vyn/gränssnittet.
        // 
        // 1. En SQL-förfrågan genereras i strängformat (notera @ för att inkludera flera rader)
        // 2. Denna SQL-förfrågan skickas, tillsammans med ett rotnodsnamn och elementnamn, till SQLResult - som i sin tur skickar tillbaka en XML
        // 3. Med detta XElement-objekt kan vi sedan lägga till nya noder med Add, utföra kompletterande beräkningar och dylikt.
        // 4. XML:en skickas sedan till vårt gränssnitt (motsvarande .cshtml-fil i mappen Views -> CSN).
        public ActionResult Test()
        {
            string query = @"SELECT a.Arendenummer, s.Beskrivning, SUM(((Sluttid-starttid +1) * b.Belopp)) as Summa
                            FROM Arende a, Belopp b, BeviljadTid bt, BeviljadTid_Belopp btb, Stodform s, Beloppstyp blt
                            WHERE a.Arendenummer = bt.Arendenummer AND s.Stodformskod = a.Stodformskod
                            AND btb.BeloppID = b.BeloppID AND btb.BeviljadTidID = bt.BeviljadTidID AND b.Beloppstypkod = blt.Beloppstypkod AND b.BeloppID LIKE '%2009'
							Group by a.Arendenummer
							Order by a.Arendenummer ASC";
            XElement test = SQLResult(query, "BeviljadeTider2009", "BeviljadTid").Result;
            XElement summa = new XElement("Total",
                (from b in test.Descendants("Summa")
                 select (int)b).Sum());
            test.Add(summa);

            // skicka presentationsxml:n till vyn /Views/Csn/Test,
            // i vyn kommer vi sedan åt den genom variabeln "Model"
            return View(test);
        }

        //
        // GET: /Csn/Index
        public ActionResult Index()
        {
            return View();
        }


        //
        // GET: /Csn/Uppgift1
        public ActionResult Uppgift1()
        {
            string query = @"SELECT a.Arendenummer, u.UtbetDatum as Datum, u.UtbetStatus as Status, SUM((Sluttid-starttid+1) * b.Belopp) AS Summa
            FROM Arende a, Utbetalningsplan up, Utbetalning u, UtbetaldTid ut, UtbetaldTid_Belopp utb, Belopp b
            WHERE a.Arendenummer = up.Arendenummer AND u.UtbetPlanID = up.UtbetPlanID AND u.UtbetID = ut.UtbetID 
            AND ut.UtbetTidID = utb.UtbetaldTidID AND utb.BeloppID = b.BeloppID
            GROUP BY a.Arendenummer, u.UtbetDatum";
            XElement result = SQLResult(query, "UtbetArende", "Utbetalning").Result;
            result.Save("Result.xml");
            
            return View(result);
        }


        //
        // GET: /Csn/Uppgift2
        public ActionResult Uppgift2()
        {
            return View();
        }

        //
        // GET: /Csn/Uppgift3
        public ActionResult Uppgift3()
        {
            return View();
        }
    }
}
