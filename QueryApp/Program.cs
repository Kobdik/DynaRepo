using System;
using System.Data;
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
            Test3();
            Console.Read();
        }

        public class Nach
        {
            public Nach() { }

            public Int32 Idn
            {
                get; set;
            }

            public DateTime DtNach
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

        public class QueryNach : DynaQuery<Nach>
        {
            public QueryNach(IDynaObject dynaObject) : base(dynaObject)
            {
                MapToCurrent("Idn", "Idn");
                MapToCurrent("Dt_Nach", "DtNach");
                MapToCurrent("Val", "Val");
                MapToCurrent("Note", "Note");
            }

            public override void OnReset(string message)
            {
                Console.WriteLine(message);
            }

        }

        static void Test3()
        {
            IDynaObject dynaObject = dataMod.GetDynaObject("NachCut");
            //dynaObject.ParmDict["Dt_Fst"].Value = "2017.01.01";
            //dynaObject.ParmDict["Dt_Lst"].Value = "2017.07.31";
            QueryNach queryNach = new QueryNach(dynaObject);
            int count = 0;
            double sum_gt = 0;
            DateTime fst = DateTime.Now;
            Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
            var query = from nach in queryNach
                        where nach.Val > 6600
                        orderby nach.DtNach
                        select nach;
            //.OrderBy(n => n.DtNach).Select(n => n).ToList()
            foreach (Nach nach in query) 
            {
                count++;
                sum_gt += nach.Val;
                //with Console - 69ms and without - 24ms
                Console.WriteLine("{0} {1} {2} {3} {4}", count, nach.Idn, nach.DtNach, nach.Val, nach.Note);
            }
            DateTime lst = DateTime.Now;
            TimeSpan ts = lst - fst;
            Console.WriteLine("Done 0. Count={0}, Sum={1}. Time elapsed {2}.", count, sum_gt, ts.Milliseconds);
            Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));

            count = 0;
            sum_gt = 0;
            fst = DateTime.Now;
            foreach (Nach nach in query)
            {
                count++;
                sum_gt += nach.Val;
            }
            lst = DateTime.Now;
            ts = lst - fst;
            Console.WriteLine("Done 1. Count={0}, Sum={1}. Time elapsed {2}", count, sum_gt, ts.Milliseconds);

            fst = DateTime.Now;
            var groupQuery = from Nach nach in query
                             group new { Mon = nach.DtNach.Month, nach.Val }
                             by nach.DtNach.Month;
            foreach (var g in groupQuery.OrderBy(g => g.Key))
            {
                foreach (var a in g.Take(10))
                {
                    Console.WriteLine("{0} {1}", a.Mon, a.Val);
                }
                Console.WriteLine("In {0} month there are {1} rows with amount {2}.", g.Key, g.Count(), g.Sum(a => a.Val));
            }
            lst = DateTime.Now;
            ts = lst - fst;
            Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
            Console.WriteLine("Done 2. Time elapsed {0}.", ts.Milliseconds);
        }

        static void Test4()
        {
            IDynaObject dynaObject = dataMod.GetDynaObject("NachCut");
            if (dynaObject == null) {
                Console.WriteLine(dataMod.lastError);
                return;
            }
            QueryNach queryNach = new QueryNach(dynaObject);
            var query = from nach in queryNach
                        where nach.DtNach < DateTime.Parse("31.12.2012")
                        orderby nach.DtNach
                        select nach;
            int count = 0;
            DateTime fst = DateTime.Now;
            foreach (Nach nach in query)
            {
                Console.WriteLine("{0} {1} {2} {3} {4}", count++, nach.Idn, nach.DtNach, nach.Val, nach.Note);
            }
            DateTime lst = DateTime.Now;
            TimeSpan ts = lst - fst;
            Console.WriteLine("Done 0. Count={0}, Time elapsed {1}.", count, ts.Milliseconds);
            Console.WriteLine("Done 0");
            Nach first = query.First();
            Console.WriteLine("{0} {1} {2} {3}", first.Idn, first.DtNach, first.Val, first.Note);
            /*
            first.DtNach = DateTime.Parse("31.12.2012");
            first.Val = -1500;
            first.Note = "Данные изменены !";
            queryNach.Update(first);
            Console.WriteLine(dynaObject.GetInfo());
            Console.WriteLine("Nach {0} {1} {2} {3}", first.Idn, first.DtNach, first.Val, first.Note);
            */
        }
    }

}
