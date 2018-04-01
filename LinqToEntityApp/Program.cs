using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using LinqToEntityApp.EF;

namespace LinqToEntityApp
{
    class Program
    {
        static List<Int32> timeList;

        static void Main(string[] args)
        {
            timeList = new List<int>(64);
            Console.WriteLine("Test Linq to Entities. Esc - to exit. Commands: J, M, 1, 2, 3, 4");
            ConsoleKeyInfo cki;
            do
            {
                cki = Console.ReadKey();
                switch (cki.Key)
                {
                    case ConsoleKey.J: TestJ(); break;
                    case ConsoleKey.M: TestM_Avg(33); break;
                    case ConsoleKey.D1: TestR01_Avg(33); break;
                    case ConsoleKey.D2: TestR04_Avg(33); break;
                    case ConsoleKey.D3: TestR08_Avg(33); break;
                    case ConsoleKey.D4: TestR16_Avg(33); break;
                }
            }
            while (cki.Key != ConsoleKey.Escape);
        }

        private static void TestW()
        {
            using (var context = new TestModel())
            {
                int count = 0;
                double sum_gt = 0;
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                //Первоначальная загрузка из БД
                Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
                var query = from invoice in context.Invoices
                            where invoice.Val > 0
                            orderby invoice.Dt_Invo
                            select invoice;
                //LINQ to Entities
                foreach (Invoice invoice in query)
                {
                    count++;
                    sum_gt += invoice.Val;
                    //with Console ~11'218'000 (1.1s) and without ~ 3'900'000 ticks (390ms)
                    //Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8}", count, invoice.Idn, invoice.Org, invoice.Knd, invoice.Dt_Invo, invoice.Val, invoice.Note, invoice.Lic, invoice.Pnt);
                }
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Console.WriteLine("Использовано памяти {0}", GC.GetTotalMemory(false));
                Console.WriteLine("Done W. Время {0} ms", ts.Milliseconds);
            }
        }

        private static void TestJ()
        {
            using (var context = new TestModel())
            {
                Console.WriteLine();
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                //Первоначальная загрузка из БД
                //Console.WriteLine("Выделено памяти {0}", m_lst - m_fst);
                using (FileStream wfs = new FileStream("InvoiceEF.json", FileMode.Create))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(IEnumerable<Invoice>));
                    ser.WriteObject(wfs, context.Invoices);
                }
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                //В json  140 ms
                //Console.WriteLine("Выделено памяти {0}", m_lst - m_fst);
                Console.WriteLine("Done J: Время {0} ms", ts.Milliseconds);
            }
        }

        private static void TestM_Avg(int max)
        {
            timeList.Clear();
            Console.WriteLine();
            long m_fst = GC.GetTotalMemory(false);
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            for (int i = 0; i < max; i++)
            {
                TestM(i);
            }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            long m_lst = GC.GetTotalMemory(false);
            Console.WriteLine("Done M. Среднее время {0} ms. Всего {1} sec, {2} ms", timeList.Skip(1).Average(), ts.Seconds, ts.Milliseconds);
            Console.WriteLine("Выделено памяти {0} байт", m_lst - m_fst);
            using (FileStream fs = new FileStream("TestM_EF.txt", FileMode.Create))
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

        private static void TestM(int num)
        {
            using (var context = new TestModel())
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                using (MemoryStream ms = new MemoryStream(1000000))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(IEnumerable<Invo>));
                    ser.WriteObject(ms, context.Invos);
                }
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                timeList.Add(ts.Milliseconds);
                //Console.WriteLine("Done J{0}: Время {1} ms", num, ts.Milliseconds);
            }
        }

        private static void TestR01_Avg(int max)
        {
            timeList.Clear();
            Console.WriteLine();
            long m_fst = GC.GetTotalMemory(false);
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            for (int i = 0; i < max; i++)
            {
                TestR01(i);
            }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            long m_lst = GC.GetTotalMemory(false);
            Console.WriteLine("Done R01. Среднее время {0} ms. Всего {1} sec, {2} ms", timeList.Skip(1).Average(), ts.Seconds, ts.Milliseconds);
            Console.WriteLine("Выделено памяти {0} байт", m_lst - m_fst);
            using (FileStream fs = new FileStream("TestR1_EF.txt", FileMode.Create))
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

        private static void TestR04_Avg(int max)
        {
            timeList.Clear();
            Console.WriteLine();
            for (int i = 0; i < max; i++)
            {
                TestR04(i);
            }
            Console.WriteLine("Done R4. Среднее время {0} ms.", timeList.Skip(1).Average());
            using (FileStream fs = new FileStream("TestR4_EF.txt", FileMode.Create))
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

        private static void TestR08_Avg(int max)
        {
            timeList.Clear();
            Console.WriteLine();
            for (int i = 0; i < max; i++)
            {
                TestR08(i);
            }
            Console.WriteLine("Done R8. Среднее время {0} ms.", timeList.Skip(1).Average());
            using (FileStream fs = new FileStream("TestR8_EF.txt", FileMode.Create))
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

        private static void TestR16_Avg(int max)
        {
            timeList.Clear();
            Console.WriteLine();
            for (int i = 0; i < max; i++)
            {
                TestR16(i);
            }
            Console.WriteLine("Done R16. Среднее время {0} ms.", timeList.Skip(1).Average());
            using (FileStream fs = new FileStream("TestR16_EF.txt", FileMode.Create))
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


        private static void TestR01(int num)
        {
            using (var context = new TestModel())
            {
                int count = 0;
                double sum_gt = 0;
                //context.Database.Log = Console.WriteLine;
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                //Console.WriteLine("Выделено памяти {0}", m_lst - m_fst);
                var query = from invo in context.Invos
                            where invo.Val > num
                            orderby invo.Dt_Invo
                            select invo;
                //LINQ to Entities
                foreach (Invo invo in query)
                {
                    count++;
                    sum_gt += invo.Val;
                }
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                //long m_lst = GC.GetTotalMemory(false);
                //Console.WriteLine("R {0}: Кол-во={1}, Сумма={2}. Время {3} ms.", num, count, sum_gt, ts.Milliseconds);
                //Console.WriteLine("Выделено памяти {0}", m_lst - m_fst);
                timeList.Add(ts.Milliseconds);
            }
        }

        private static void TestR04(int num)
        {
            using (var context = new TestModel())
            {
                int count = 0;
                double sum_gt = 0;
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                var query = from invo in context.InvoR04s
                            where invo.Val > 0
                            orderby invo.Dt_Invo
                            select invo;
                //LINQ to Entities
                foreach (InvoR04 invo in query)
                {
                    count++;
                    sum_gt += invo.Val;
                }
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                //Console.WriteLine("R {0}: Кол-во={1}, Сумма={2}. Время {3} ms.", num, count, sum_gt, ts.Milliseconds);
                timeList.Add(ts.Milliseconds);
            }
        }

        private static void TestR08(int num)
        {
            using (var context = new TestModel())
            {
                int count = 0;
                double sum_gt = 0;
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                var query = from invo in context.InvoR08s
                            where invo.Val > 0
                            orderby invo.Dt_Invo
                            select invo;
                //LINQ to Entities
                foreach (InvoR08 invo in query)
                {
                    count++;
                    sum_gt += invo.Val;
                }
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                //Console.WriteLine("R {0}: Кол-во={1}, Сумма={2}. Время {3} ms.", num, count, sum_gt, ts.Milliseconds);
                timeList.Add(ts.Milliseconds);
            }
        }

        private static void TestR16(int num)
        {
            using (var context = new TestModel())
            {
                int count = 0;
                double sum_gt = 0;
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                var query = from invo in context.InvoR16s
                            where invo.Val > 0
                            orderby invo.Dt_Invo
                            select invo;
                //LINQ to Entities
                foreach (InvoR16 invo in query)
                {
                    count++;
                    sum_gt += invo.Val;
                }
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                //Console.WriteLine("R {0}: Кол-во={1}, Сумма={2}. Время {3} ms.", num, count, sum_gt, ts.Milliseconds);
                timeList.Add(ts.Milliseconds);
            }
        }

        private static void Test1()
        {
            using (var context = new TestModel())
            {
                int count = 0;
                double sum_gt = 0;
                long fst = DateTime.Now.Ticks;
                //Первоначальная загрузка из БД
                Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
                var query = from invo in context.Invos
                            where invo.Val > 0
                            orderby invo.Dt_Invo
                            select invo;
                //LINQ to Entities
                foreach (Invo invo in query)
                {
                    count++;
                    sum_gt += invo.Val;
                    //вывод в консоль ~9'840'000 (984s), 
                    //лучшее время вывода без консоли 
                    //составило ~ 3'125'000 ticks (312ms)
                    //статистически ~ более 350 ms
                    //Console.WriteLine("{0} {1} {2} {3} {4}", count, invo.Idn, invo.Dt_Invo, invo.Val, invo.Note);
                }
                long lst = DateTime.Now.Ticks;
                long ts = lst - fst;
                Console.WriteLine("Done 0. Count={0}, Sum={1}. Time elapsed {2} ticks", count, sum_gt, ts);
                Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
                //На основании скорости повторного исполнения убеждаемся, что данные кэшированы
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
                // Time ~156'000 ticks
                Console.WriteLine("Done 1. Count={0}, Sum={1}. Time elapsed {2}", count, sum_gt, ts);
                //Пример группирующего запроса
                fst = DateTime.Now.Ticks;
                var groupQuery = from Invo invo in query
                                 group new { Mon = invo.Dt_Invo.Month, invo.Val }
                                 by invo.Dt_Invo.Month;

                foreach (var g in groupQuery.OrderBy(g => g.Key))
                {
                    foreach (var a in g.Take(10))
                    {
                        Console.WriteLine("{0} {1}", a.Mon, a.Val);
                    }
                    Console.WriteLine("In {0} month there are {1} rows with amount {2}.", g.Key, g.Count(), g.Sum(a => a.Val));
                }
                lst = DateTime.Now.Ticks;
                ts = lst - fst;
                // Time ~468'000 ticks, Memory ~7'265'648
                Console.WriteLine("Done 2. Time elapsed {0}.", ts);
                Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
            }
        }
    }
}
