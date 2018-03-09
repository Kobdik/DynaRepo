# DynaLib

Основной целью проекта является разработка инструментария на базе ADO.NET, для вызова хранимых процедур БД, передачи возвращаемых данных из `IDataReader` сразу в выходной поток без необходимости создания объектов модели для последующей их сериализации. Такой подход применим при реализации трёх-звенной архитектуры приложения или WEB API интерфейса, так как на стороне сервера нет необходимости во взаимодействии с объектами-сущностями, достаточно лишь последовательно прочитать из реализации `IDataReader` данные свойств и записать их в поток.

Высокая скорость работы `SqlDataReader` обусловлена применением `FAST_FORWARD` курсора на стороне БД, однако, непосредственно использовать его неудобно, получается слишком много ручного кодирования. Поэтому, для доступа к данным прибегают к помощи `EF` (Entity Framework), который внутренне использует `SqlDataReader`, однако, скорость загрузки данных падает от 3-х до более чем в 10 раз. Забегая вперёд скажу, что в моих тестовых замерах производительности относительная скорость `EF` ниже в 3.7 раза при больших выборках, когда запрос возвращает более 70 000 записей, а на малых выборках, менее 1 000 записей, относительная скорость падает в 17 раз!

Кроме того, `EF` расходует в 3 раза больше памяти, создавая дополнительную работу для сборщика мусора. Использование страничных запросов для снижения расхода памяти не только нагружает серверную сторону частыми обращениями, но и приводит в режим малых выборок, где относительная производительность `EF` в 17 раз ниже. Получается, что при частых обращениях клиентов к WEB API приложению с запросами, возвращающими малые выборки, следует отказаться от использования `EF` для доступа к данным, конечно, если вы не решили кэшировать всю базу в памяти.

Для решения проблем с производительностью `EF`, а точнее, для того чтобы не использовать его вовсе, была разработана библиотека `DynaLib`, главная роль в которой принадлежит классу `DynaObject`, который умеет читать параметры из входного потока, вызывать хранимые процедуры на стороне БД, непосредственно работать с `IDataReader`, записывая данные из него в выходной поток в `binary`, `json` или `xml` форматах.

## Dictionary First

Если задача серверного приложения переслать клиентской программе данные табличного запроса, то почему обязательно сначала создавать сущности, затем инициализировать, считывая  из `IDataReader` их свойства, а затем уже сериализовать эти объекты в выходной поток? Чем один запрос отличается от другого? - Параметрами и набором возвращаемых колонок. Зная эти метаданные можно вызвать хранимую процедуру, прочитать результат и выгрузить его в поток. При этом на стороне сервера не нужно плодить классы модели, классы контроллеров, при изменении структуры запроса не нужно перекомпилировать приложение, главное знать актуальные метаданные. 

Организация словарей метаданных в форме таблиц описаний, позволяет эффективно управлять запросами на основе хранимых процедур, унифицирует и упрощает обработку данных, способствует применению декларативного стиля программирования. Подробнее тут: [Dictionary First](Dictionary.md)

 

## DynaObject

Для создания объектов `DynaObject` используется фабричный метод `IDynaObject DataMod.GetDynaObject(string queryName)`. Singleton экземпляр DataMod при инициализации загружает всю необходимую мета-информацию из БД, используемую при создании объектов, а также снабжает их `binary`, `json` или `xml` форматтерами для записи и чтения из потоков.

На стороне контроллера объекты `DynaObject` доступны через реализуемый ими интерфейс `IDynaObject`: 
```csharp
//Через интерфейс удобнее и 
//безопаснее работать с объектом
public interface IDynaObject : IDisposable
{
 //описания параметров select-запроса
 Dictionary<String, IDynaProp> ParmDict { get; }
 //описания колонок запросов
 Dictionary<String, IDynaProp> PropDict { get; }
 //читает из потока значения select-параметров 
 //или значения колонок при insert/update
 void ReadPropStream(Stream stream, string cmd);
 //пишут результаты запросов в выходной поток
 void SelectToStream(Stream stream);
 void DetailToStream(Stream stream, int idn);
 void ActionToStream(Stream stream, string cmd);
 //Служит для целей отладки, возвращает
 //имена и значения колонок PropDict
 string GetInfo();
}
```

## DynaQuery<T>
Для применения `LINQ to Objects` следует создать экземпляр обобщенного класса `DynaQuery<T>`, реализующий обобщенные интерфейсы `IEnumerable<T>`, `IEnumerator<T>`, передав ему в конструкторе соответствующий классу модели `T` экземпляр `DynaObject`. Там же в конструкторе следует указать mapping колонок `DynaObject` на открытые свойства класса `T`. Перед использованием следует указать параметры запроса, если они имеются.

При первом проходе цикла итераций, сначала исполняется select-запрос к БД, возвращается `IDataReader` и каждая последующая итерация продвигается вместе с курсором БД. Данные кэшируются во внутреннем списке `List<T>`, так что повторные циклы итераций работают уже над кэшем, который при желании можно обновить.

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
//а также mapping колонок полей DynaObject на <Invo> 
public class QueryInvo : DynaQuery<Invo>
{
 public QueryInvo(IDynaObject dynaObject) 
  : base(dynaObject)
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
```

Затем, в коде можно осуществлять запросы `LINQ to Objects`
```csharp
 //DataMod dataMod = DataMod.Current();
 IDynaObject dynaObject = dataMod.GetDynaObject("Invoice");
 dynaObject.ParmDict["Dt_Fst"].Value = "2017.01.01";
 dynaObject.ParmDict["Dt_Lst"].Value = "2017.12.31";
 QueryInvo queryInvo = new QueryInvo(dynaObject);
 var query = 
     from invo in queryInvo
     where invo.Val > 1000
     orderby invo.DtInvo
     select invo;
 //LINQ to Objects
 foreach (Invo invo in query) 
 {
  Console.WriteLine("{0} {1} {2} {3}", invo.Idn, invo.DtInvo, invo.Val, invo.Note);
 }
```
Заметьте, что в данном случае в `dynaObject` колонок оказалось больше, чем открытых свойств `Invo`, что не мешает загрузке данных в `List<Invo>`.


## Замеры производительности

По-началу я думал, что раз `EF` инициализирует новые сущности прибегая к рефлексии, то в этом и причина.  

Итак, начнем с `EF` в приложении LinqToEntityApp.
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
   //без вывода ~ 3'600'000 ticks (360ms)
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
  // Time ~468'000 ticks, 
  // Memory ~7'265'648
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
В моем реальном WEB API приложении параметры приходят в теле post-запроса, что позволяет избежать привязки моделей. Более того, не нужно создавать отдельный контроллер под каждый тип запроса. Достаточно одного контроллера c 4-5 точками входа, обрабатывающего запросы в стиле RPC.

Тесты с замерами производительности DynaObject находятся в QueryApp. В данных примерах dataMod создает экземпляры dynaObject, настроенные на использование SqlConnection и чтение/запись в json формате.

Пример стандартного применения `DynaObject` без создания объектов модели `Invo`
```csharp
static void Test2()
{
 IDynaObject dynaObject = dataMod.GetDynaObject("Invoice");
 //Запрос sel_Invoice возвращает на 4 поля больше, 
 //чем в таблице T_InvoCut, с которой работает `EF`
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
 //вся выборка выгружается в json-файл размером 552Kb 
 //за 625'000 ticks, это в 5.76 раз быстрее, 
 //чем EF только считывает аналогичные данные из БД
 Console.WriteLine("Total time elapsed {0}", ts); 
}
```
Надо будет замерить, сколько времени потребуется `EF` для выборки и сериализации коллекции объектов модели в поток. В ближайшее время сделаю, а пока продолжим замеры с классом `QueryInvo : DynaQuery<Invo>`.

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
Первоначальная выборка с записью в консоль выполняется за 680ms против 1000ms у `EF`.
Выборка данных без вывовода в консоль занимает 32ms против 360ms, что более чем в 11 раз быстрее. 
Повторное исполние запроса в обоих случаях происходит по кэшированным данным, только замеры `DynaQuery` дают 0ms, а у `EF` - 15ms.   
Группировка осуществляется примерно в 1.5 раза быстрее. Оперативной памяти используется при этом 2mb против 7mb.
