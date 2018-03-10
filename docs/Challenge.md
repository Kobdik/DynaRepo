## Замеры производительности

Итак, начнем с `EF` в приложении LinqToEntityApp. В первые несколько раз или после небольшой паузы расход времени больще раза в два-три, но будем снисходительны, в зачёт пойдут только лучшие результаты.

Сделаем небольшой тест по выборке данных с помощью `EF` и сериализации стандартным способом в json-файл. 
```csharp
private static void Test1J()
{
 using (var context = new TestModel())
 {
  long fst = DateTime.Now.Ticks;
  //Первоначальная загрузка из БД
  Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
  var query = from invoice in context.Invoices
      where invoice.Val > 0
      orderby invoice.Dt_Invo
      select invoice;
  //Лучшее время 4'654'000 ticks
  using (FileStream wfs = new FileStream("InvoiceEF.json", FileMode.Create))
  {
   DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(IEnumerable<Invoice>));
   ser.WriteObject(wfs, query);
  }
  long lst = DateTime.Now.Ticks;
  long ts = lst - fst;
  Console.WriteLine("Done 0. Time elapsed {0} ticks", ts);
  Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
 }
}
```
Пример стандартного применения `DynaObject` без создания объектов модели `Invoice`
```csharp
static void Test2()
{
 IDynaObject dynaObject = dataMod.GetDynaObject("Invoice");
 using (FileStream rfs = new FileStream("Invoice_Params.json", FileMode.Open))
 {
  //считываем параметры из входного потока
  dynaObject.ReadPropStream(rfs, "sel");
 }
 long fst = DateTime.Now.Ticks;
 //исполним запрос, результат пишем в файловый поток
 using (FileStream fs = new FileStream("Invoice.json", FileMode.Create))
 {
  dynaObject.SelectToStream(fs);
 }
 long lst = DateTime.Now.Ticks;
 long ts = lst - fst;
 //вся выборка выгружается в json-файл размером 552Kb за 625'000 ticks,
 //это в 5.76 раз быстрее, чем EF только считывает данные из БД
 //и в 7.43 раз быстрее, чем EF читает и пишет данные в поток
 Console.WriteLine("Total time elapsed {0}", ts); 
}
```
Надо будет замерить, сколько времени потребуется `EF` для выборки и сериализации коллекции объектов модели в поток.

```csharp
private static void Test1()
{
 using (var context = new TestModel())
 {
  int count = 0;
  double sum_gt = 0;
  long fst = DateTime.Now.Ticks;
  //Первоначальная загрузка из БД
  Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
  var query = 
      from invo in context.Invos
      where invo.Val > 0
      orderby invo.Dt_Invo
      select invo;
  //LINQ to Entities
  foreach (Invo invo in query)
  {
   count++;
   sum_gt += invo.Val;
   //с выводом в консоль ~10'000'000 ticks (1s), 
   //без вывода ~ 3'125'000 ticks (312ms)
   Console.WriteLine("{0} {1} {2} {3} {4}", count, invo.Idn, invo.Dt_Invo, invo.Val, invo.Note);
  }
  long lst = DateTime.Now.Ticks;
  long ts = lst - fst;
  Console.WriteLine("Done 0. Count={0}, Sum={1}. Time elapsed {2} ticks", count, sum_gt, ts);
  Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
  //На основании скорости повторного исполнения 
  //убеждаемся, что данные были кэшированы
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
  // Time ~150'000 ticks
  Console.WriteLine("Done 1. Count={0}, Sum={1}. Time elapsed {2}", count, sum_gt, ts);
  //Пример группирующего запроса
  fst = DateTime.Now.Ticks;
  var groupQuery = 
      from Invo invo in query
      group new { Mon = invo.Dt_Invo.Month, invo.Val }
      by invo.Dt_Invo.Month;
  //Группировка по кэшированным данным
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
  //Лучшее время ~468'000 ticks, 
  //Memory ~7'265'648
  Console.WriteLine("Done 2. Time elapsed {0}.", ts);
  Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
 }
}
```

Для тестирования `DynaObject` используем файловые потоки ввода-вывода. 
Например, входные параметры могут загружаться из Invoice_Params.json
```json
{"Dt_Fst":"2009.01.01", "Dt_Lst":"2017.07.01"}
```
Что равносильно заданию параметров в коде
```csharp
dynaObject.ParmDict["Dt_Fst"].Value = "2009.01.01";
dynaObject.ParmDict["Dt_Lst"].Value = "2017.07.01";
```

Тесты с замерами производительности DynaObject находятся в QueryApp. В данных примерах dataMod создает экземпляры dynaObject, настроенные на использование SqlConnection и чтение/запись в json формате.

В ближайшее время сделаю, а пока продолжим замеры с классом `QueryInvo : DynaQuery<Invo>`.

```csharp
static void Test3()
{
 IDynaObject dynaObject = dataMod.GetDynaObject("InvoCut");
 //Запрос без параметров
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
  //с выводом в консоль ~6'800'000 (680ms) ticks 
  //и без вывода ~ 320'000 tikcs (32ms)
  //Console.WriteLine("{0} {1} {2} {3} {4}", 
  // count, invo.Idn, invo.DtInvo, invo.Val, invo.Note);
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
 // Time ~0 ticks
 Console.WriteLine("Done 1. Count={0}, Sum={1}. Time elapsed {2}", count, sum_gt, ts);

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
 Console.WriteLine("In {0} month there are {1} rows with amount {2}.", g.Key, g.Count(), g.Sum(a => a.Val));
 }
 lst = DateTime.Now.Ticks;
 ts = lst - fst;
 // Time ~312'500 ticks, Memory ~2'072'780
 Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
 Console.WriteLine("Done 2. Time elapsed {0}.", ts);
}
```
По-началу я думал, что раз `EF` инициализирует новые сущности прибегая к рефлексии, то в этом и причина.  

Первоначальная выборка с записью в консоль выполняется за 680ms против 1000ms у `EF`.
Выборка данных без вывовода в консоль занимает 32ms против 360ms, что более чем в 11 раз быстрее. 
Повторное исполние запроса в обоих случаях происходит по кэшированным данным, только замеры `DynaQuery` дают 0ms, а у `EF` - 15ms.   
Группировка осуществляется примерно в 1.5 раза быстрее. Оперативной памяти используется при этом 2mb против 7mb.
