using System;
using System.Data;
using System.IO;
using System.Linq;
using Kobdik.Common;
using Kobdik.Dynamics;
using Kobdik.DataModule;

namespace QueryApp
{
    class Program
    {
        static DataMod dataMod = DataMod.Current();

        static void Main(string[] args)
        {
            dataMod.ShowError = (message) => {
                Console.WriteLine("Error! {0}", message);
            };
            dataMod.ShowMessage = (message) => {
                Console.WriteLine(message);
            };
            // Test it
            Test2();
            Console.Read();
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
                MapToCurrent("Idn", "Idn");
                MapToCurrent("Dt_Invo", "DtInvo");
                MapToCurrent("Val", "Val");
                MapToCurrent("Note", "Note");
            }

            public override void OnReset(string message)
            {
                Console.WriteLine(message);
            }

        }

        static void Test2()
        {
            IDynaObject dynaObject = dataMod.GetDynaObject("Invoice");
            //dynaObject.ParmDict["Dt_Fst"].Value = "2017.01.11";
            //dynaObject.ParmDict["Dt_Lst"].Value = "2017.01.11";
            dynaObject.ParmDict["Dt_Fst"].Value = "2009.01.01";
            dynaObject.ParmDict["Dt_Lst"].Value = "2017.07.01";
            long fst = DateTime.Now.Ticks;
            using (FileStream fs = new FileStream("Invoice.json", FileMode.Create))
            {
                dynaObject.SelectToStream(fs);
            }
            long lst = DateTime.Now.Ticks;
            long ts = lst - fst;
            //вся таблица выгружается в json-файл размером 726Kb за 590'398 ticks, 
            //это в 6 раз быстрее, чем EF только считывает данные из БД
            Console.WriteLine("Total time elapsed {0}", ts); 
        }

        static void Test3()
        {
            IDynaObject dynaObject = dataMod.GetDynaObject("InvoCut");
            //dynaObject.ParmDict["Dt_Fst"].Value = "2009.01.01";
            //dynaObject.ParmDict["Dt_Lst"].Value = "2017.12.31";
            QueryInvo queryInvo = new QueryInvo(dynaObject);
            int count = 0;
            double sum_gt = 0;
            long fst = DateTime.Now.Ticks;
            Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
            var query = from invo in queryInvo
                        where invo.Val > 0
                        orderby invo.DtInvo
                        select invo;
            //LINQ to Objects
            foreach (Invo invo in query) 
            {
                count++;
                sum_gt += invo.Val;
                //with Console - 101ms and without - 20ms ~ 312'534 tikcs
                //Console.WriteLine("{0} {1} {2} {3} {4}", count, invo.Idn, invo.DtInvo, invo.Val, invo.Note);
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
            Console.WriteLine("Done 1. Count={0}, Sum={1}. Time elapsed {2}", count, sum_gt, ts);

            fst = DateTime.Now.Ticks;
            //IGrouping<int, <Invo>>
            var groupQuery = from Invo invo in query
                             group new { Mon = invo.DtInvo.Month, invo.Val }
                             by invo.DtInvo.Month;

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
            var query = from invo in queryInvo
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
            Console.WriteLine("Done 0");
            Invo first = query.First();
            Console.WriteLine("{0} {1} {2} {3}", first.Idn, first.DtInvo, first.Val, first.Note);
            /**/
            first.DtInvo = DateTime.Parse("31.12.2012");
            first.Val = -1500;
            first.Note = "Данные изменены !";
            queryInvo.Update(first);
            Console.WriteLine(dynaObject.GetInfo());
            Console.WriteLine("First Invoice {0} {1} {2} {3}", first.Idn, first.DtInvo, first.Val, first.Note);
            
        }

    }

}
