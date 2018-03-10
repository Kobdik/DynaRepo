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
```
Тесты с замерами производительности `DynaObject` находятся в проекте *QueryApp*. В данных примерах dataMod создает экземпляры dynaObject, настроенные на использование SqlConnection и чтение/запись в json формате.

Входные параметры могут загружаться из Invoice_Params.json
```json
{"Dt_Fst":"2009.01.01", "Dt_Lst":"2017.07.01"}
```
Что равносильно заданию параметров в коде
```csharp
dynaObject.ParmDict["Dt_Fst"].Value = "2009.01.01";
dynaObject.ParmDict["Dt_Lst"].Value = "2017.07.01";
```
Пример стандартного применения `DynaObject` без создания объектов модели `Invoice`.
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
Оба запроса обращаются к таблице *dbo.T_Invoice*, в которой 4569 записей. Общая производительность `DynaLib` в 7.43 раза выше. Чуть позже дам оценку скорости сериализации `EF` коллекции объектов модели.

Проведем трех-этапный тест `EF`. В первую очередь нас интересует скорость чтения данных из БД без вывода в консоль при первоначальной загрузке данных. В данном случае лучший результат составил 3'125'000 ticks, около 312 ms.
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
  //Время ~150'000 ticks
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
  //Память ~7'265'648 Mb
  Console.WriteLine("Done 2. Time elapsed {0}.", ts);
  Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
 }
}
```

Для сравнения с `EF` произведём замеры с классом `QueryInvo : DynaQuery<Invo>` используя аналогичные запросы `LINQ to Objects`.
И снова прежде всего нас интересует скорость чтения данных из БД без вывода в консоль при первоначальной загрузке данных. В данном случае статистически устойчивый результат составил 312'000 ticks, около 31 ms.

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
  //статистически ~ 312'000 tikcs ( 31ms),
  //хотя лучшее время 156'000 ticks (16ms)
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
 //Время ~0 ticks
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

1. Первоначальная выборка с записью в консоль выполняется за 680ms против 1000ms у `EF`.
2. Выборка данных без вывода в консоль занимает 31ms против 360ms, что более чем в 11.5 раз быстрее. 
3. Повторное исполние запроса в обоих случаях происходит по кэшированным данным, только замеры `DynaQuery` дают 0ms, а у `EF` - 15ms.   
4. Группировка по кэшированным данным осуществляется примерно в 1.5 раза быстрее. 
5. Оперативной памяти используется при этом 2mb против 7mb.

По-началу я думал, что раз `EF` инициализирует новые сущности, прибегая к рефлексии далеко не самым эффективным способом, то в этом и причина основных "тормозов". Однако, замеры на разных по размеру выборках показали, что постоянная составляющая в линейной зависимости затраченного времени к размеру выборки у `EF` примерно в 17.4 раза больше чем у `DynaLib`. Потери же на неэффективную рефлексию сидят в более высокой переменной составляющей расхода времени пропорционально размеру выборки, который у `EF` оказался в 2.66 раза выше. 

Линейная зависимость расхода времени к размеру выборки у `DynaLib`

|общий расход|постоянная|переменная|в отн. ед.|выборка|	
|--------|------|-------|-------|-----------|
|312     | 156  | 156   | 1.000 |base (4569)|
|781	 | 156  | 625   | 4.006 |base4      |
|1 406	 | 156  | 1 250 | 8.013 |base8      |
|2 656	 | 156  | 2 500 | 16.02 |base16     |

Поясню данные таблицы замеров: base - базовая выборка с 4569 записями, base4 - 4х кратная реплика base, base8 - 8-ми кратная реплика base, base16 - 16-ти кратная реплика base. 

Общий расход времени на базовую выборку составил 312 000 ticks, из них 156 000 ticks - постоянная составляющая и 156 000 ticks - переменная. Пусть 1.000 в относительных единицах означает 156 000 ticks переменной составляющей.

Общий расход времени на выборку base4 составил 781 000 ticks, что означает 625 000 ticks на переменную составляющую или 4.006 в относительных единицах, что хорошо согласуется с 4-х кратным увеличением выборки. Аналогично, хорошо согласованы с предположением о величине постоянной составляющей в 156 000 ticks данные по выборкам base8 и base16.

Данные аналогичных замеров `EF` приводят к следующей таблице линейной зависимости расхода времени к размеру выборки:  
|общий расход|постоянная|переменная|в отн. ед.|выборка|	
|--------|------|-------|-------|-----------|
|3 125	 |2 710 |415    |1.000  |base (4569)|
|4 375	 |2 710 |1 665  |4.012  |base4      |
|5 780   |2 710 |3 070  |7.398  |base8      |
|9 530   |2 710 |6 820	|16.434 |base16     |

Данные уже не так хорошо согласованы, `EF` крайне не стабилен при замерах времени, заваливаясь по скорости, но я настойчиво выполнял одни и те же прогоны, чтобы выявить лучшие результаты.

Вспомним самый первый тест с записью в поток. Общее время у `EF` составило 4'654'000 ticks, а у `DynaLib` - 626'000 ticks. Это означает, что на запись в поток `EF` затратил около 4'654'000 - 3'125'000 = 1'529'000 ticks, а `DynaLib` 626'000 - 312'000 = 314'000 ticks, что 4.87 раз быстрее результата `EF`. 

Совокупная переменная составляющая чтения и записи у `EF` около 4'654'000 - 2'710'000 = 1'944'000 ticks, а `DynaLib` 626'000 - 156'000 = 470'000 ticks, что 4.14 раз быстрее результата `EF`.

Нивелировать потери на постоянную составляющую затрат времени `EF` (подготовку и исполнение SQL запроса и прочее) можно при запросе огромных выборок в сотни тысяч и более записей. Только когда такое может пригодится? Однако, даже при таком неэффективном сценарии `EF` будет уступать в скорости `DynaLib` более чем в 4 раза!
