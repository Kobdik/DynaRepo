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

        private static void Test0()
        {
            using (var context = new TestModel())
            {
                int count = 0;
                double sum_gt = 0;
                long fst = DateTime.Now.Ticks;
                
                Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
                var query = from nach in context.Invos
                            where nach.Val > 0
                            orderby nach.Dt_Invo
                            select nach;
                foreach (Invo invo in query) //.OrderBy(n => n.Dt_Nach).Select(n => new Nach(n.INN, n.Dt_Nach, n.Val)).ToList()
                {
                    count++;
                    sum_gt += invo.Val;
                    //with Console 539ms and without 328ms ~ 3'593'014 ticks
                    //Console.WriteLine("{0} {1} {2} {3} {4}", count, invo.Idn, invo.Dt_Invo, invo.Val, invo.Note);
                }
                long lst = DateTime.Now.Ticks;
                long ts = lst - fst;
                Console.WriteLine("Done 0. Count={0}, Sum={1}. Time elapsed {2} ticks", count, sum_gt, ts);
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
                Console.WriteLine("Done 1. Count={0}, Sum={1}. Time elapsed {2}", count, sum_gt, ts);

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
                Console.WriteLine("Done 2. Time elapsed {0}.", ts);
                Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
            }
        }
    }
}
