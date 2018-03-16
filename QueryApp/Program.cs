using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Kobdik.Common;
using Kobdik.DataModule;

namespace QueryApp
{
    class Program
    {
        static DataMod dataMod = DataMod.Current();
        static List<Int32> timeList;

        static void Main(string[] args)
        {
            timeList = new List<int>(64);
            dataMod.ShowError = (message) => {
                Console.WriteLine("Error! {0}", message);
            };
            dataMod.ShowMessage = (message) => {
                Console.WriteLine(message);
            };
            Console.WriteLine("Test Linq to Objects. Esc - to exit. Commands: J, M, P, 1, 2, 3, 4");
            ConsoleKeyInfo cki;
            do
            {
                cki = Console.ReadKey();
                switch (cki.Key)
                {
                    case ConsoleKey.P: TestP(); break;
                    case ConsoleKey.J: TestJ(); break;
                    case ConsoleKey.M: TestM_Avg("InvoCut", 33); break;
                    case ConsoleKey.D1: TestQ_Avg("InvoCut", 33); break;
                    case ConsoleKey.D2: TestQ_Avg("InvoR4", 33); break;
                    case ConsoleKey.D3: TestQ_Avg("InvoR8", 33); break;
                    case ConsoleKey.D4: TestQ_Avg("InvoR16", 33); break;
                }
            }
            while (cki.Key != ConsoleKey.Escape);
        }

        public class Invo
        {
            public Invo() { }

            public Int32 Idn
            {
                get; set;
            }

            public DateTime DtInvo
            {
                get; set;
            }

            public double Val
            {
                get; set;
            }

            public string Note
            {
                get; set;
            }

        }

        public class QueryInvo : DynaQuery<Invo>
        {
            public QueryInvo(IDynaObject dynaObject) : base(dynaObject)
            {
                //select props
                AutoMapProps(3);
                //MapToCurrent("Idn", "Idn");
                MapToCurrent("Dt_Invo", "DtInvo");
                //MapToCurrent("Val", "Val");
                //MapToCurrent("Note", "Note");
            }

            public override void OnReset(string message)
            {
                //Console.WriteLine(message);
            }

        }

        static void TestP()
        {
            IDynaObject dynaObject = dataMod.GetDynaObject("Invoice");
            using (FileStream wfs = new FileStream("Stored_Procs.txt", FileMode.Create))
            {
                StreamWriter sw = new StreamWriter(wfs);
                sw.WriteLine(dynaObject.GetInfo("create"));
                sw.WriteLine(dynaObject.GetInfo("select"));
                sw.WriteLine(dynaObject.GetInfo("detail"));
                sw.WriteLine(dynaObject.GetInfo("insert"));
                sw.WriteLine(dynaObject.GetInfo("update"));
                sw.Close();
            }
        }

        static void TestJ()
        {
            Console.WriteLine();
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            IDynaObject dynaObject = dataMod.GetDynaObject("Invoice");
            using (FileStream rfs = new FileStream("Invoice_Params.json", FileMode.Open))
            {
                //считываем параметры запроса из входного json-потока
                dynaObject.ReadPropStream(rfs, "sel");
            }
            using (FileStream wfs = new FileStream("Invoice.json", FileMode.Create))
            {
                dynaObject.SelectToStream(wfs);
            }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            //вся таблица выгружается в json-файл размером 552Kb за 44 ms,
            //в 3.2 раз быстрее, чем EF читает и пишет данные в поток
            Console.WriteLine("Done J: Время {0} ms", ts.Milliseconds);
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
            //Время 12 ms на чтение и запись в поток
            //в 2.4 раз быстрее, чем EF только читает
            //в 7.5 раз быстрее, чем EF только читает и пишет
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
            IDynaObject dynaObject = dataMod.GetDynaObject(queryName);
            dynaObject.SelectToStream(null);
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            //long i_len = dynaObject.StreamWriter.Result.Length;
            //Console.WriteLine("Done M{0}: Время {1} ms, размер {2}", num, ts.Milliseconds, i_len);
            timeList.Add(ts.Milliseconds);
        }

        static void TestQ_Avg(string queryName, int max)
        {
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
            using (FileStream fs = new FileStream("TestQ_DL.txt", FileMode.Create))
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

        static void TestQ(string queryName, int num)
        {
            IDynaObject dynaObject = dataMod.GetDynaObject(queryName);
            QueryInvo queryInvo = new QueryInvo(dynaObject);
            int count = 0;
            double sum_gt = 0;
            //long m_fst = GC.GetTotalMemory(false);
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            var query =
                from invo in queryInvo
                where invo.Val > num
                orderby invo.DtInvo
                select invo;
            //LINQ to Objects
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
        }


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

        static void Test4()
        {
            IDynaObject dynaObject = dataMod.GetDynaObject("InvoCut");
            if (dynaObject == null) {
                Console.WriteLine(dataMod.lastError);
                return;
            }
            QueryInvo queryInvo = new QueryInvo(dynaObject);
            var query = 
                from invo in queryInvo
                where invo.DtInvo < DateTime.Parse("31.12.2012")
                orderby invo.DtInvo
                select invo;
            int count = 0;
            DateTime fst = DateTime.Now;
            foreach (Invo invo in query)
            {
                Console.WriteLine("{0} {1} {2} {3} {4}", count++, invo.Idn, invo.DtInvo, invo.Val, invo.Note);
            }
            DateTime lst = DateTime.Now;
            TimeSpan ts = lst - fst;
            Console.WriteLine("Done 0. Count={0}, Time elapsed {1}.", count, ts.Milliseconds);
            Invo first = query.First();
            Console.WriteLine("{0} {1} {2} {3}", first.Idn, first.DtInvo, first.Val, first.Note);
            first.DtInvo = DateTime.Parse("31.12.2012");
            first.Val = -1500;
            first.Note = "Данные изменены !";
            queryInvo.Update(first);
            Console.WriteLine(dynaObject.GetInfo("props"));
            Console.WriteLine("First Invoice {0} {1} {2} {3}", first.Idn, first.DtInvo, first.Val, first.Note);
            
        }

    }

}
