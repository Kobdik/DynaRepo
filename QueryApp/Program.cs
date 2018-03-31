using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Kobdik.Common;
using Kobdik.DataModule;

namespace QueryApp
{
    class Program
    {
        static DataMod dataMod = DataMod.Current();
        static string connString = @"Data Source=(LocalDb)\MSSQLLocalDb;Integrated Security=True;initial catalog=test;";
        static List<Int32> timeList;
        static List<double> valList;

        static void Main(string[] args)
        {
            timeList = new List<int>(64);
            valList = new List<double>(64);
            //connString = ConfigurationManager.AppSettings["Conn"];
            dataMod.ShowError = (message) => {
                Console.WriteLine("Error! {0}", message);
            };
            dataMod.ShowMessage = (message) => {
                Console.WriteLine(message);
            };
            //dataMod.GetConnection = new Connection(GetSqlConnection);
            dataMod.GetConnection = () => new SqlConnection(connString);
            dataMod.LoadMeta();
            Console.WriteLine("Meta loaded.");
            Console.WriteLine("Test Linq to Objects. Esc - to exit. Commands: J, M, P, Q, U, V");
            ConsoleKeyInfo cki;
            do
            {
                cki = Console.ReadKey();
                switch (cki.Key)
                {
                    case ConsoleKey.P: TestP(); break;
                    case ConsoleKey.J: TestJ(); break;
                    case ConsoleKey.M: TestM_Avg("InvoCut", 33); break;
                    case ConsoleKey.Q: TestQ_Avg("InvoCut", 33); break;
                    case ConsoleKey.V: TestV(); break;
                    case ConsoleKey.U: TestU(); break;
                }
            }
            while (cki.Key != ConsoleKey.Escape);
        }

        public class Invo
        {
            public Invo() { }

            public Int32 Idn { get; set; }

            public DateTime Dt_Invo { get; set; }

            public double Val { get; set; }

            public string Note { get; set; }

        }

        public class QueryInvo : DynaQuery<Invo>
        {
            public QueryInvo(IDynaRecord dynaRecord) : base(dynaRecord)
            {
                //MapToCurrent("Idn", "Idn");
                //MapToCurrent("Dt_Invo", "DtInvo");
                //MapToCurrent("Val", "Val");
                //MapToCurrent("Note", "Note");
                AutoMapProps(CmdBit.Sel);
            }

            public override void OnReset(string message)
            {
                //Console.WriteLine(message);
            }

        }

        static void TestP()
        {
            IDynaRecord dynaRecord = dataMod.GetDynaRecord("Invoice");
            using (FileStream wfs = new FileStream("Stored_Procs.txt", FileMode.Create))
            {
                StreamWriter sw = new StreamWriter(wfs);
                sw.WriteLine(dynaRecord.GetInfo("create"));
                sw.WriteLine(dynaRecord.GetInfo("select"));
                sw.WriteLine(dynaRecord.GetInfo("detail"));
                sw.WriteLine(dynaRecord.GetInfo("insert"));
                sw.WriteLine(dynaRecord.GetInfo("update"));
                sw.Close();
            }
        }


        static void TestJ()
        {
            Console.WriteLine();
            long m_fst = GC.GetTotalMemory(false);
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            IDynaRecord dynaRecord = dataMod.GetDynaRecord("InvoCut");
            /*
            using (FileStream rfs = new FileStream("Invoice_Params.json", FileMode.Open))
            {
                //считываем параметры запроса из входного json-потока
                dynaRecord.ReadPropStream(rfs, "sel");
            }
            */
            using (Stream wfs = new FileStream("InvoCut.json", FileMode.Create), bfs = new BufferedStream(wfs, 4 * 1024))
            {
                dynaRecord.SelectToStream(bfs, CommandBehavior.SequentialAccess);
            }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            long m_lst = GC.GetTotalMemory(false);
            //вся таблица выгружается в json-файл размером 381Kb за 18 - 27 ms (TextStreamWriter), лучшее 15 ms
            //вся таблица выгружается в json-файл размером 381Kb за 35 - 40 ms (JsonStreamWriter),
            //Core EF читает и пишет данные в поток за 78 - 90 ms
            Console.WriteLine("Done J: Время {0} ms", ts.Milliseconds);
            Console.WriteLine("Выделено памяти {0} байт", m_lst - m_fst);
        }

        static void TestM_Avg(string queryName, int max)
        {
            timeList.Clear();
            Console.WriteLine();
            long m_fst = GC.GetTotalMemory(false);
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            for (int i = 0; i < max; i++)
            {
                TestM(queryName, i);
            }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            long m_lst = GC.GetTotalMemory(false);
            //Время 12.5 ms на чтение и запись в поток (TextStreamWriter)
            //Время 12.8 ms на чтение и запись в поток (JsonStreamWriter)
            //в 2.4 раз быстрее, чем EF только читает
            //Core EF читает и пишет за 76 ms
            Console.WriteLine("Done M. Среднее время {0} ms. Всего {1} sec, {2} ms", timeList.Skip(1).Average(), ts.Seconds, ts.Milliseconds);
            Console.WriteLine("Выделено памяти {0} байт", m_lst - m_fst);
            using (FileStream fs = new FileStream("TestM_DL.txt", FileMode.Create))
            {
                StreamWriter sr = new StreamWriter(fs);
                foreach (int t in timeList)
                {
                    sr.WriteLine("{0}", t);
                }
                sr.Flush();
                sr.Close();
            }
        }

        static void TestM(string queryName, int num)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            IDynaRecord dynaRecord = dataMod.GetDynaRecord(queryName);
            using (MemoryStream ms = new MemoryStream(1000000))
                dynaRecord.SelectToStream(ms, CommandBehavior.SequentialAccess);
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            //long i_len = dynaObject.StreamWriter.Result.Length;
            //Console.WriteLine("Done M{0}: Время {1} ms, размер {2}", num, ts.Milliseconds, i_len);
            timeList.Add(ts.Milliseconds);
        }

        static void TestQ_Avg(string queryName, int max)
        {
            valList.Clear();
            timeList.Clear();
            Console.WriteLine();
            long m_fst = GC.GetTotalMemory(false);
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            for (int i=0; i<max; i++)
            {
                TestQ(queryName, i);
            }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            long m_lst = GC.GetTotalMemory(false);
            Console.WriteLine("Done Q. Среднее время {0} ms. Всего {1} sec, {2} ms", timeList.Skip(1).Average(), ts.Seconds, ts.Milliseconds);
            Console.WriteLine("Выделено памяти {0} байт", m_lst - m_fst);
            using (FileStream fs = new FileStream("TestQ_DL.csv", FileMode.Create))
            {
                StreamWriter sr = new StreamWriter(fs);
                sr.WriteLine("Query: {0}", queryName);
                for (int i=0; i < max; i++)
                {
                    sr.WriteLine("{0};{1}", timeList[i], valList[i]);
                }
                sr.Flush();
                sr.Close();
            }
        }

        static void TestQ(string queryName, int num)
        {
            int count = 0;
            double sum_gt = 0;
            //long m_fst = GC.GetTotalMemory(false);
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            IDynaRecord dynaRecord = dataMod.GetDynaRecord(queryName);
            QueryInvo queryInvo = new QueryInvo(dynaRecord);
            //LINQ to Objects
            var query =
                from invo in queryInvo
                where invo.Val > num
                orderby invo.Dt_Invo
                select invo;
            //Iterate
            foreach (Invo invo in query)
            {
                count++;
                sum_gt += invo.Val;
            }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            //long m_lst = GC.GetTotalMemory(false);
            //Console.WriteLine("Q {0}: Кол-во={1}, Сумма={2}. Время {3} ms.", num, count, sum_gt, ts.Milliseconds);
            //Console.WriteLine("Выделено памяти {0}", m_lst - m_fst);
            timeList.Add(ts.Milliseconds);
            valList.Add(sum_gt);
        }

        static void TestV()
        {
            Console.WriteLine();
            IDynaRecord dynaRecord = dataMod.GetDynaRecord("Invo");
            dynaRecord.FieldDict["Dt_Fst"].Value = "2017.04.01";
            dynaRecord.FieldDict["Dt_Lst"].Value = "2017.04.05";
            dynaRecord.FieldDict["Note"].Value = @"
Апрель! Апрель!
На дворе звенит капель.
По полям бегут ручьи,
На дорогах лужи.";
            if (dynaRecord == null)
            {
                Console.WriteLine(dataMod.lastError);
                return;
            }
            IDataCommand dynaCmd = dynaRecord as IDataCommand;
            var out_fields = dynaCmd.Action("c16");
            Console.WriteLine("Done V. Rows affected={0}.", dynaCmd.Rows_Affected);
            foreach(var field in out_fields)
                Console.WriteLine("{0} : {1}", field.GetName(), field.Value);
        }

        static void TestU()
        {
            Console.WriteLine();
            IDynaRecord dynaRecord = dataMod.GetDynaRecord("Invo");
            dynaRecord.FieldDict["Dt_Fst"].Value = "2017.04.01";
            dynaRecord.FieldDict["Dt_Lst"].Value = "2017.04.10";
            if (dynaRecord == null)
            {
                Console.WriteLine(dataMod.lastError);
                return;
            }
            QueryInvo queryInvo = new QueryInvo(dynaRecord);
            var query =
                from invo in queryInvo
                where invo.Val > 0
                orderby invo.Dt_Invo
                select invo;
            int count = 0;
            foreach (Invo invo in query)
            {
                Console.WriteLine("{0} {1} {2} {3} {4}", count++, invo.Idn, invo.Dt_Invo, invo.Val, invo.Note);
            }
            Console.WriteLine("Done U. Count={0}", count);
            Invo first = query.First();
            Console.WriteLine("Исход.: {0} {1} {2} {3}", first.Idn, first.Dt_Invo, first.Val, first.Note);
            first.Dt_Invo = DateTime.Parse("07.04.2017");
            first.Val = -1500;
            first.Note = "Попытка изменения данных";
            queryInvo.Action(first, "upd");
            Console.WriteLine("Измен.: {0} {1} {2} {3}", first.Idn, first.Dt_Invo, first.Val, first.Note);
            Console.WriteLine(dynaRecord.GetInfo("fields"));
        }


        /*
                static void Test3()
                {
                    IDynaObject dynaObject = dataMod.GetDynaObject("InvoCut");
                    //запрос без параметров
                    QueryInvo queryInvo = new QueryInvo(dynaObject);
                    int count = 0;
                    double sum_gt = 0;
                    long fst = DateTime.Now.Ticks;
                    Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
                    var query = 
                        from invo in queryInvo
                        where invo.Val > 0
                        orderby invo.DtInvo
                        select invo;
                    //LINQ to Objects
                    foreach (Invo invo in query) 
                    {
                        count++;
                        sum_gt += invo.Val;
                        //with Console ~6'800'000 ticks (680ms)
                        //статистически ~  312'000 tikcs ( 31ms)
                        //лучшее время 156'000 ticks (16ms)
                        //Console.WriteLine("{0} {1} {2} {3} {4}", 
                        // count, invo.Idn, invo.DtInvo, invo.Val, invo.Note);
                    }
                    long lst = DateTime.Now.Ticks;
                    long ts = lst - fst;
                    Console.WriteLine("Done 0. Count={0}, Sum={1}. Time elapsed {2} ticks.", count, sum_gt, ts);
                    Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));

                    count = 0;
                    sum_gt = 0;
                    fst = DateTime.Now.Ticks;
                    foreach (Invo invo in query)
                    {
                        count++;
                        sum_gt += invo.Val;
                    }
                    lst = DateTime.Now.Ticks;
                    ts = lst - fst;
                    // ~0 ticks
                    Console.WriteLine("Done 1. Count={0}, Sum={1}. Time elapsed {2}", 
                        count, sum_gt, ts);

                    fst = DateTime.Now.Ticks;
                    //IGrouping<int, <Invo>>
                    var groupQuery = 
                        from Invo invo in query
                        group new { Mon = invo.DtInvo.Month, invo.Val }
                        by invo.DtInvo.Month;

                    foreach (var g in groupQuery.OrderBy(g => g.Key))
                    {
                        foreach (var a in g.Take(10))
                        {
                            Console.WriteLine("{0} {1}", a.Mon, a.Val);
                        }
                        Console.WriteLine("In {0} month there are {1} rows with amount {2}.", 
                            g.Key, g.Count(), g.Sum(a => a.Val));
                    }
                    lst = DateTime.Now.Ticks;
                    ts = lst - fst;
                    // ~312'500 ticks Memory 2'072'780
                    Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
                    Console.WriteLine("Done 2. Time elapsed {0}.", ts);
                }

                */
    }

}
