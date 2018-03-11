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
  //автоматически отобразить
  //select поля на свойства
  AutoMapProps(3);
  //MapToCurrent("Idn", "Idn");
  //MapToCurrent("Val", "Val");
  //MapToCurrent("Note", "Note");
  //вручную - если имена отличаются
  MapToCurrent("Dt_Invo", "DtInvo");
 }

 public override void OnReset(string message)
 {
  //отладочная информация
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
Заметьте, что в данном случае в *dynaObject* колонок полей больше, чем свойств `Invo`, но `QueryInvo` загружает данные на пересечении свойств. Замеры производительности показывают, что скорость исполнения *LINQ to Objects* запросов по кэшированным данным у `DynaQuery<T>` в полтора раза выше, чем у `EF`. Кроме того, переменная составляющая, характеризующая в том числе затраты времени на рефлексию свойств при загрузке данных из БД, в 2.66 раза меньше, чем у `EF`.

Замеры производительности в сравнении с LINQ to EF приведены тут: [Challenge](https://github.com/Kobdik/DynaRepo/blob/master/docs/Challenge.md).
