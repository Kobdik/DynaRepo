using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqToEntityApp.EF;
using System.Runtime.Serialization.Json;

namespace LinqToEntityApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Test Linq to Entities");
            Test1J();
            Console.ReadLine();
        }

        private static void Test1W()
        {
            using (var context = new TestModel())
            {
                int count = 0;
                double sum_gt = 0;
                long fst = DateTime.Now.Ticks;
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
                long lst = DateTime.Now.Ticks;
                long ts = lst - fst;
                Console.WriteLine("Done 0. Count={0}, Sum={1}. Time elapsed {2} ticks", count, sum_gt, ts);
                Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
            }
        }

        private static void Test1J()
        {
            using (var context = new TestModel())
            {
                long fst = DateTime.Now.Ticks;
                //Первоначальная загрузка из БД
                Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
                //Лучшее время 4'654'000 ticks
                using (FileStream wfs = new FileStream("InvoiceEF.json", FileMode.Create))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(IEnumerable<Invoice>));
                    ser.WriteObject(wfs, context.Invoices);
                }
                long lst = DateTime.Now.Ticks;
                long ts = lst - fst;
                Console.WriteLine("Done 0. Time elapsed {0} ticks", ts);
                Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
            }
        }

        private static void Test1R()
        {
            using (var context = new TestModel())
            {
                int count = 0;
                double sum_gt = 0;
                long fst = DateTime.Now.Ticks;
                //Первоначальная загрузка из БД T_InvoRep - 4-х кратная реплика T_InvoCut
                Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
                /*
                var query = from invo in context.Invos
                            where invo.Val > 0
                            orderby invo.Dt_Invo
                            select invo;
                            */
                //LINQ to Entities
                foreach (Invo invo in context.Invos)
                {
                    count++;
                    sum_gt += invo.Val;
                    //R4 составило ~ 4'000'000 ticks (400ms)
                    //R8 составило ~ 6'500'000 ticks (650ms)
                    //статистически ~ более 420 ms
                    //Console.WriteLine("{0} {1} {2} {3} {4}", count, invo.Idn, invo.Dt_Invo, invo.Val, invo.Note);
                }
                long lst = DateTime.Now.Ticks;
                long ts = lst - fst;
                Console.WriteLine("Done 0. Count={0}, Sum={1}. Time elapsed {2} ticks", count, sum_gt, ts);
                Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
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
