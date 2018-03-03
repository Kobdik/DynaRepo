using System;
using System.Collections.Generic;
using System.Linq;
using LinqToEntityApp.EF;

namespace LinqToEntityApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Test Linq to Entities");
            Test0();
            Console.ReadLine();
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
                    //with Console ~10'000'000 (1s) and without ~ 3'600'000 ticks (360ms)
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
