# DynaRepo
Основной целью проекта является разработка инструментария для быстрой передачи данных из хранилища БД в выходной поток. Такая потребность возникает при организации трёх-звенной архитектуры приложения или при реализации WEB API интерфейса. Скорость работы `SqlDataReader` обусловлена применением `FAST_FORWARD` курсора на стороне БД, однако, непосредственно использовать его неудобно, получается слишком много кода. С другой стороны, `SqlDataAdapter` кэширует данные из БД в `DataSet`, `Entity Framework` также кэширует сущности в памяти, оба подхода создают дополнительную работу для сборщика мусора, что неприемлемо с точки зрения производительности. По этой причине был разработан `DynaObject`, некий аналог `SqlDataAdapter`, который умеет передавать параметры и вызывать хранимые процедуры БД и работает непосредственно с `IDataReader`, записывая данные из него в выходной поток в `binary`, `json` или `xml` форматах.
## DynaObject
Обычно, изменения в структуре запросов к БД приводят к вынужденной перекомпиляции модулей как это происходит со сгенерированными частичными классами сущностей в `Entity Framework`. Для устойчивости к изменениям `DynaObject` использует словари метаданных, сохраняемые в самой БД в виде таблиц описания запросов, передаваемых параметров и возвращаемых колонок. Например, изменяя хранимые процедуры для осуществления операций чтения, надо лишь внести соответствующие изменения в словари метаданных и после их повторной загрузки `DynaObject` настроится на запись в поток данных с новой структурой.
Для примера, входные параметры загружаются из Invoice_Params.json
```json
{"Dt_Fst":"2009.01.01", "Dt_Lst":"2017.07.01"}
```
В моем реальном WEB API приложении параметры приходят в теле post-запроса, что позволяет избежать привязки моделей. Более того, не нужно создавать отдельный контроллер под каждый тип запроса. Достаточно одного многофункционального контроллера, обрабатывающего запросы в стиле RPC.
```csharp
static void Test2()
{
 IDynaObject dynaObject = dataMod.GetDynaObject("Invoice");
 //запрос на максимальную выборку
 //dynaObject.ParmDict["Dt_Fst"].Value = "2009.01.01";
 //dynaObject.ParmDict["Dt_Lst"].Value = "2017.07.01";
 using (FileStream rfs = new FileStream("Invoice_Params.json", FileMode.Open))
 {
  //считываем параметры запроса из входного json-потока
  dynaObject.ReadPropStream(rfs, "sel");
 }
 long fst = DateTime.Now.Ticks;
 //исполним запрос и запишем результат в файл
 using (FileStream fs = new FileStream("Invoice.json", FileMode.Create))
 {
  dynaObject.SelectToStream(fs);
 }
 long lst = DateTime.Now.Ticks;
 long ts = lst - fst;
 //вся таблица выгружается в json-файл размером 726Kb за 590'398 ticks, 
 //это в 6 раз быстрее, чем EF только считывает аналогичные данные из БД
 Console.WriteLine("Total time elapsed {0}", ts); 
}
```
## DynaQuery<T>
Пользователям `LINQ to Entities` использующим обобщенный интерфейс `IQueryable<T>` можно предложить воспользоваться обобщенным классом `DynaQuery<T>`, реализующим обобщенные интерфейсы `IEnumerable<T>`, `IEnumerator<T>`, который внутренне использует объект `DynaObject`.
```csharp	
//Model
public class Invo
{
 public Invo() { }
 public Int32 Idn { get; set; }
 public DateTime DtInvo { get; set; }
 public double Val { get; set; }
 public string Note { get; set; }
}
	
//Реализует IEnumerable<Invo>, IEnumerator<Invo>,
//а также mapping свойств DynaObject на <Invo> 
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
  //with Console ~7'000'000 (700ms) ticks and without ~ 300'000 tikcs (30ms)
  //Console.WriteLine("{0} {1} {2} {3} {4}", count, invo.Idn, invo.DtInvo, invo.Val, invo.Note);
 }
 long lst = DateTime.Now.Ticks;
 long ts = lst - fst;
 Console.WriteLine("Done 0. Count={0}, Sum={1}. Time elapsed {2} ticks.", count, sum_gt, ts);
 Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
 //Запрос по кэшированным данным
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
```
Скорость исполнения на порядок выше, чем при использовании `EF`:
Аналогичный код в `EF` c выводом в консоль занял 539ms, а без вывода в консоль - 347ms.
Можете сами проверить, пример выложу.