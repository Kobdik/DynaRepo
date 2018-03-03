# DynaLib
Основной целью проекта является разработка инструментария, работающего в среде .NET, для быстрой передачи запрашиваемых данных из БД сразу в выходной поток без необходимости создания объектов модели для последующей их сериализации. Такой подход применим при реализации трёх-звенной архитектуры приложения или WEB API интерфейса, так как на стороне сервера нет необходимости во взаимодействии с объектами-сущностями, достаточно лишь последовательно прочитать из реализации `IDataReader` их свойства и записать в поток.

Высокая скорость работы `SqlDataReader` обусловлена применением `FAST_FORWARD` курсора на стороне БД, однако, непосредственно использовать его неудобно, получается слишком много кода. Для доступа к данным обычно прибегают к помощи `EF` (Entity Framework), который внутренне использует `SqlDataReader`, но при этом инициализирует новые сущности, прибегая к рефлексии, что в конечном итоге приводит к существенному (десятикратному) снижению скорости при выборке данных из БД (Test0 - EF, Test2, Test3, Test4 - альтернативные подходы). Ситуацию не улучшить, если далее использовать стандартные подходы сериализации созданных объектов с использованием рефлексии. Сравнение скорости записи в выходной поток проведу в ближайшее время.

Кроме того, `EF` кэширует созданные объекты в памяти, создавая дополнительную работу для сборщика мусора. Использование страничных запросов только нагружает серверную сторону, не решая проблемы с замусоривающейся памятью. По моему мнению, лучше использовать кэширование данных на стороне клиента, там же манипулируя ими, разбивая на страницы и т.д., а на стороне сервера постараться замкнуть поток входных данных из БД прямо на выходной поток.

Для решения проблем с производительностью `EF`, а точнее, для того чтобы не использовать его вовсе, была разработана библиотека `DynaLib`, главная роль в которой принадлежит классу `DynaObject`, который умеет читать параметры из входного потока, вызывать хранимые процедуры на стороне БД, непосредственно работать с `IDataReader`, записывая данные из него в выходной поток в `binary`, `json` или `xml` форматах.

## DynaObject
Обычно, изменения в структуре запросов к БД приводят к вынужденной перекомпиляции модулей как это происходит со сгенерированными частичными классами сущностей в `EF`. Для устойчивости к изменениям `DynaObject` использует словари метаданных, сохраняемые в самой БД в виде таблиц описания запросов, передаваемых параметров и возвращаемых колонок.

T_QryDict - таблица описания запросов, в минимальном варианте:

| Qry_Id | Qry_Name | Qry_Head                | Col_Def |
|--------|----------|-------------------------|---------|
|     27 | Invoice  | Счета                   |      27 |
|     28 | InvoCut  | Счета(усеченный запрос) |      28 |

По соглашению к запросу Invoice относятся хранимая процедура на выборку sel_Invoice, на вставку - ins_Invoice, на изменение - upd_Invoice, на удаление - del_Invoice. Соответственно, к запросу InvoCut относятся хранимые процедуры sel_InvoCut, ins_InvoCut, upd_InvoCut, del_InvoCut. Данные процедуры могут быть нацелены на одну и ту же таблицу.

T_PamDict - таблица параметров select-запросов, в минимальном варианте:

| Qry_Id | Pam_Id | Pam_Name | Pam_Head      | Pam_Type | Pam_Size |
|--------|--------|----------|---------------|----------|----------|
|     27 |      1 |   Dt_Fst |Начальная дата |       40 |        3 |
|     27 |      2 |   Dt_Lst |Конечная дата  |       40 |        3 |

У запроса sel_Invoice имеется два параметра типа BbType.Date (в таблице указано числовое значение перечисления), а у sel_InvoCut параметров нет.

T_ColDict - сводная таблица колонок полей, участвующих в запросах, в минимальном варианте:

| Qry_Id | Col_Id | Col_Name | Col_Head       | Col_Type | Col_Size | Note       | Col_Flag |
|--------|--------|----------|----------------|----------|----------|------------|----------|
|27      | 0      | Idn      | Код начисления | 56       | 4        | idn,out    | 9        |
|27      | 1      | Org      | Контрагент     | 52       | 2        | sel,det    | 6        |
|27      | 2      | Knd      | Вид начисления | 48       | 1        | sel,det    | 6        |
|27      | 3      | Dt_Invo  | Дата начисления| 40       | 3        | sel,det    | 6        |
|27      | 4      | Val      | Начислено      | 62       | 8        | sel,det    | 6        |
|27      | 5      | Crs      | Квитовано      | 62       | 8        |            | 0        |
|27      | 6      | Ost      | Остаток        | 62       | 8        |            | 0        |
|27      | 7      | Note     | Примечание     | 167      | 100      | sel,det    | 6        |
|27      | 8      | Sdoc     | Состояние док. | 48       | 1        | det,opt    | 20       |
|27      | 9      | Dt_Sdoc  | Дата изм. сост.| 40       | 3        | det,opt    | 20       |
|27      | 10     | Lic      | Лицензия       | 56       | 4        | sel,det    | 6        |
|27      | 11     | Usr      | Пользователь   | 167      | 15       | usr        | 32       |
|27      | 12     | Pnt      | Получатель     | 48       | 1        | sel,det    | 6        |
|28      | 0      | Idn      | Код начисления | 56       | 4        | idn,out    | 9        |
|28      | 1      | Dt_Invo  | Дата начисления| 40       | 3        | sel,det    | 6        |
|28      | 2      | Val      | Начислено      | 62       | 8        | sel,det,out| 14       |
|28      | 3      | Note     | Примечание     | 167      | 100      | sel,det,out| 14       |

Операция sel_Invoice на выборку данных может выглядеть так:
```
create proc [dbo].[sel_Invoice] @Dt_Fst date, @Dt_Lst date
as 
 select n.Idn, n.Org, n.Knd, n.Dt_Invo, n.Val, n.Note, n.Lic, n.Pnt
 from dbo.T_Invoice n where n.Dt_Invo between @Dt_Fst and @Dt_Lst
return 0;
```
По соглашению при исполнении sel_Invoice будут выбраны значения только тех колонок, где включены биты idn(1) и sel(2). Col_Flag как раз и определяет маску (сумму) битовых флагов колонки (idn - 1, sel - 2, det - 4, out - 8, opt - 16, usr - 32). Даже, если в select-запросе будет присутствовать n.Sdoc, у колонки Sdoc маска флагов 20=4+16, бит sel(2) не включен, в поток она не попадет. 

Битовым флагом sel помечены колонки полей отображаемых в списках. В реальном приложении поле Note в списке не отображается (бит sel выключен), оно слишком длинное и будет запрашиваться только при запросе на детализацию выбранной строки списка. Детализации строки в данном случае соответствует отдельная операция det_Invoice, которая может выглядеть так:
```
create proc [dbo].[det_Invoice] @Idn int
as 
 select n.Idn, n.Org, n.Knd, n.Dt_Invo, n.Val, n.Note, n.Sdoc, n.Dt_Sdoc, n.Lic, n.Usr, n.Pnt
 from dbo.T_Invoice n where n.Idn=@Idn
return 0;
```
По соглашению при исполнении det_Invoice в выходной поток попадут значения только тех колонок, где включены биты idn(1), sel(2), det(4) и usr(32).

Операция upd_Invoice на изменение данных может выглядеть так:
```
create proc [dbo].[upd_Invoice]
@Idn int out, @Dt_Invo date, @Org smallint, @Knd tinyint,  @Val float, @Note varchar(100), @Lic int, @Usr varchar(15), @Pnt tinyint
as
  update dbo.T_Invoice 
  set Org=@Org, Knd=@Knd, Dt_Invo=@Dt_Invo, Val=@Val, Note=@Note, Lic=@Lic, Usr=@Usr, Sts=2, Chg=GETDATE(), Pnt=@Pnt
  where Idn=@Idn
return 0;
```
Также по соглашению при исполнении ins_Invoice и upd_Invoice по умолчанию из входного потока будут выбраны значения только тех колонок, где включены биты idn(1), det(4), out(8) и usr(32), для каждой такой колонки будет создан входной параметр нужного типа и размера. Для колонок с битом out(8) параметры получат ParameterDirection.InputOutput, после исполнения хранимой процедуры, выходные значения будут считаны и записаны в выходной поток. 

Изменяя хранимые процедуры, следует внести соответствующие изменения в словари метаданных и после их повторной загрузки `DynaObject` настроится на запись в поток данных с новой структурой. Такое обновление словаря можно осуществлять без остановки WEB API приложения или сервера приложения.

Ориентация на оптимизированные специалистом БД хранимые процедуры, разграничение прав доступа к ним, по меньшей мере дисциплинирует программиста .NET, снижает вероятность непреднамеренного нарушения целостности данных, либо компрометации конфиденциальных данных.

## DynaQuery<T>
Для применения `LINQ to Objects` воспользуйтесь обобщенным классом `DynaQuery<T>`, реализующим обобщенные интерфейсы `IEnumerable<T>`, `IEnumerator<T>`, который в конструкторе принимает объект `DynaObject`. Там же в конструкторе следует указать mapping колонок `DynaObject` на открытые свойства <T>.  

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
```

Затем, в коде можно осуществлять запросы `LINQ to Objects`
```csharp	
 IDynaObject dynaObject = dataMod.GetDynaObject("Invoice");
 dynaObject.ParmDict["Dt_Fst"].Value = "2017.01.01";
 dynaObject.ParmDict["Dt_Lst"].Value = "2017.12.31";
 QueryInvo queryInvo = new QueryInvo(dynaObject);
 var query = from invo in queryInvo
             where invo.Val > 1000
             orderby invo.DtInvo
             select invo;
 //LINQ to Objects
 foreach (Invo invo in query) 
 {
  Console.WriteLine("{0} {1} {2} {3}", invo.Idn, invo.DtInvo, invo.Val, invo.Note);
 }
```

## Замеры производительности

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
  var query = from invo in context.Invos
              where invo.Val > 0
              orderby invo.Dt_Invo
              select invo;
  //LINQ to Entities
  foreach (Invo invo in query)
  {
   count++;
   sum_gt += invo.Val;
   //вывод в консоль ~10'000'000 ticks (1s), без вывода ~ 3'600'000 ticks (360ms)
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

Тесты с замерами производительности DynaObject находятся в QueryApp.

```csharp
static void Test2()
{
 //В данном случае dataMod создает экземпляр dynaObject, настроенный
 //на использование SqlConnection и чтение/запись в json формате.  
 IDynaObject dynaObject = dataMod.GetDynaObject("Invoice");
 using (FileStream rfs = new FileStream("Invoice_Params.json", FileMode.Open))
 {
  //считываем параметры запроса из входного json-потока
  dynaObject.ReadPropStream(rfs, "sel");
 }
 long fst = DateTime.Now.Ticks;
 //исполним запрос и запишем результат в файловый поток
 using (FileStream fs = new FileStream("Invoice.json", FileMode.Create))
 {
  dynaObject.SelectToStream(fs);
 }
 long lst = DateTime.Now.Ticks;
 long ts = lst - fst;
 //вся выборка выгружается в json-файл размером 726Kb за 590'398 ticks, 
 //это в 6 раз быстрее, чем EF только считывает аналогичные данные из БД
 Console.WriteLine("Total time elapsed {0}", ts); 
}
```


```csharp
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