# DynaRepo
Основной целью проекта является разработка инструментария для быстрой передачи данных из хранилища БД в выходной поток данных. `SqlDataAdapter` считывает данные из БД в `DataSet`, `Entity Framework` также кэширует сущности в памяти, оба подхода неприемлемы с точки зрения производительности. 
## DynaObject
Использование `DynaObject` позволяет сразу записывать данные из `IDataReader` в выходной поток в `binary`, `json` или `xml` форматах. Изменения в структуре запросов к БД не должны приводить к вынужденной перекомпиляции модулей как это происходит со сгенерированными частичными классами сущностей в `Entity Framework`. Для этих целей применяются словари метаданных, сохраняемые в самой БД в виде таблиц описания запросов, параметров и колонок запросов. Например, изменяя хранимые процедуры для осуществления операций чтения, надо лишь внести соответствующие изменения в словари метаданных и после их повторной загрузки DynaObject настроится на запись в поток данных с новой структурой.

## DynaQuery<T>
Пользователям `LINQ to Entities` использующим обобщенный интерфейс `IQueryable<T>` можно предложить воспользоваться обобщенным классом `DynaQuery<T>`, реализующим обобщенные интерфейсы `IEnumerable<T>`, `IEnumerator<T>`, который внутренне использует объект `DynaObject`.
```csharp
	
    //Model
    public class Nach
    {
        public Nach() { }
        public Int32 Idn { get; set; }
        public DateTime DtNach { get; set; }
        public double Val { get; set; }
        public string Note { get; set; }
    }
	
    //Реализует IEnumerable<Nach>, IEnumerator<Nach>,
    //а также mapping свойств DynaObject на <Nach> 
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

    static void Test1()
    {
        //запрос NachCut исполняется без параметров и возвращает колонки: Idn, Dt_Nach, Val, Note;
        IDynaObject dynaObject = dataMod.GetDynaObject("NachCut");
        QueryNach queryNach = new QueryNach(dynaObject);
        int count = 0;
        double sum_gt = 0;
        DateTime fst = DateTime.Now;
        Console.WriteLine("Total memory {0}", GC.GetTotalMemory(false));
        //LINQ to Objects
		var query = from nach in queryNach
                    where nach.Val > 6600
                    orderby nach.DtNach
                    select nach;
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
    }
```
Скорость исполнения на порядок выше, чем при использовании `EF`:
Аналогичный код в `EF` c выводом в консоль занял 539ms, а без вывода в консоль - 347ms.
Можете сами проверить, пример выложу.